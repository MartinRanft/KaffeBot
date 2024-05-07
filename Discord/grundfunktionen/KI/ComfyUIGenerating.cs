using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
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

using Newtonsoft.Json;

using static KaffeBot.Models.KI.Enums.BildConfigEnums;
using System.Linq;

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

            _databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);

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

            _databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);

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
                //suchen der User die gelöscht werden sollen aus dem speicher mit Ihren einstellungen.
                if(UserSettings.TryGetValue(userId, out UserAiSettings? userAiSettings))
                {
                    MySqlParameter[] parameters =
                    [
                        new MySqlParameter("@_UserID", userAiSettings.UserId),
                        new MySqlParameter("@_lora1", userAiSettings.Lora1),
                        new MySqlParameter("@_strength1", userAiSettings.Strength1),
                        new MySqlParameter("@_lora2", userAiSettings.Lora2),
                        new MySqlParameter("@_strength2", userAiSettings.Strength2),
                        new MySqlParameter("@_lora3", userAiSettings.Lora3),
                        new MySqlParameter("@_strength3", userAiSettings.Strength3),
                        new MySqlParameter("@_lora4", userAiSettings.Lora4),
                        new MySqlParameter("@_strength4", userAiSettings.Strength4),
                        new MySqlParameter("@_lora5", userAiSettings.Lora5),
                        new MySqlParameter("@_strength5", userAiSettings.Strength5),
                        new MySqlParameter("@_model", userAiSettings.Model),
                        new MySqlParameter("@_cfg", userAiSettings.Cfg)
                    ];

                    _databaseService.ExecuteStoredProcedure("UpdateUserAISettings", parameters);
                }
                // Diese Variable wird nicht verwendet, aber TryRemove erfordert sie.
                UserSettings.TryRemove(userId, out UserAiSettings? removedValue);
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

            UserAiSettings? userSetting = LoadSettings(userId);

            EmbedBuilder embed = new();
            ComponentBuilder component = new();

            embed.WithTitle("Deine aktuellen AI Einstellungen")
                 .WithColor(Color.DarkBlue);

            if(userSetting is not null)
            {
                List<LoraStack?> loras = [userSetting.Lora1, userSetting.Lora2, userSetting.Lora3, userSetting.Lora4, userSetting.Lora5];

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

            UserAiSettings? userSetting = LoadSettings(userId);

            EmbedBuilder embed = new();
            ComponentBuilder component = new();

            embed.WithTitle("Deine aktuellen AI Einstellungen")
                 .WithColor(Color.DarkBlue);

            if(userSetting is not null)
            {
                List<LoraStack?> loras = [userSetting.Lora1, userSetting.Lora2, userSetting.Lora3, userSetting.Lora4, userSetting.Lora5];

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

        private UserAiSettings? LoadSettings(ulong userId)
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

            UserAiSettings result;

            // Überprüfe, ob Daten vorhanden sind
            if(userSetting.Rows.Count != 0)
            {

                DataRow row = userSetting.Rows[0];

                result = new UserAiSettings
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
            }
            else
            {
                result = new UserAiSettings()
                {
                    UserId = userId,
                    Cfg = 0.7,

                    Lora1 = LoraStack.AddDetail,
                    Strength1 = 0.0,

                    Lora2 = LoraStack.Alucardserasintegra,
                    Strength2 = 0.0,

                    Lora3 = LoraStack.AssLickingV2,
                    Strength3 = 0.0,

                    Lora4 = LoraStack.CGgamebuttonbsw,
                    Strength4 = 0.0,

                    Lora5 = LoraStack.Button,
                    Strength5 = 0.0,

                    Model = Modelle.Dreamshaper8Nsfw,

                    LoadedDateTime = DateTime.Now
                };
            }
            lock(UserSettings)
            {
                UserSettings[userId] = result;
            }
            return result;
        }

        private async Task GenerateAiPicture(SocketSlashCommand command)
        {
            List<ulong> serverList = [715296154212106411, 1175142792494841907, 483521710709014538];

            if(!serverList.Contains(command.GuildId.Value))
            {
                await command.RespondAsync("Dieser Befehl ist auf diesem Server nicht verfügbar. Bitte wenden Sie sich an den Bot Owner für eine Freischaltung", ephemeral: true);
                return;
            }

            // Extrahiere die Werte aus den Optionen
            string? positiveInput = (string)command.Data.Options.FirstOrDefault(o => o.Name == "positive")?.Value!;
            string? negativeInput = (string)command.Data.Options.FirstOrDefault(o => o.Name == "negative")?.Value!;
            UserAiSettings setting = LoadSettings(command.User.Id)!;

            ImagesSettings question = new()
            {
                UserAiSettings = setting,
                PositivePrompt = positiveInput,
                NegativePrompt = negativeInput,
            };

            await command.DeferAsync(ephemeral: false);

            HttpClient httpClient = new();

            string apiUrl = "https://api.bytewizards.de/api/bildgen/GeneratePic";

            string jsonData = JsonConvert.SerializeObject(question);
            HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                List<string>? base64Images = JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());
                HashSet<string> uniqueImages = [..base64Images];

                foreach(byte[] imageData in uniqueImages.Select(base64Image => Convert.FromBase64String(base64Image)))
                {
                    using MemoryStream stream = new(imageData);
                    FileAttachment fileAttachment = new(stream, "generated_image.png");

                    EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle("Generated Image")
                    .WithImageUrl("attachment://generated_image.png");

                    builder.AddField("Positive Prompt", $"{positiveInput}", true);
                    builder.AddField("Negative Prompt", $"{negativeInput}", true);
                    builder.AddField("Model Used:", $"{setting.Model.ToString()}", true);
                    

                    await command.FollowupWithFileAsync(fileAttachment, embed: builder.Build());
                }
            }
            else
            {
                // Handle the case where the API call failed
                await command.FollowupAsync("Failed to generate image. Please try again later.");
            }

        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            // Registriere den Slash-Befehl
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                                                             .WithName("generate_picture")
                                                             .WithDescription("Generiert ein KI Bild (Achtung Service ist nicht immer verfügbar)")
                                                             .AddOption("positive", ApplicationCommandOptionType.String, "Positive Attribute, die das Bild beeinflussen sollen", isRequired: true)
                                                             .AddOption("negative", ApplicationCommandOptionType.String, "Negative Attribute, die das Bild beeinflussen sollen", isRequired: true)
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

            DataTable modules = _databaseService.ExecuteSqlQuery(query, parameter);

            if(modules.Rows.Count > 0)
            {
                string moduleNameInDb = modules!.Rows[0]["ModuleName"]!.ToString()!;
                if(moduleNameInDb!.Equals(modulename, StringComparison.OrdinalIgnoreCase))
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
            string customId = component.Data.CustomId;

            string[] parts = customId.Split('_');
            string buttonType = $"{parts[0]}_{parts[1]}";

            object? __ = buttonType switch
            {
                "change_lora" => SelectKategorieAsync(component, customId),
                "change_model" => SelectAiModel(component),
                "change_loraModel" => SettingAi(component),
                _ => ""
            };

            return Task.CompletedTask;
        }

        private static Task SelectAiModel(SocketMessageComponent component)
        {
            SelectMenuBuilder selectMenuBuilder = new();

            // Platzhalter und CustomId für das Auswahlmenü setzen
            selectMenuBuilder.WithPlaceholder("Bitte wähle das Model, das du verwenden möchtest")
                             .WithCustomId("changeModel_1");

            // Durchlaufen aller Modelle-Enum-Werte und Hinzufügen als Optionen zum Menü
            foreach (Modelle model in Enum.GetValues(typeof(Modelle)))
            {
                // Enum-Wert in String umwandeln
                string modelName = model.ToString();

                // Beschreibung aus dem AiAttribut abrufen
                string description = EnumExtensions.GetAiDesc(model);

                // Füge die Option dem Menü hinzu
                selectMenuBuilder.AddOption(modelName,modelName, description: description);
            }

            ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

            // Senden der Nachricht mit dem Auswahlmenü
            return component.RespondAsync("Bitte wähle das Model, das du verwenden möchtest", components: componentBuilder.Build(), ephemeral: true);
        }

        private static async Task SelectKategorieAsync(SocketMessageComponent component, string customID)
        {
            SelectMenuBuilder selectMenuBuilder = new();
            string[] parts = customID.Split("_");
            selectMenuBuilder.WithPlaceholder($"Bitte wähle die Kategorie aus für {parts[1]} nummer {parts[2]}")
                             .WithCustomId($"selected_cat_{parts[2]}");

            Dictionary<string, List<string>> loraStackCategories = EnumExtensions.GetEnumsGroupedByCategory<LoraStack>();

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
                "change_loraModel" => SetLoraModel(component, component.Data.CustomId),
                "changeModel_1" => SetModel(component),
                "change_str" => UserLoraStrng(component),
                _ => throw new NotImplementedException()
            };
            return Task.CompletedTask;
        }

        private Task UserLoraStrng(SocketMessageComponent component)
        {
            ulong userID = component.User.Id;
            string customId = component.Data.CustomId;
            string[] part = customId.Split("_");
            _ = int.TryParse(part[2], out int loraNumber);
            double.TryParse(component.Data.Values.First(), NumberStyles.Any, CultureInfo.InvariantCulture, out double loraStr);

            lock (UserSettings)
            {
                if(!UserSettings.TryGetValue(userID, out UserAiSettings? userSettings))
                {
                    component.DeferAsync(true);
                    component.FollowupAsync("Fehler in der Verarbeitung", ephemeral: true);
                    return Task.CompletedTask;
                }

                switch (loraNumber)
                {
                    case 1:
                        userSettings.Strength1 = loraStr;
                        break;

                    case 2:
                        userSettings.Strength2 = loraStr; 
                        break;

                    case 3:
                        userSettings.Strength3 = loraStr;
                        break;

                    case 4:
                        userSettings.Strength4 = loraStr;
                        break;

                    case 5:
                        userSettings.Strength5 = loraStr;
                        break;
                }

            }

            _ = SettingAi(component);
            return Task.CompletedTask;
        }

        private Task SetModel(SocketMessageComponent component)
        {
            Modelle selectedAiModel = Enum.Parse<Modelle>(component.Data.Values.First());

            lock (UserSettings)
            {
                if(!UserSettings.TryGetValue(component.User.Id, out UserAiSettings? userAiSettings))
                {
                    component.DeferAsync(true);
                    component.FollowupAsync("Fehler in der Verarbeitung", ephemeral: true);
                    return Task.CompletedTask;
                }
                else
                {
                    userAiSettings.Model = selectedAiModel;
                }
            }

            _ = SettingAi(component);
            return Task.CompletedTask;
        }

        private static Task SetLoraModel(SocketMessageComponent component, string LoraModelString)
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
                    return Task.CompletedTask;
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

            _ = SetLoraStrength(component, loraNumber);
            return Task.CompletedTask;
        }

        private static async Task SetLoraStrength(SocketMessageComponent component, int loraNumber)
        {
            await component.DeferAsync(true);

            SelectMenuBuilder selectMenuBuilder = new();

            selectMenuBuilder.WithPlaceholder("Bitte Wähle die % Stärke des einflusses auf das Bild aus")
                             .WithCustomId($"change_str_{loraNumber}");

            for (int i = 0; i <= 10; i++)
            {
                selectMenuBuilder.AddOption($"{i}0%", $"0.{i}0", $"{i}0 % Einfluss auf das Bild");
            }

            ComponentBuilder? builder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

            await component.FollowupAsync("Bitte die Stärke des Lora Angeben", components: builder.Build(), ephemeral: true);

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
            commandHandler.RegisterSelectionModul("changeModel_1", this);
            commandHandler.RegisterSelectionModul("change_str", this);

            return Task.CompletedTask;
        }
    }
}