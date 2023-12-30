using System.Data;

using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.auto_roll
{
    internal class Autorol(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
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
            client.JoinedGuild += SyncRolesWithDB;
            client.LeftGuild += DeleteRolesOfServer;
            client.GuildAvailable += SyncRolesWithDB;
            client.RoleCreated += AddRoleOfServer;
            client.RoleUpdated += UpdateRoleOfServer;
            client.RoleDeleted += DeleteRoleOfServer;
            client.UserJoined += AddUserToRole;

            foreach(SocketGuild? guild in client.Guilds)
            {
                await SyncRolesWithDB(guild);
            }
            await RegisterModul(nameof(Autorol).ToString());
        }

        private Task AddRoleOfServer(SocketRole role)
        {
            if(!IsActive(role.Guild.Id, "auto_roll"))
                return Task.CompletedTask;

            int serverID = GetServerDbId(role.Guild.Id);

            _databaseService.ExecuteStoredProcedure("UpsertRole",
            [
                new MySqlParameter("@p_ServerID", serverID),
                new MySqlParameter("@p_RollID", role.Id),
                new MySqlParameter("@p_RoleName", role.Name)
            ]);
            return Task.CompletedTask;
        }

        private int GetServerDbId(ulong id)
        {
            MySqlParameter outParameter = new()
            {
                ParameterName = "@p_DbId",
                MySqlDbType = MySqlDbType.Int32,
                Direction = ParameterDirection.Output
            };

            _databaseService.ExecuteStoredProcedure("GetServerDbId",
            [
                new MySqlParameter("@p_ServerID", id),
                outParameter
            ]);

            // Konvertieren des Ausgabeparameterwerts zu einem int und Rückgabe
            return Convert.ToInt32(outParameter.Value);
        }

        private async Task AddUserToRole(SocketGuildUser user)
        {
            // Holen Sie die Server-Datenbank-ID
            int serverDbId = GetServerDbId(user.Guild.Id);

            // Holen Sie alle Standardrollen für diesen Server
            const string query = "SELECT RollID FROM discord_rollen WHERE ServerID = @ServerID AND IsStandart = 1";
            DataTable roles = _databaseService.ExecuteSqlQuery(query, [new MySqlParameter("@ServerID", serverDbId)]);

            foreach(DataRow row in roles.Rows)
            {
                ulong roleId = Convert.ToUInt64(row["RollID"]);
                SocketRole? role = user.Guild.GetRole(roleId);

                if(role != null)
                {
                    // Fügen Sie dem Benutzer die Rolle hinzu
                    await user.AddRoleAsync(role);
                }
            }
        }

        private Task DeleteRoleOfServer(SocketRole role)
        {
            if(!IsActive(role.Guild.Id, "auto_roll"))
                return Task.CompletedTask;

            const string deleteRoleQuery = "DELETE FROM discord_rollen WHERE RollID = @RollID";
            _databaseService.ExecuteSqlQuery(deleteRoleQuery, [new MySqlParameter("@RollID", role.Id)]);
            return Task.CompletedTask;
        }

        private Task UpdateRoleOfServer(SocketRole oldRole, SocketRole newRole)
        {
            if(!IsActive(newRole.Guild.Id, "auto_roll"))
                return Task.CompletedTask;

            _databaseService.ExecuteStoredProcedure("UpsertRole",
            [
                new MySqlParameter("@p_ServerID", newRole.Guild.Id),
                new MySqlParameter("@p_RollID", oldRole.Id),
                new MySqlParameter("@p_RoleName", newRole.Name)
            ]);
            return Task.CompletedTask;
        }

        private Task DeleteRolesOfServer(SocketGuild guild)
        {
            if(!IsActive(guild.Id, "auto_roll"))
                return Task.CompletedTask;

            const string deleteRolesQuery = "DELETE FROM discord_rollen WHERE ServerID = @ServerID";
            _databaseService.ExecuteSqlQuery(deleteRolesQuery, [new MySqlParameter("@ServerID", GetServerDbId(guild.Id))]);
            return Task.CompletedTask;
        }

        private Task SyncRolesWithDB(SocketGuild guild)
        {
            // Überprüfen, ob das Modul für diesen Server aktiv ist
            if(!IsActive(guild.Channels.FirstOrDefault()!.Id, "Autorol"))
                return Task.CompletedTask;

            int serverDbId = GetServerDbId(guild.Id);

            foreach(SocketRole? role in guild.Roles)
            {
                // Aufrufen der gespeicherten Prozedur
                _databaseService.ExecuteStoredProcedure("UpsertRole", [
                    new MySqlParameter("@p_ServerID", serverDbId),
                    new MySqlParameter("@p_RollID", role.Id),
                    new MySqlParameter("@p_RoleName", role.Name),
                ]);
            }
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
                const string insert = "INSERT INTO discord_module (ModuleName, IsServerModul, IsChannelModul) VALUES (@NameModul , @ServerModulIs , @ChannelModulIs )";
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