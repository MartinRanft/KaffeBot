using System.Data;
using System.Net.Http.Json;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Models.Api.NAS;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Newtonsoft.Json;

namespace KaffeBot.Discord.BotOwner
{
    /// <summary>
    /// Represents a class for handling AI picture to channel functionality.
    /// </summary>
    public class AiPicToChanel(DiscordSocketClient client, IDatabaseService databaseService) : InteractionModuleBase<SocketInteractionContext>, IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;
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

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("ai_bilder")
                .WithDescription("AI Bilder Function")
                .Build());

            await RegisterModul(nameof(AiPicToChanel).ToString());
        }

        private async Task SendAiPicToChannel(SocketInteraction command)
        {
            _ = command.DeferAsync(true);
            SocketUser? user = command.User;
            ISocketMessageChannel? channel = command.Channel;

            if(IsActive(channel.Id, GetType().Name))
            {
                HttpClient client = new();
                const string apiBase = "https://api.bytewizards.de/";

                MySqlParameter[] parameter =
                [
                new MySqlParameter("@user_id", user.Id),
                ];

                DataTable UserData = _databaseService.ExecuteStoredProcedure("GetDiscordUserDetails", parameter);

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
                            foreach(FtpDataModel file in data!)
                            {
                                using MemoryStream ms = new(file.Data!);
                                // Der Name der Datei, die an Discord gesendet wird
                                string? fileName = file.FileName;
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
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleName)
            ];

            string getActive = "" +
                "SELECT isActive " +
                " FROM view_channel_module_status " +
                " WHERE ChannelID = @IDChannel" +
                " AND ModuleName = @NameModul;";

            DataTable rows = _databaseService.ExecuteSqlQuery(getActive, isActivePara);

            return (bool)rows.Rows[0]["isActive"];
        }

        public Task RegisterModul(string modulename)
        {
            MySqlParameter[] parameter =
            [
                new MySqlParameter("@NameModul", modulename),
                new MySqlParameter("@ServerModulIs", true),
                new MySqlParameter("@ChannelModulIs", true)
            ];

            const string query = "SELECT * FROM discord_module WHERE ModuleName = @NameModul";

            DataTable Modules = _databaseService.ExecuteSqlQuery(query, parameter);

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
                const string insert = "INSERT INTO discord_module (ModuleName , IsServerModul , IsChannelModul ) VALUES (@NameModul , @ServerModulIs , @ChannelModulIs )";
                _databaseService.ExecuteSqlQuery(insert, parameter);
                Console.WriteLine($"Modul {modulename} der DB hinzugefügt");
            }
            return Task.CompletedTask;
        }

        public Task RegisterCommandsAsync(SlashCommandHandler commandHandler)
        {
            commandHandler.RegisterModule("ai_bilder", this);
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketSlashCommand command)
        {
            if(command.Data.Name == "ai_bilder")
            {
                await SendAiPicToChannel(command);
            }
        }
    }
}