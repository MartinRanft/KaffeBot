using System.Collections.Concurrent;
using System.Data;
using System.Threading.Channels;

using Discord;
using Discord.WebSocket;

using KaffeBot.Discord.grundfunktionen.Server;
using KaffeBot.Functions.Discord.EmbedButton;
using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Models.KI;
using KaffeBot.Models.KI.Enums;
using KaffeBot.Services.DB;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using static KaffeBot.Models.KI.Enums.BildConfigEnums;

namespace KaffeBot.Discord.grundfunktionen.KI
{
    public class ComfyUiGenerating(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule, IButtonModule, ICompounModule
    {
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly DiscordSocketClient _client = client;

        private static ConcurrentDictionary<ulong, UserAiSettings> UserSettings { get; set; } = [];

        public bool ShouldExecuteRegularly { get; set; } = true;

        public Task ActivateAsync(ulong channelId, string moduleName)
        {
            MySqlParameter[] isActivePara =
            [
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleName),
                new MySqlParameter("@IsActive", true)
            ];

            _ = _databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);

            return Task.CompletedTask;
        }

        public Task DeactivateAsync(ulong channelId, string moduleName)
        {
            MySqlParameter[] isActivePara =
            [
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleName),
                new MySqlParameter("@IsActive", false)
            ];

