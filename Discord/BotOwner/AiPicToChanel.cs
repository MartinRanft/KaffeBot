using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

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

using GroupAttribute = Discord.Interactions.GroupAttribute;

namespace KaffeBot.Discord.BotOwner
{

    internal class AiPicToChanel : InteractionModuleBase<SocketInteractionContext>, IBotModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IDatabaseService _databaseService;
        private bool _isActive;
        public bool ShouldExecuteRegularly { get; set; }

        public AiPicToChanel(DiscordSocketClient client, IDatabaseService databaseService) 
        {
            _client = client;
            _databaseService = databaseService;
            ShouldExecuteRegularly = false;
        }

        public Task ActivateAsync(ulong serverId)
        {
            _isActive = true;
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(ulong serverId)
        {
            _isActive = false;
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
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
            command.DeferAsync(true);
            var user = command.User;
            HttpClient client = new HttpClient();
            var apiBase = "https://api.bytewizards.de/";

            MySqlParameter[] parameter = new MySqlParameter[]
            {
                new MySqlParameter("@user_id", user.Id),
            };

            var UserData = _databaseService.ExecuteStoredProcedure("GetDiscordUserDetails", parameter);

            if (UserData.Rows.Count > 0 && (bool)UserData.Rows[0]["isAdmin"])
            {
                List<string>? picList = null;
                using(HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiBase + "api/nas/pics"))
                {
                    request.Headers.Add("ApiKey", UserData.Rows[0]["ApiKey"].ToString());
                    HttpResponseMessage response = await client.SendAsync(request);

                    response.EnsureSuccessStatusCode();

                    picList = await response.Content.ReadFromJsonAsync<List<string>>();                    
                }

                if(picList.Count == 0 || picList is null)
                {
                    await command.FollowupAsync("Keine Bilder vorhanden.", ephemeral: true);
                }
                else
                {
                    List<FtpDataModel>? data = null;
                    using(HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiBase + "api/nas/getFiles"))
                    {
                        request.Headers.Add("ApiKey", UserData.Rows[0]["ApiKey"].ToString());
                        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                       
                        string jsonContent = JsonConvert.SerializeObject(picList);
                        request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.SendAsync(request); 
                        
                        response.EnsureSuccessStatusCode();

                        data = await response.Content.ReadFromJsonAsync<List<FtpDataModel>>();

                        if(command.Channel is SocketTextChannel channel)
                        {
                            foreach(var file in data)
                            {
                                using(var ms = new MemoryStream(file.data))
                                {
                                    // Der Name der Datei, die an Discord gesendet wird
                                    var fileName = file.fileName;
                                    // Sende die Datei im Discord-Kanal
                                    await channel.SendFileAsync(ms, fileName);
                                }
                            }
                            await command.FollowupAsync("Alle AI-Bilder wurden gepostet.", ephemeral: true);
                        }
                        else
                        {
                            await command.FollowupAsync("Dieser Befehl kann nur in Textkanälen verwendet werden.", ephemeral: true);
                        }

                    }
                }
            }
            else
            {
                await command.FollowupAsync("Sie haben nicht die Berechtigung für diesen Command.", ephemeral: true);
            }

        }

        public bool IsActive(ulong serverId)
        {
            return _isActive;
        }
    }
}
