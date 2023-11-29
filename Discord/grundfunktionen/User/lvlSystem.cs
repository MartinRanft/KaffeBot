﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Models.UserStat;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.User
{
    internal partial class LvlSystem(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;
        private List<UserStatModel> UserStat = [];
        private Timer? _syncTimer;

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
            await LoadStatData();
            // Registriere den Slash-Befehl
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("stat")
                .WithDescription("Zeigt dir deine Statistik und lvl an.")
                .Build());

            client.MessageReceived += StatGenerating;
            client.UserIsTyping += AddUserToStat;

            await RegisterModul(nameof(LvlSystem));
            
            _syncTimer = new Timer(SyncWithDatabase, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private Task LoadStatData()
        {
            string query = "SELECT * FROM `discord_user_stat_view`";

            DataTable data = _databaseService.ExecuteSqlQuery(query, []);

            if(data.Rows.Count > 0)
            {
                foreach(DataRow dataRow in data.Rows)
                {
                    UserStatModel model = new()
                    {
                        DiscordID = (ulong)dataRow["DiscordUserID"],
                        DiscordServerID = (ulong)dataRow["DiscordServerID"],
                        Birthday = (DateTime?)dataRow["Birthday"],
                        InternServerID = (int)dataRow["InternServerID"],
                        DBUserID = (ulong)dataRow["InternID"],
                        ImageCount = (int)dataRow["PicCount"],
                        UrlCount = (int)dataRow["UrlCount"],
                        WordCount = (int)dataRow["WordCount"]
                    };
                    UserStat.Add(model);
                }
            }
            return Task.CompletedTask;
        }

        private void SyncWithDatabase(object? state)
        {
            foreach(UserStatModel UserStat in UserStat)
            {
                MySqlParameter[] insertData = [
                    new("@p_UserID", UserStat.DBUserID),
                    new("p_DiscordServer", UserStat.InternServerID),
                    new("p_WordCount", UserStat.WordCount),
                    new("p_PicCount", UserStat.ImageCount),
                    new("p_UrlCount", UserStat.UrlCount),
                    new("p_Birthday", UserStat.Birthday)
                    ];

                _ = _databaseService.ExecuteStoredProcedure("UpdateOrInsertUserStat", insertData);
            }
        }

        private async Task AddUserToStat(Cacheable<IUser, ulong> cacheableUser, Cacheable<IMessageChannel, ulong> cacheableChannel)
        {
            if(cacheableUser.HasValue)
            {
                ulong userId = cacheableUser.Id;

                IMessageChannel channel = await cacheableChannel.GetOrDownloadAsync();
                ulong serverId = 0;

                if(channel is SocketGuildChannel guildChannel)
                {
                    serverId = guildChannel.Guild.Id;
                }
                else
                {
                    System.Console.WriteLine("ERROR: Not a guild channel");
                    return;
                }

                var existingUserStat = UserStat.FirstOrDefault(u => u.DiscordID == userId);
                if(existingUserStat != null)
                {
                    return;
                }

                DataTable userData = _databaseService.ExecuteStoredProcedure("GetDiscordUserDetails", [new MySqlParameter("@user_id", userId)]);
                if(userData.Rows.Count > 0)
                {
                    DataRow userDataRow = userData.Rows[0];

                    var outParameter = new MySqlParameter
                    {
                        ParameterName = "@p_DbId",
                        MySqlDbType = MySqlDbType.Int32,
                        Direction = ParameterDirection.Output
                    };

                    _databaseService.ExecuteStoredProcedure("GetServerDbId",
                        [
                    new MySqlParameter("@p_ServerID", serverId),
                    outParameter
                        ]);

                    if(outParameter.Value != DBNull.Value && int.TryParse(outParameter.Value!.ToString(), out int serverDataRow))
                    {
                        MySqlParameter[] userstatParameters = [
                            new MySqlParameter("@internID", int.Parse(userDataRow["ID"].ToString()!)),
                            new MySqlParameter("@internServer", serverDataRow)
                        ];

                        string query = "SELECT * FROM discord_user_static WHERE UserID = @internID AND DiscordServer = @internServer";
                        DataTable userStatTable = _databaseService.ExecuteSqlQuery(query, userstatParameters);

                        UserStatModel addUser;
                        if(userStatTable.Rows.Count > 0)
                        {
                            DataRow userStatRow = userStatTable.Rows[0];
                            addUser = new UserStatModel
                            {
                                DiscordID = userId,
                                DBUserID = ulong.Parse(userDataRow["ID"].ToString()!),
                                DiscordServerID = serverId,
                                InternServerID = serverDataRow,
                                UrlCount = (int)userStatRow["UrlCount"],
                                ImageCount = (int)userStatRow["PicCount"],
                                WordCount = (int)userStatRow["WordCount"],
                                Birthday = userStatRow["Birthday"] == DBNull.Value ? null : (DateTime?)userStatRow["Birthday"]
                            };
                        }
                        else
                        {
                            addUser = new UserStatModel
                            {
                                DiscordID = userId,
                                DBUserID = ulong.Parse(userDataRow["ID"].ToString()!),
                                DiscordServerID = serverId,
                                InternServerID = serverDataRow,
                                UrlCount = 0,
                                ImageCount = 0,
                                WordCount = 0,
                                Birthday = null
                            };
                        }

                        UserStat.Add(addUser);
                    }
                    else
                    {
                        System.Console.WriteLine("ERROR: Server not found in DB");
                    }
                }
                else
                {
                    System.Console.WriteLine("ERROR: User not found in DB");
                }
            }
        }

        private Task StatGenerating(SocketMessage message)
        {
            if(message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            int linkCount = CountLink(message.Content);
            int imageCount = message.Attachments.Count(att => IsImage(att));
            int WordCount = CountWords(message.Content);

            ulong discordId = message.Author.Id;

            UserStatModel userStat = UserStat.FirstOrDefault(u => u.DiscordID == discordId)!;

            if(userStat != null)
            {
                // Benutzer gefunden, aktualisieren Sie seine Statistiken
                userStat.UrlCount += linkCount;
                userStat.ImageCount += imageCount;
                userStat.WordCount += WordCount;
            }
            else
            {
                UserStatModel newUserStat = new()
                {
                    DiscordID = discordId,
                    DBUserID = 0, // Setzen Sie die DBUserID entsprechend
                    ImageCount = imageCount,
                    UrlCount = linkCount,
                    WordCount = WordCount,
                    Birthday = null
                };

                UserStat.Add(newUserStat);
            }
            return Task.CompletedTask;
        }

        private int CountWords(string content)
        {
            string contentWithoutUrls = UrlRegEx().Replace(content, "");

            char[] delimiters = [' ', '\r', '\n', '\t'];
            string[] words = contentWithoutUrls.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            int WordCount = 0;

            foreach(string word in words)
            {
                if(word.Length > 1)
                {
                    WordCount++;
                }
            }
            return WordCount;
        }

        private bool IsImage(global::Discord.Attachment attachment)
        {
            return attachment.Filename.EndsWith(".png") ||
            attachment.Filename.EndsWith(".jpg") ||
            attachment.Filename.EndsWith(".jpeg") ||
            attachment.Filename.EndsWith(".webp") ||
            attachment.Filename.EndsWith(".webm") ||
            attachment.Filename.EndsWith(".gif");
        }

        private int CountLink(string content)
        {
            var urlRegex = UrlRegEx();
            var matches = urlRegex.Matches(content);
            return matches.Count;
        }

        public async Task HandleCommandAsync(SocketSlashCommand command)
        {
            // Überprüfen, ob der ausgeführte Befehl der ist, den dieses Modul behandelt
            await (command.Data.Name switch
                {
                    "stat" => SendUserStatsToDiscord(command),
                    _ => Task.CompletedTask
                }).ConfigureAwait(false);
        }

        private async Task SendUserStatsToDiscord(SocketSlashCommand command)
        {
            // Identifizieren Sie die Discord-ID des Benutzers, der den Befehl ausgelöst hat
            ulong userId = command.User.Id;
            ulong ServId = command.GuildId!.Value;

            // Suchen Sie die Statistiken des Benutzers
            UserStatModel? userStats = UserStat.FirstOrDefault(u => u.DiscordID == userId && u.DiscordServerID == ServId);


            if(userStats != null)
            {
                // Formatieren Sie die Nachricht
                var statsMessage = $"**Deine Statistiken:**\n" +
                                   $"- Wörter gezählt: {userStats.WordCount}\n" +
                                   $"- Bilder gepostet: {userStats.ImageCount}\n" +
                                   $"- URLs geteilt: {userStats.UrlCount}";

                // Erstellen Sie eine Embed-Nachricht für bessere Formatierung
                var embed = new EmbedBuilder()
                    .WithTitle("Deine Statistiken")
                    .WithDescription(statsMessage)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Gold)
                    .Build();

                // Senden Sie die Nachricht an den Benutzer
                await command.RespondAsync(embed: embed, ephemeral: true); // Ephemeral bedeutet, dass nur der anfragende Benutzer die Antwort sieht
            }
            else
            {
                // Falls keine Statistiken gefunden wurden
                await command.RespondAsync("Es wurden keine Statistiken für dich gefunden.", ephemeral: true);
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

            if(rows.Rows.Count > 0)
            {
                return (bool)rows.Rows[0]["isActive"];
            }
            else
            {
                // Keine Daten gefunden, kehre mit einem Standardwert zurück oder handle den Fall entsprechend
                return false;
            }
        }

        public Task RegisterModul(string modulename)
        {
            MySqlParameter[] parameter =
            [
                new("@NameModul", modulename),
                new("@ServerModulIs", true),
                new("@ChannelModulIs", false)
            ];

            string query = "SELECT * FROM discord_module WHERE ModuleName = @NameModul";

            var Modules = _databaseService.ExecuteSqlQuery(query, parameter);

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
                string insert = "INSERT INTO discord_module (ModuleName, IsServerModul, IsChannelModul) VALUES (@NameModul , @ServerModulIs , @ChannelModulIs )";
                _databaseService.ExecuteSqlQuery(insert, parameter);
                System.Console.WriteLine($"Modul {modulename} der DB hinzugefügt");
            }
            return Task.CompletedTask;
        }

        [GeneratedRegex(@"https?:\/\/[\w-]+(\.[\w-]+)+[\/\w- .]*\??[\w-=&%]*")]
        private static partial Regex UrlRegEx();

        public Task RegisterCommandsAsync(SlashCommandHandler commandHandler)
        {
            commandHandler.RegisterModule("stat", this);
            return Task.CompletedTask;
        }
    }
}