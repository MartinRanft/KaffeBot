using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

using KaffeBot.Discord.grundfunktionen.auto_roll;
using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.Console
{
    internal abstract class ConsoleToWeb(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly DiscordSocketClient _client = client;
        public event Action<List<SocketMessage>>? OnNewMessages;

        private Timer? _messageCleanupTimer;
        public bool ShouldExecuteRegularly { get; set; } = false;

        public List<SocketMessage> SocketMessages { get; private set; } = [];

        public static ConsoleToWeb? ToWeb { get; set; }

        private void NotifyNewMessages()
        {
            OnNewMessages?.Invoke(SocketMessages);
        }

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
            _client.MessageReceived += MessageReceivedAsync;
            ToWeb = this;

            // Timer einrichten, um Nachrichten alle 30 Minuten zu bereinigen
            _messageCleanupTimer = new Timer(CleanupOldMessages, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

            await RegisterModul(nameof(ConsoleToWeb).ToString());
        }

        private void CleanupOldMessages(object? state)
        {
            DateTime threshold = DateTime.UtcNow.AddMinutes(-30);
            lock(SocketMessages)
            {
                SocketMessages = SocketMessages.Where(msg => msg.Timestamp.UtcDateTime > threshold).ToList();
            }
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            lock(SocketMessages)
            {
                SocketMessages.Add(message);
            }

            NotifyNewMessages();
            return Task.CompletedTask;
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

        public Task RegisterCommandsAsync(SlashCommandHandler commandHandler)
        {
            commandHandler.RegisterModule(null, this);
            return Task.CompletedTask;
        }

        public Task HandleCommandAsync(SocketSlashCommand command)
        {
            return Task.CompletedTask;
        }
    }
}
