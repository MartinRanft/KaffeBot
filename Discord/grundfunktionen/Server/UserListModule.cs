using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.Server
{
    public class UserListModule(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly Dictionary<ulong, bool> _activeServers = [];

        public bool ShouldExecuteRegularly { get; set; } = false;

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            _client.UserJoined += OnUserJoinedAsync;
            SyncUsersWithDatabase();
            await RegisterModul(nameof(UserListModule));
        }

        private void SyncUsersWithDatabase()
        {
            foreach(var guild in _client.Guilds)
            {
                foreach(var user in guild.Users)
                {
                    System.Console.WriteLine($"Gefundene User: {user.DisplayName}");
                    AddOrUpdateUser(user);
                }
            }
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        private async Task OnUserJoinedAsync(SocketGuildUser user)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            AddOrUpdateUser(user);
        }

        public Task Execute(CancellationToken stoppingToken)
        {
            // Hier könnte Logik eingefügt werden, um regelmäßige Aufgaben auszuführen, falls ShouldExecuteRegularly true ist.
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

        private void AddOrUpdateUser(SocketGuildUser user)
        {
            // Hier die Logik zum Hinzufügen oder Aktualisieren des Benutzers in der Datenbank
            var parameters = new MySqlParameter[]
            {
            new("@p_UserID", user.Id),
            new("@p_UserName", user.Username),
            new("@p_DiscordName", $"{user.Username}#{user.Discriminator}"),
            // isActive wird auf false gesetzt, da der Benutzer standardmäßig nicht aktiv ist.
            new("@p_IsActive", false)
            };

            _ = _databaseService.ExecuteStoredProcedure("AddOrUpdateDiscordUser", parameters);
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