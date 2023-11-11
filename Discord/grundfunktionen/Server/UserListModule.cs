﻿using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.Server
{
    public class UserListModule : IBotModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IDatabaseService _databaseService;
        private readonly Dictionary<ulong, bool> _activeServers = new();


        public bool ShouldExecuteRegularly { get; set; }

        public UserListModule(DiscordSocketClient client, IDatabaseService databaseService)
        {
            _client = client;
            _databaseService = databaseService;
            ShouldExecuteRegularly = false;
        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            _client.UserJoined += OnUserJoinedAsync;
            await SyncUsersWithDatabase();
            await RegisterModul(nameof(UserListModule));
        }

        private async Task SyncUsersWithDatabase()
        {
            foreach(var guild in _client.Guilds)
            {
                foreach(var user in guild.Users)
                {
                    System.Console.WriteLine($"Gefundene User: {user.DisplayName}");
                    await AddOrUpdateUser(user);
                }
            }
        }

        private async Task OnUserJoinedAsync(SocketGuildUser user)
        {
            await AddOrUpdateUser(user);
        }


        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Hier könnte Logik eingefügt werden, um regelmäßige Aufgaben auszuführen, falls ShouldExecuteRegularly true ist.
            return Task.CompletedTask;
        }

        public Task ActivateAsync(ulong ChannelID)
        {
            _activeServers[ChannelID] = true;
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(ulong ChannelID)
        {
            _activeServers[ChannelID] = false;
            return Task.CompletedTask;
        }

        public bool IsActive(ulong ChannelID)
        {
            return _activeServers.TryGetValue(ChannelID, out var isActive) && isActive;
        }

        private async Task AddOrUpdateUser(SocketGuildUser user)
        {
            // Hier die Logik zum Hinzufügen oder Aktualisieren des Benutzers in der Datenbank
            var parameters = new MySqlParameter[]
            {
            new MySqlParameter("@p_UserID", user.Id),
            new MySqlParameter("@p_UserName", user.Username),
            new MySqlParameter("@p_DiscordName", $"{user.Username}#{user.Discriminator}"),
            // isActive wird auf false gesetzt, da der Benutzer standardmäßig nicht aktiv ist.
            new MySqlParameter("@p_IsActive", false)
            };

            _ = _databaseService.ExecuteStoredProcedure("AddOrUpdateDiscordUser", parameters);
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
