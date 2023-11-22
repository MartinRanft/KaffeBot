using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.Console
{
    internal class ConsoleToWeb(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly DiscordSocketClient _client = client;
        public event Action<List<SocketMessage>>? OnNewMessages;
        private List<SocketMessage> _recentMessages = [];
        private Timer? _messageCleanupTimer;
        public bool ShouldExecuteRegularly { get; set; } = false;

        public List<SocketMessage> SocketMessages { get => _recentMessages; }
        public static ConsoleToWeb? ToWeb { get; set; }

        private void NotifyNewMessages()
        {
            OnNewMessages?.Invoke(_recentMessages);
        }

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

        public Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            _client.MessageReceived += MessageReceivedAsync;
            ToWeb = this;

            // Timer einrichten, um Nachrichten alle 30 Minuten zu bereinigen
            _messageCleanupTimer = new Timer(CleanupOldMessages, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }

        private void CleanupOldMessages(object? state)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-30);
            lock(_recentMessages)
            {
                _recentMessages = _recentMessages.Where(msg => msg.Timestamp.UtcDateTime > threshold).ToList();
            }
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            lock(_recentMessages)
            {
                _recentMessages.Add(message);
            }

            NotifyNewMessages();
            return Task.CompletedTask;
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
                    System.Console.WriteLine($"Modul ({modulename}) in DB");
                }
            }
            else
            {
                string insert = "INSERT INTO discord_module (ModuleName) VALUES (@NameModul)";
                _databaseService.ExecuteSqlQuery(insert, parameter);
                System.Console.WriteLine($"Modul {modulename} der DB hinzugefügt");
            }
            return Task.CompletedTask;
        }
    }
}
