using System.Data;

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
    public class ComfyUiGenerating(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly DiscordSocketClient _client = client;
        public bool ShouldExecuteRegularly { get; set; } = false;

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
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketSlashCommand command)
        {
            await (command.Data.Name switch
            {
                "generate_picture" => GenerateAIPicture(command),
                "setting_aigenrating" => SettingAi(command),
                _ => Task.CompletedTask
            }).ConfigureAwait(false);
        }

        private async Task SettingAi(SocketSlashCommand command)
        {
            ulong userId = command.User.Id;

            Dictionary<string, List<string>> lorDictionary = [];

            foreach (LoraStack lora in Enum.GetValues(typeof(LoraStack)))
            {
                string? description = lora.GetDescription(); // Nutze die Erweiterungsmethode, um die Description zu bekommen
                
                if(description == null)
                {
                    continue;
                }
                
                // Extrahiere den Ordner aus der Beschreibung
                int lastBackslashIndex = description.LastIndexOf('\\');
                if(lastBackslashIndex <= -1)
                {
                    continue;
                }
                    
                string folder = description[..lastBackslashIndex];
                string enumName = lora.ToString();

                // Überprüfe, ob der Ordner bereits im Dictionary existiert
                if (!lorDictionary.TryGetValue(folder, out List<string>? value))
                {
                    value = ([]);
                    lorDictionary[folder] = value; // Füge einen neuen Eintrag hinzu, falls nicht vorhanden
                }

                value.Add(enumName); // Füge den Enum-Namen zur Liste hinzu
            }

            UserAiSettings? userSetting = LoadSettings(userId);

            EmbedBuilder embed = new();
            ComponentBuilder component = new();
            
            embed.WithTitle("Deine aktuellen AI Einstellungen")
                 .WithColor(Color.DarkBlue);

            if(userSetting is not null)
            {
                AiButtonGen.AddSettingFieldAndButton(embed, component, "Lora 1", userSetting.Lora1, 1);
                AiButtonGen.AddSettingFieldAndButton(embed, component, "Lora 2", userSetting.Lora2, 2);
                AiButtonGen.AddSettingFieldAndButton(embed, component, "Lora 3", userSetting.Lora3, 3);
                AiButtonGen.AddSettingFieldAndButton(embed, component, "Lora 4", userSetting.Lora4, 4);
                AiButtonGen.AddSettingFieldAndButton(embed, component, "Lora 5", userSetting.Lora5, 5);
                AiButtonGen.AddSettingFieldAndButton(embed, component, "Model", userSetting.Model, 5);
            }
            else
            {
                embed.WithDescription("Keine Einstellungen gesetzt.");
            }

            await command.RespondAsync(embed: embed.Build(), components: component.Build(),ephemeral: true);
        }

        private UserAiSettings? LoadSettings(ulong userId)
        {
            MySqlParameter[] param =
            [
                new MySqlParameter("p_userID", userId)
            ];

            DataTable userSetting = _databaseService.ExecuteStoredProcedure("GetUserAISettings", param);

            // Überprüfe, ob Daten vorhanden sind
            if (userSetting.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = userSetting.Rows[0];
            UserAiSettings result = new()
            {
                // Annahme: "UserID" ist immer vorhanden und nicht NULL
                UserId = (long)row["UserID"],

                Lora1 = row["lora1"] != DBNull.Value ? (BildConfigEnums.LoraStack)(uint)row["lora1"] : null,
                Strength1 = row["strength1"] != DBNull.Value ? (double)row["strength1"] : null,
                Lora2 = row["lora2"] != DBNull.Value ? (BildConfigEnums.LoraStack)(uint)row["lora2"] : null,
                Strength2 = row["strength2"] != DBNull.Value ? (double)row["strength2"] : null,
                Lora3 = row["lora3"] != DBNull.Value ? (BildConfigEnums.LoraStack)(uint)row["lora3"] : null,
                Strength3 = row["strength3"] != DBNull.Value ? (double)row["strength3"] : null,
                Lora4 = row["lora4"] != DBNull.Value ? (BildConfigEnums.LoraStack)(uint)row["lora4"] : null,
                Strength4 = row["strength4"] != DBNull.Value ? (double)row["strength4"] : null,
                Lora5 = row["lora5"] != DBNull.Value ? (BildConfigEnums.LoraStack)(uint)row["lora5"] : null,
                Strength5 = row["strength5"] != DBNull.Value ? (double)row["strength5"] : null,
                Model = row["model"] != DBNull.Value ? (BildConfigEnums.Modelle)(uint)row["model"] : null,
                Cfg = row["cfg"] != DBNull.Value ? (double)row["cfg"] : null,
                
            };

            return result;
        }

        private async Task GenerateAIPicture(SocketSlashCommand command)
        {
            ModalBuilder modalBuilder = new();

            modalBuilder.WithTitle("AI Bild Erstellung");

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
    }
}