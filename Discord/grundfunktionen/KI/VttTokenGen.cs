using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Discord;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Models.KI;
using KaffeBot.Services.DB;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Newtonsoft.Json;

namespace KaffeBot.Discord.grundfunktionen.KI
{
    /// <summary>
    /// Represents a class for generating VTT tokens.
    /// </summary>
    internal class VttTokenGen(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        public bool ShouldExecuteRegularly { get; set; } = false;

        public Task ActivateAsync(ulong channelId, string moduleName)
        {
            MySqlParameter[] isActivePara =
            [
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleName),
                new MySqlParameter("@IsActive", true)
            ];

            databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);

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

            databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);

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
                "gentoken" => GenerateVTTToken(command),
                _ => Task.CompletedTask
            }).ConfigureAwait(false);
        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                                                             .WithName("gentoken")
                                                             .WithDescription("Generiert einen Token basierend auf einem einfachen Prompt")
                                                             .AddOption("prompt", ApplicationCommandOptionType.String, "Der Prompt, welcher zur Generierung des Tokens genutzt wird", isRequired: true)
                                                             .Build());

            await RegisterModul(nameof(VttTokenGen));
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

            DataTable rows = databaseService.ExecuteSqlQuery(getActive, isActivePara);

            return (bool)rows.Rows[0]["isActive"];
        }

        public Task RegisterCommandsAsync(SlashCommandHandler commandHandler)
        {
            commandHandler.RegisterModule("gentoken", this);
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

            DataTable modules = databaseService.ExecuteSqlQuery(query, parameter);

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
                databaseService.ExecuteSqlQuery(insert, parameter);
                System.Console.WriteLine($"Modul {modulename} der DB hinzugefügt");
            }
            return Task.CompletedTask;
        }
        
        private async Task GenerateVTTToken(SocketSlashCommand command)
        {
            List<ulong> serverList = [715296154212106411, 1175142792494841907, 483521710709014538];

            if(!serverList.Contains(command.GuildId.Value))
            {
                await command.RespondAsync("Dieser Befehl ist auf diesem Server nicht verfügbar. Bitte wenden Sie sich an den Bot Owner für eine Freischaltung", ephemeral: true);
                return;
            }

            // Extrahiere die Werte aus den Optionen
            string? promptInput = (string)command.Data.Options.FirstOrDefault(o => o.Name == "prompt")?.Value!;

            VTTTokenSettings question = new()
            {
                UserId = command.User.Id.ToString(),
                PositivePrompt = promptInput
            };

            await command.DeferAsync(ephemeral: false);

            HttpClient httpClient = new();

            string apiUrl = "https://api.bytewizards.de/api/bildgen/GenerateVTTToken";
            
            string jsonData = JsonConvert.SerializeObject(question);
            HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                List<string>? base64Images = JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());
                HashSet<string> uniqueImages = [..base64Images];
                List<FileAttachment> fileAttachments = [];
                short iter = 1;
                
                foreach(byte[] imageData in uniqueImages.Select(base64Image => Convert.FromBase64String(base64Image)))
                {
                    MemoryStream stream = new(imageData);
                    FileAttachment fileAttachment = new(stream, "generated_image_" + iter + ".png");
                    fileAttachments.Add(fileAttachment);
                    iter++;
                }

                await command.FollowupWithFilesAsync(fileAttachments.AsEnumerable());
                GC.Collect();
            }
            else
            {
                // Handle the case where the API call failed
                await command.FollowupAsync("Failed to generate image. Please try again later.");
            }
        }
    }
}
