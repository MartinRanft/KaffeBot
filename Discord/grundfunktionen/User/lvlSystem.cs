using System;
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
        private readonly List<UserStatModel> _userStat = [];
        private Timer? _syncTimer;

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
            const string query = "SELECT * FROM `discord_user_stat_view`";

            DataTable data = _databaseService.ExecuteSqlQuery(query, []);

            if(data.Rows.Count <= 0)
            {
                return Task.CompletedTask;
            }
            
            foreach(DataRow dataRow in data.Rows)
            {
                UserStatModel model = new()
                {
                    DiscordID = (long)dataRow["DiscordUserID"],
                    DiscordServerID = (long)dataRow["DiscordServerID"],
                    Birthday = dataRow["Birthday"] == DBNull.Value ? null : (DateTime?)dataRow["Birthday"],
                    InternServerID = (uint)dataRow["InternServerID"],
                    DBUserID = (uint)dataRow["InternID"],
                    ImageCount = (int)dataRow["PicCount"],
                    UrlCount = (int)dataRow["UrlCount"],
                    WordCount = (int)dataRow["WordCount"]
                };
                _userStat.Add(model);
            }
            return Task.CompletedTask;
        }

        private void SyncWithDatabase(object? state)
        {
            foreach(MySqlParameter[] insertData in _userStat.Select(userStat => (MySqlParameter[])[
                        new MySqlParameter("@p_UserID", userStat.DBUserID),
                        new MySqlParameter("p_DiscordServer", userStat.InternServerID),
                        new MySqlParameter("p_WordCount", userStat.WordCount),
                        new MySqlParameter("p_PicCount", userStat.ImageCount),
                        new MySqlParameter("p_UrlCount", userStat.UrlCount),
                        new MySqlParameter("p_Birthday", userStat.Birthday)
                    ]))
            {
                _ = _databaseService.ExecuteStoredProcedure("UpdateOrInsertUserStat", insertData);
            }
        }

        private async Task AddUserToStat(Cacheable<IUser, ulong> cacheableUser, Cacheable<IMessageChannel, ulong> cacheableChannel)
        {
            
            if(cacheableUser.HasValue)
            {
                IUser user = await cacheableUser.GetOrDownloadAsync();

                if(user.IsBot)
                {
                    return;
                }
                
                long userId = (long)cacheableUser.Id;
                
                IMessageChannel channel = await cacheableChannel.GetOrDownloadAsync();
                long serverId = 0;

                if(channel is SocketGuildChannel guildChannel)
                {
                    serverId = (long)guildChannel.Guild.Id;
                }
                else
                {
                    System.Console.WriteLine("ERROR: Not a guild channel");
                    return;
                }

                UserStatModel? existingUserStat = _userStat.FirstOrDefault(u => u.DiscordID == userId);
                if(existingUserStat != null &&  existingUserStat.DiscordServerID == serverId)
                {
                    return;
                }

                DataTable userData = _databaseService.ExecuteStoredProcedure("GetDiscordUserDetails", [new MySqlParameter("@user_id", userId)]);
                if(userData.Rows.Count > 0)
                {
                    DataRow userDataRow = userData.Rows[0];

                    MySqlParameter outParameter = new()
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

                        const string query = "SELECT * FROM discord_user_static WHERE UserID = @internID AND DiscordServer = @internServer";
                        DataTable userStatTable = _databaseService.ExecuteSqlQuery(query, userstatParameters);

                        UserStatModel addUser;
                        if(userStatTable.Rows.Count > 0)
                        {
                            DataRow userStatRow = userStatTable.Rows[0];
                            addUser = new UserStatModel
                            {
                                DiscordID = userId,
                                DBUserID = uint.Parse(userDataRow["ID"].ToString()!),
                                DiscordServerID = (long)serverId,
                                InternServerID = (uint)serverDataRow,
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
                                DBUserID = uint.Parse(userDataRow["ID"].ToString()!),
                                DiscordServerID = (long)serverId,
                                InternServerID = (uint)serverDataRow,
                                UrlCount = 0,
                                ImageCount = 0,
                                WordCount = 0,
                                Birthday = null
                            };
                        }

                        _userStat.Add(addUser);
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

            long discordServerId = 0;

            if(message.Channel is SocketGuildChannel guildChannel)
            {
                discordServerId = (long)guildChannel.Guild.Id;
            }

            int linkCount = CountLink(message.Content);
            int imageCount = message.Attachments.Count(IsImage);
            int WordCount = CountWords(message.Content);

            ulong discordId = message.Author.Id;

            UserStatModel userStat = _userStat.FirstOrDefault(u => u.DiscordID == (long)discordId && u.DiscordServerID == discordServerId)!;

            // Benutzer gefunden, aktualisieren Sie seine Statistiken
            userStat.UrlCount += linkCount;
            userStat.ImageCount += imageCount;
            userStat.WordCount += WordCount;
            return Task.CompletedTask;
        }

        private static int CountWords(string content)
        {
            string contentWithoutUrls = UrlRegEx().Replace(content, "");

            char[] delimiters = [' ', '\r', '\n', '\t'];
            string[] words = contentWithoutUrls.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            return words.Count(word => word.Length > 1);
        }

        private static bool IsImage(global::Discord.Attachment attachment)
        {
            return attachment.Filename.EndsWith(".png") ||
            attachment.Filename.EndsWith(".jpg") ||
            attachment.Filename.EndsWith(".jpeg") ||
            attachment.Filename.EndsWith(".webp") ||
            attachment.Filename.EndsWith(".webm") ||
            attachment.Filename.EndsWith(".gif");
        }

        private static int CountLink(string content)
        {
            Regex urlRegex = UrlRegEx();
            MatchCollection matches = urlRegex.Matches(content);
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

        private async Task SendUserStatsToDiscord(SocketInteraction command)
        {
            // Identifizieren Sie die Discord-ID des Benutzers, der den Befehl ausgelöst hat
            ulong userId = command.User.Id;
            ulong servId = command.GuildId!.Value;

            // Suchen Sie die Statistiken des Benutzers
            UserStatModel? userStats = _userStat.FirstOrDefault(u => u.DiscordID == (long)userId && u.DiscordServerID == (long)servId);


            if(userStats != null)
            {
                // Formatieren Sie die Nachricht
                string statsMessage = $"**Deine Statistiken:**\n" +
                                      $"- Wörter gezählt: {userStats.WordCount}\n" +
                                      $"- Bilder gepostet: {userStats.ImageCount}\n" +
                                      $"- URLs geteilt: {userStats.UrlCount}";

                // Erstellen Sie eine Embed-Nachricht für bessere Formatierung
                Embed? embed = new EmbedBuilder()
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
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleName)
            ];

            const string getActive = "" +
                                     "SELECT isActive " +
                                     " FROM view_channel_module_status " +
                                     " WHERE ChannelID = @IDChannel" +
                                     " AND ModuleName = @NameModul;";

            DataTable rows = _databaseService.ExecuteSqlQuery(getActive, isActivePara);

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
                const string insert = "INSERT INTO discord_module (ModuleName, IsServerModul, IsChannelModul) VALUES (@NameModul , @ServerModulIs , @ChannelModulIs )";
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