            _ = _databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);
            return Task.CompletedTask;
        }

        public Task Execute(CancellationToken stoppingToken)
        {
            List<ulong> keysToRemove = UserSettings
                                       .Where(kv => (DateTime.Now - kv.Value.LoadedDateTime)?.TotalHours > 2)
                                       .Select(kv => kv.Key)
                                       .ToList();

            foreach (ulong userId in keysToRemove)
            {
                // Diese Variable wird nicht verwendet, aber TryRemove erfordert sie.
                _ = UserSettings.TryRemove(userId, out UserAiSettings removedValue);
            }

            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketSlashCommand command)
        {
            await (command.Data.Name switch
            {
                "generate_picture" => GenerateAiPicture(command),
                "setting_aigenrating" => SettingAi(command),
                _ => Task.CompletedTask
            }).ConfigureAwait(false);
        }

        private async Task SettingAi(SocketSlashCommand command)
        {
            ulong userId = command.User.Id;

            UserAiSettings? userSetting = await LoadSettings(userId);

            EmbedBuilder embed = new();
            ComponentBuilder component = new();

            embed.WithTitle("Deine aktuellen AI Einstellungen")
                 .WithColor(Color.DarkBlue);

            if(userSetting is not null)
            {
                List<LoraStack?> loras = new() { userSetting.Lora1, userSetting.Lora2, userSetting.Lora3, userSetting.Lora4, userSetting.Lora5 };

                for (int i = 0; i < loras.Count; i++)
                {
                    AiButtonGen.AddSettingFieldAndButton(embed, component, $"Lora {i + 1}", loras[i], i + 1);
                }


                AiButtonGen.AddSettingFieldAndButton(embed, component, "Model", userSetting.Model, 1);
            }
            else
            {
                embed.WithDescription("Keine Einstellungen gesetzt.");
            }

            await command.RespondAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
        }

        private async Task SettingAi(SocketMessageComponent command)
        {
            ulong userId = command.User.Id;

            UserAiSettings? userSetting = await LoadSettings(userId);

            EmbedBuilder embed = new();
            ComponentBuilder component = new();

            embed.WithTitle("Deine aktuellen AI Einstellungen")
                 .WithColor(Color.DarkBlue);

            if(userSetting is not null)
            {
                List<LoraStack?> loras = new() { userSetting.Lora1, userSetting.Lora2, userSetting.Lora3, userSetting.Lora4, userSetting.Lora5 };

                for (int i = 0; i < loras.Count; i++)
                {
                    AiButtonGen.AddSettingFieldAndButton(embed, component, $"Lora {i + 1}", loras[i], i + 1);
                }

                AiButtonGen.AddSettingFieldAndButton(embed, component, "Model", userSetting.Model, 1);
            }
            else
            {
                embed.WithDescription("Keine Einstellungen gesetzt.");
            }

            await command.RespondAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
        }

        private async Task<UserAiSettings?> LoadSettings(ulong userId)
        {
            lock(UserSettings)
            {
                if(UserSettings.TryGetValue(userId, out UserAiSettings? settings))
                {
                    TimeSpan? timeSinceLastLoad = DateTime.Now - settings.LoadedDateTime;
                    if(timeSinceLastLoad is { TotalHours: < 2 })
                    {
                        return settings;
                    }
                }
            }

            MySqlParameter[] param =
            [
                new MySqlParameter("p_userID", userId)
            ];

            DataTable userSetting = _databaseService.ExecuteStoredProcedure("GetUserAISettings", param);

            // Überprüfe, ob Daten vorhanden sind
            if(userSetting.Rows.Count == 0)
                return null;

            DataRow row = userSetting.Rows[0];

            UserAiSettings result = new()
            {
                // Annahme: "UserID" ist immer vorhanden und nicht NULL
                UserId = Convert.ToUInt64(row["UserID"]),

                Lora1 = row["lora1"] is not DBNull ? (BildConfigEnums.LoraStack)Convert.ToUInt32(row["lora1"]) : null,
                Strength1 = row["strength1"] is not DBNull ? Convert.ToDouble(row["strength1"]) : null,

                Lora2 = row["lora2"] is not DBNull ? (BildConfigEnums.LoraStack)Convert.ToUInt32(row["lora2"]) : null,
                Strength2 = row["strength2"] is not DBNull ? Convert.ToDouble(row["strength2"]) : null,

                Lora3 = row["lora3"] is not DBNull ? (BildConfigEnums.LoraStack)Convert.ToUInt32(row["lora3"]) : null,
                Strength3 = row["strength3"] is not DBNull ? Convert.ToDouble(row["strength3"]) : null,

                Lora4 = row["lora4"] is not DBNull ? (BildConfigEnums.LoraStack)Convert.ToUInt32(row["lora4"]) : null,
                Strength4 = row["strength4"] is not DBNull ? Convert.ToDouble(row["strength4"]) : null,

                Lora5 = row["lora5"] is not DBNull ? (BildConfigEnums.LoraStack)Convert.ToUInt32(row["lora5"]) : null,
                Strength5 = row["strength5"] is not DBNull ? Convert.ToDouble(row["strength5"]) : null,

                Model = row["model"] is not DBNull ? (BildConfigEnums.Modelle)Convert.ToUInt32(row["model"]) : null,
                Cfg = row["cfg"] is not DBNull ? Convert.ToDouble(row["cfg"]) : null,
                LoadedDateTime = DateTime.Now
            };
            lock(UserSettings)
            {
                UserSettings[userId] = result;
            }
            return result;
        }

        private Task GenerateAiPicture(SocketSlashCommand command)
        {
            ModalBuilder modalBuilder = new();

            modalBuilder.WithTitle("AI Bild Erstellung");

            return Task.CompletedTask;
        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            // Registriere den Slash-Befehl
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                                                             .WithName("generate_picture")
                                                             .WithDescription("Generiert ein KI Bild (Achtung Service ist nicht immer verfügbar)")
                                                             .Build());

            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                                                             .WithName("setting_aigenrating")
                                                             .WithDescription("Einstellungen zur Bilder Herstellung, Wie lora und model")
                                                             .Build());

            await RegisterModul(nameof(ComfyUiGenerating));
        }

        public bool IsActive(ulong channelId, string moduleName)
        {
            MySqlParameter[] isActivePara =
            [
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleName)
            ];

            const string getActive = "" +
                                     "SELECT isActive " +
                                     " FROM view_channel_module_status " +
                                     " WHERE ChannelID = @IDChannel" +
                                     " AND ModuleName = @NameModul;";

            DataTable rows = _databaseService.ExecuteSqlQuery(getActive, isActivePara);

            return (bool)rows.Rows[0]["isActive"];
        }

        public Task RegisterCommandsAsync(SlashCommandHandler commandHandler)
        {
            commandHandler.RegisterModule("generate_picture", this);
            commandHandler.RegisterModule("setting_aigenrating", this);
            return Task.CompletedTask;
        }

        public Task RegisterModul(string modulename)
        {
            MySqlParameter[] parameter =
            [
                new MySqlParameter("@NameModul", modulename),
                new MySqlParameter("@ServerModulIs", true),
                new MySqlParameter("@ChannelModulIs", false)
            ];

            const string query = "SELECT * FROM discord_module WHERE ModuleName = @NameModul";

            DataTable Modules = _databaseService.ExecuteSqlQuery(query, parameter);

            if(Modules.Rows.Count > 0)
            {
                string moduleNameInDB = Modules!.Rows[0]["ModuleName"]!.ToString()!;
                if(moduleNameInDB!.Equals(modulename, StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine($"Modul ({modulename}) in DB");
                }
            }
            else
            {
                const string insert = "INSERT INTO discord_module (ModuleName , IsServerModul , IsChannelModul ) VALUES (@NameModul , @ServerModulIs , @ChannelModulIs )";
                _databaseService.ExecuteSqlQuery(insert, parameter);
                System.Console.WriteLine($"Modul {modulename} der DB hinzugefügt");
            }
            return Task.CompletedTask;
        }

        public Task HandleButtonAsync(SocketMessageComponent component)
        {
            string customID = component.Data.CustomId;

            string[] parts = customID.Split('_');
            string buttonType = $"{parts[0]}_{parts[1]}";

            switch(buttonType)
            {
                case "change_lora":
                    _ = SelectKategorieAsync(component, customID);
                    break;

                case "change_loraModel":
                    _ = SettingAi(component);
                    break;
            }

            return Task.CompletedTask;
        }

        private static async Task SelectKategorieAsync(SocketMessageComponent component, string customID)
        {
            SelectMenuBuilder selectMenuBuilder = new();
            string[] parts = customID.Split("_");
            selectMenuBuilder.WithPlaceholder($"Bitte wähle die Kategorie aus für {parts[1]} nummer {parts[2]}")
                             .WithCustomId($"selected_cat_{parts[2]}");

            Dictionary<string, List<string>> loraStackCategories = EnumExtensions.GetEnumsGroupedByCategory<BildConfigEnums.LoraStack>();

            foreach(KeyValuePair<string, List<string>> dataCategory in loraStackCategories)
            {
                selectMenuBuilder.AddOption(dataCategory.Key, dataCategory.Key);
            }

            ComponentBuilder? builder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

            await component.RespondAsync($"Wähle eine Kategorie für {parts[1]} Nummer {parts[2]}", components: builder.Build(), ephemeral: true);
        }

        Task IButtonModule.RegisterButtonAsync(ButtonCommandHandler commandHandler)
        {
            for(int i = 1; i <= 5; i++)
            {
                commandHandler.RegisterButtonModul($"change_lora_{i}", this);
                commandHandler.RegisterButtonModul($"change_loraModel_{i}", this);
            }

            commandHandler.RegisterButtonModul("change_model_1", this);

            return Task.CompletedTask;
        }

        public Task HandleMenuSelectionAsync(SocketMessageComponent component)
        {
            string[] part = component.Data.CustomId.Split("_");

            _ = $"{part[0]}_{part[1]}" switch
            {
                "selected_cat" => GenerateCatSelectionAsync(component),
                "change_loraModel" => SetLoraModel(component, component.Data.CustomId)
            };
            return Task.CompletedTask;
        }

        private async Task SetLoraModel(SocketMessageComponent component, string LoraModelString)
        {
            string[] parts = LoraModelString.Split("_");
            int loraNumber = int.Parse(parts[2]);
            ulong userId = component.User.Id;
            LoraStack selectedLoraModel = Enum.Parse<LoraStack>(component.Data.Values.First());

            lock(UserSettings)
            {
                if(!UserSettings.TryGetValue(userId, out UserAiSettings? userSettings))
                {
                    component.DeferAsync(true);
                    component.FollowupAsync("Fehler in der Verarbeitung");
                    return;
                }
                else
                {
                    switch(loraNumber)
                    {
                        case 1:
                            userSettings.Lora1 = selectedLoraModel;
                            break;

                        case 2:
                            userSettings.Lora2 = selectedLoraModel;
                            break;

                        case 3:
                            userSettings.Lora3 = selectedLoraModel;
                            break;

                        case 4:
                            userSettings.Lora4 = selectedLoraModel;
                            break;

                        case 5:
                            userSettings.Lora5 = selectedLoraModel;
                            break;

                        default:
                            break;
                    }
                }
            }
            _ = SettingAi(component);
        }

        private static async Task GenerateCatSelectionAsync(SocketMessageComponent component)
        {
            await component.DeferAsync(ephemeral: true);

            Dictionary<string, List<string>> loras = EnumExtensions.GetEnumsGroupedByCategory<LoraStack>();
            SelectMenuBuilder selectMenuBuilder = new();

            string[] parts = component.Data.CustomId.Split("_");

            // Ausgabe aller getroffenen Auswahlmöglichkeiten
            foreach(string? selectedValue in component.Data.Values)
            {
                selectMenuBuilder.WithPlaceholder($"Bitte wähle nun das Lora Modell das du verwenden möchtest.")
                                 .WithCustomId($"change_loraModel_{parts[2]}");

                foreach(string lorModel in loras[selectedValue])
                {
                    LoraStack LoraValue = (LoraStack)Enum.Parse(typeof(LoraStack), lorModel);
                    string? desc = LoraValue.GetAiDesc();
                    selectMenuBuilder.AddOption(lorModel, LoraValue.ToString(), desc);
                }
            }

            ComponentBuilder? builder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

            await component.FollowupAsync("Wähle das Lora Model was du Benutzen willst", components: builder.Build(), ephemeral: true);
        }

        public Task RegisterSelectionHandlerAsync(MenuSelectionHandler commandHandler)
        {
            commandHandler.RegisterSelectionModul("selected_cat", this);
            commandHandler.RegisterSelectionModul("change_loraModel", this);

            return Task.CompletedTask;
        }
    }
}