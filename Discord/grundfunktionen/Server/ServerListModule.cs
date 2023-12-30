using System.Data;

using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.Server
{
    public class ServerListModule(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly HashSet<ulong> _activeServers = [];

        public bool ShouldExecuteRegularly { get; set; } = false;

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            // Initialisiere die Liste der aktiven Server aus der Datenbank
            LoadActiveServersFromDatabase();
            await RegisterModul(nameof(ServerListModule));
        }

        public Task Execute(CancellationToken stoppingToken)
        {
            // Liste alle Server auf, auf denen sich der Bot befindet
            foreach(SocketGuild? guild in _client.Guilds)
            {
                System.Console.WriteLine($"Der Bot ist auf dem Server: {guild.Name} ({guild.Id})");

                // Füge Logik hinzu, um mit der Datenbank abzugleichen
                SyncWithDatabase(guild.Id, guild.Name);
            }
            return Task.CompletedTask;
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

        public bool IsActive(ulong channelId, string moduleNam)
        {
            MySqlParameter[] isActivePara =
            [
                new MySqlParameter("@IDChannel", channelId),
                new MySqlParameter("@NameModul", moduleNam)
            ];

            const string getActive = "" +
                                     "SELECT isActive " +
                                     " FROM view_channel_module_status " +
                                     " WHERE ChannelID = @IDChannel" +
                                     " AND ModuleName = @NameModul;";

            DataTable rows = _databaseService.ExecuteSqlQuery(getActive, isActivePara);

            return (bool)rows.Rows[0]["isActive"];
        }

        private void LoadActiveServersFromDatabase()
        {
            MySqlParameter[] parameters = [];
            DataTable dataTable = _databaseService.ExecuteSqlQuery("SELECT ServerID, ServerName FROM discord_server", parameters);
            foreach(System.Data.DataRow row in dataTable.Rows)
            {
                // Stellen Sie sicher, dass der Wert positiv ist, bevor Sie ihn zu ulong konvertieren
                object serverIdValue = row["ServerID"];
                if(serverIdValue != DBNull.Value && (long)serverIdValue >= 0)
                {
                    _activeServers.Add((ulong)(long)serverIdValue);
                }
                else
                {
                }
            }
        }

        private void SyncWithDatabase(ulong serverId, string serverName)
        {
            // Implementiere die Logik, um den Server mit der Datenbank abzugleichen
            MySqlParameter[] parameters = [
            new MySqlParameter("@DiscordID", serverId),
                new MySqlParameter("@DiscordName", serverName)
            ];

            _databaseService.ExecuteStoredProcedure("SyncServerWithDatabase", parameters);
        }

        private void UpdateServerStatusInDatabase(ulong serverId, bool isActive)
        {
            var parameters = new MySqlParameter[]
            {
            new("@ServerId", serverId),
            new("@IsActive", isActive)
            };

            _databaseService.ExecuteStoredProcedure("UpdateServerStatus", parameters);
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