using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.User
{
    internal class LvlSystem(DiscordSocketClient client, IDatabaseService databaseService) : IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;
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
            // Registriere den Slash-Befehl
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("reg_webinterface")
                .WithDescription("Registriert ein Passwort für das Webinterface.")
                .AddOption("password", ApplicationCommandOptionType.String, "Das Passwort, das du registrieren möchtest", isRequired: true)
                .Build());

            _client.SlashCommandExecuted += HandleSlashCommandAsync;
            await RegisterModul(nameof(LvlSystem));
        }

        private Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            // Überprüfen, ob der ausgeführte Befehl der ist, den dieses Modul behandelt
            switch(command.Data.Name)
            {
                case "reg_webinterface":
                     ;
                    break;

                case "password_reset":
                     ;
                    break;
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
    }
}
