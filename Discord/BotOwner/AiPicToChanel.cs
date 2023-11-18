using System.Net.Http.Json;
using System.Threading.Channels;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Models.Api.NAS;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Newtonsoft.Json;

namespace KaffeBot.Discord.BotOwner
{
    public class AiPicToChanel(DiscordSocketClient client, IDatabaseService databaseService) : InteractionModuleBase<SocketInteractionContext>, IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;
        public bool ShouldExecuteRegularly { get; set; } = false;

        public Task ActivateAsync(ulong channelId, string moduleName)
        {
            MySqlParameter[] isActivePara =
            [
                new("@IDChannel", channelId),
                new("@NameModul", moduleName),
                new("@IsActive", true)
            ];

            _ = _databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);

            return Task.CompletedTask;
        }

        public Task DeactivateAsync(ulong channelId, string moduleName)
        {
            MySqlParameter[] isActivePara =
            [
                new("@IDChannel", channelId),
                new("@NameModul", moduleName),
                new("@IsActive", false)
            ];

            _ = _databaseService.ExecuteStoredProcedure("SetModuleStateByName", isActivePara);
            return Task.CompletedTask;
        }

        public Task Execute(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("ai_bilder")
                .WithDescription("AI Bilder Function")
                .Build());

            _client.SlashCommandExecuted += HandleSlashCommandAsync;

            await RegisterModul(nameof(AiPicToChanel).ToString());
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            switch(command.Data.Name)
            {
                case "ai_bilder":
                    await SendAIPicToChannel(command);
                    break;
            }
        }

        [SlashCommand("ai_bilder", "AI Bilder Function")]
        private async Task SendAIPicToChannel(SocketSlashCommand command)
        {
            _ = command.DeferAsync(true);
            var user = command.User;
            var channel = command.Channel;

            if(IsActive(channel.Id,GetType().Name))
            {
                HttpClient client = new();
                var apiBase = "https://api.bytewizards.de/";

                MySqlParameter[] parameter =
                [
                new("@user_id", user.Id),
                ];

                var UserData = _databaseService.ExecuteStoredProcedure("GetDiscordUserDetails", parameter);

                if(UserData.Rows.Count > 0 && (bool)UserData.Rows[0]["isAdmin"])
                {
                    List<string>? picList = null;
                    using(HttpRequestMessage request = new(HttpMethod.Get, apiBase + "api/nas/pics"))
                    {
                        request.Headers.Add("ApiKey", UserData.Rows[0]["ApiKey"].ToString());
                        HttpResponseMessage response = await client.SendAsync(request);

                        response.EnsureSuccessStatusCode();

                        picList = await response.Content.ReadFromJsonAsync<List<string>>();
                    }

                    if(picList!.Count == 0 || picList is null)
                    {
                        await command.FollowupAsync("Keine Bilder vorhanden.", ephemeral: true);
                    }
                    else
                    {
                        using HttpRequestMessage request = new(HttpMethod.Post, apiBase + "api/nas/getFiles");
                        request.Headers.Add("ApiKey", UserData.Rows[0]["ApiKey"].ToString());
                        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                        string jsonContent = JsonConvert.SerializeObject(picList);
                        request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.SendAsync(request);

                        response.EnsureSuccessStatusCode();

                        List<FtpDataModel>? data = await response.Content.ReadFromJsonAsync<List<FtpDataModel>>();

                        if(channel is SocketTextChannel)
                        {
                            foreach(var file in data!)
                            {
                                using var ms = new MemoryStream(file.Data!);
                                // Der Name der Datei, die an Discord gesendet wird
                                var fileName = file.FileName;
                                // Sende die Datei im Discord-Kanal
                                await channel.SendFileAsync(ms, fileName);
                            }
                            await command.FollowupAsync("Alle AI-Bilder wurden gepostet.", ephemeral: true);
                        }
                        else
                        {
                            await command.FollowupAsync("Dieser Befehl kann nur in Textkanälen verwendet werden.", ephemeral: true);
                        }
                    }
                }
                else
                {
                    await command.FollowupAsync("Sie haben nicht die Berechtigung für diesen Command.", ephemeral: true);
                }
            }
            else
            {
                await command.FollowupAsync("Dieser Befehl ist für diesen Channel nicht verfügbar.");
            }
        }

        public bool IsActive(ulong channelId, string moduleName)
        {

            MySqlParameter[] isActivePara =
            [
                new("@IDChannel", channelId),
                new("@NameModul", moduleName)
            ];

            string getActive = "" +
                "SELECT isActive " +
                " FROM view_channel_module_status " +
                " WHERE ChannelID = @IDChannel" +
                " AND ModuleName = @NameModul;";

            var rows = _databaseService.ExecuteSqlQuery(getActive, isActivePara);

            return (bool)rows.Rows[0]["isActive"];
        }

        public Task RegisterModul(string modulename)
        {
            MySqlParameter[] parameter =
            [
                new("@NameModul", modulename)
            ];

            string query = "SELECT * FROM discord_module WHERE ModuleName = @NameModul";

            var Modules = _databaseService.ExecuteSqlQuery(query, parameter);

            if(Modules.Rows.Count > 0)
            {
                string moduleNameInDB = Modules!.Rows[0]["ModuleName"]!.ToString()!;
                if(moduleNameInDB!.Equals(modulename, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Modul ({modulename}) in DB");
                }
            }
            else
            {
                string insert = "INSERT INTO discord_module (ModuleName) VALUES (@NameModul)";
                _databaseService.ExecuteSqlQuery(insert, parameter);
                Console.WriteLine($"Modul {modulename} der DB hinzugefügt");
            }
            return Task.CompletedTask;
        }
    }
}