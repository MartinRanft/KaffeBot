using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;

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
            foreach(var guild in _client.Guilds)
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

        public bool IsActive(ulong channelId, string moduleNam)
        {

            MySqlParameter[] isActivePara =
            [
                new("@IDChannel", channelId),
                new("@NameModul", moduleNam)
            ];

            string getActive = "" +
                "SELECT isActive " +
                " FROM view_channel_module_status " +
                " WHERE ChannelID = @IDChannel" +
                " AND ModuleName = @NameModul;";

            var rows = _databaseService.ExecuteSqlQuery(getActive, isActivePara);

            return (bool)rows.Rows[0]["isActive"];
        }

        private void LoadActiveServersFromDatabase()
        {
            MySqlParameter[] parameters = [];
            var dataTable = _databaseService.ExecuteSqlQuery("SELECT ServerID, ServerName FROM discord_server", parameters);
            foreach(System.Data.DataRow row in dataTable.Rows)
            {
                // Stellen Sie sicher, dass der Wert positiv ist, bevor Sie ihn zu ulong konvertieren
                var serverIdValue = row["ServerID"];
                if(serverIdValue != DBNull.Value && (long)serverIdValue >= 0)
                {
                    _activeServers.Add((ulong)(long)serverIdValue);
                }
                else
                {
                    // Behandeln Sie den Fall, dass der Wert negativ ist oder DBNull
                    // Zum Beispiel: Loggen oder Fehler werfen
                }
            }
        }

        private void SyncWithDatabase(ulong serverId, string serverName)
        {
            // Implementiere die Logik, um den Server mit der Datenbank abzugleichen
            var parameters = new MySqlParameter[]
            {
            new("@DiscordID", serverId),
            new("@DiscordName", serverName)
            };

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