using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.Server
{
    public class ServerListModule : IBotModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IDatabaseService _databaseService;
        private HashSet<ulong> _activeServers;

        public bool ShouldExecuteRegularly { get; set; }

        public ServerListModule(DiscordSocketClient client, IDatabaseService databaseService)
        {
            ShouldExecuteRegularly = false;
            _client = client;
            _databaseService = databaseService;
            _activeServers = new HashSet<ulong>();
        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            // Initialisiere die Liste der aktiven Server aus der Datenbank
            await LoadActiveServersFromDatabase();
            await RegisterModul(nameof(ServerListModule));
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Liste alle Server auf, auf denen sich der Bot befindet
            foreach(var guild in _client.Guilds)
            {
                System.Console.WriteLine($"Der Bot ist auf dem Server: {guild.Name} ({guild.Id})");

                // Füge Logik hinzu, um mit der Datenbank abzugleichen
                await SyncWithDatabase(guild.Id, guild.Name);
            }
        }

        public async Task ActivateAsync(ulong serverId)
        {
            _activeServers.Add(serverId);

            // Aktualisiere die Datenbank, um den Server als aktiv zu markieren
            await UpdateServerStatusInDatabase(serverId, true);
        }

        public async Task DeactivateAsync(ulong serverId)
        {
            _activeServers.Remove(serverId);
            // Aktualisiere die Datenbank, um den Server als inaktiv zu markieren
            await UpdateServerStatusInDatabase(serverId, false);
        }

        public bool IsActive(ulong serverId)
        {
            return _activeServers.Contains(serverId);
        }

        private async Task LoadActiveServersFromDatabase()
        {
            MySqlParameter[] parameters = new MySqlParameter[]
            {
                // Ihre Parameter, falls welche benötigt werden
            };
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


        private async Task SyncWithDatabase(ulong serverId, string serverName)
        {
            // Implementiere die Logik, um den Server mit der Datenbank abzugleichen
            var parameters = new MySqlParameter[]
            {
            new MySqlParameter("@DiscordID", serverId),
            new MySqlParameter("@DiscordName", serverName)
            };

            _databaseService.ExecuteStoredProcedure("SyncServerWithDatabase", parameters);
        }

        private async Task UpdateServerStatusInDatabase(ulong serverId, bool isActive)
        {
            var parameters = new MySqlParameter[]
            {
            new MySqlParameter("@ServerId", serverId),
            new MySqlParameter("@IsActive", isActive)
            };

            _databaseService.ExecuteStoredProcedure("UpdateServerStatus", parameters);
        }

        public Task RegisterModul(string modulename)
        {
            MySqlParameter[] parameter = new MySqlParameter[]
            {
                new MySqlParameter("@NameModul", modulename)
            };

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
