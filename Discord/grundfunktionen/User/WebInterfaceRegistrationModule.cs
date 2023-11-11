using System.Data;
using System.Data.Common;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using GroupAttribute = Discord.Interactions.GroupAttribute;

namespace KaffeBot.Discord.grundfunktionen.User
{
    public class WebInterfaceRegistrationModule : InteractionModuleBase<SocketInteractionContext>, IBotModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IDatabaseService _databaseService;
        private bool _isActive;

        public bool ShouldExecuteRegularly { get; set; }

        public WebInterfaceRegistrationModule(DiscordSocketClient client, IDatabaseService databaseService)
        {
            _client = client;
            _databaseService = databaseService;
            ShouldExecuteRegularly = false;
        }

        public async Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration)
        {
            // Registriere den Slash-Befehl
            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("reg_webinterface")
                .WithDescription("Registriert ein Passwort für das Webinterface.")
                .AddOption("password", ApplicationCommandOptionType.String, "Das Passwort, das du registrieren möchtest", isRequired: true)
                .Build());

            await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("password_reset")
                .WithDescription("Setzen dein Password für das WebInterface neu")
                .AddOption("password", ApplicationCommandOptionType.String, "Das Password was du nun Benutzen möchtest", isRequired: true)
                .Build());
            
            _client.SlashCommandExecuted += HandleSlashCommandAsync;
            await RegisterModul(nameof(WebInterfaceRegistrationModule));
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            // Überprüfen, ob der ausgeführte Befehl der ist, den dieses Modul behandelt
            switch(command.Data.Name)
            {
                case "reg_webinterface":
                    await RegisterWebInterfacePasswordAsync(command);
                    break;
                case "password_reset":
                    await ResetWebInterfacePasswordAsync(command);
                    break;
            }
        }

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Führen Sie hier regelmäßige Aufgaben aus, wenn ShouldExecuteRegularly true ist
            return Task.CompletedTask;
        }

        public Task ActivateAsync(ulong serverId)
        {
            _isActive = true;
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(ulong serverId)
        {
            _isActive = false;
            return Task.CompletedTask;
        }

        public bool IsActive(ulong serverId)
        {
            // Implementieren Sie die Logik, um zu überprüfen, ob das Modul für einen bestimmten Server aktiv ist
            return _isActive;
        }

        [SlashCommand("reg_webinterface", "Registriert ein Passwort für das Webinterface.")]
        public async Task RegisterWebInterfacePasswordAsync(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            ulong userId = command.User.Id;
            string password = (string)command.Data.Options.First().Value;
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            MySqlParameter[] parameters = new MySqlParameter[]
            {
                new MySqlParameter("@DiscordUserID", userId),
                new MySqlParameter("@hashedPassword", hashedPassword)
            };

            try
            {
                string query = "SELECT * FROM discord_user WHERE UserID = @DiscordID ";

                MySqlParameter[] userpara = new MySqlParameter[]
                {
                    new MySqlParameter("@DiscordID", userId)
                };

                DataTable userData = _databaseService.ExecuteSqlQuery(query, userpara);

                if(userData.Rows[0]["Password"] == DBNull.Value)
                {

                    DataTable rowsAffected = _databaseService.ExecuteStoredProcedure("SetUserPassword", parameters);

                    if(rowsAffected.Rows.Count > 0)
                    {
                        // Senden Sie die endgültige Antwort
                        await command.FollowupAsync($"Dein Benutzername: {command.User.Username}\nDein Passwort: {password}", ephemeral: true);
                    }
                    else
                    {
                        await command.FollowupAsync("Es gab ein Problem beim Setzen deines Passworts. Bist du sicher, dass du registriert bist?", ephemeral: true);
                    }
                }
                else
                {
                    await command.FollowupAsync(@"Du hast dich Bereits regestriert für das WebInterface. Bitte Nutze /password_reset");
                }
            }
            catch(Exception ex)
            {
                // Logging der Ausnahme
                Console.WriteLine(ex.ToString());
                await command.FollowupAsync("Ein Fehler ist aufgetreten. Bitte versuche es später erneut.", ephemeral: true);
            }
        }

        [SlashCommand("password_reset", "Setzen dein Password für das WebInterface neu")]
        private async Task ResetWebInterfacePasswordAsync(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            ulong userId = command.User.Id;
            string password = (string)command.Data.Options.First().Value;
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            try
            {
                string query = "SELECT * FROM discord_user WHERE UserID = @DiscordID ";

                MySqlParameter[] userpara = new MySqlParameter[]
                {
                    new MySqlParameter("@DiscordID", userId)
                };

                DataTable userData = _databaseService.ExecuteSqlQuery(query, userpara);

                if(userData.Rows[0]["Password"] != DBNull.Value)
                {
                    MySqlParameter[] parameters = new MySqlParameter[]
                    {
                        new MySqlParameter("@DiscordUserID", userId),
                        new MySqlParameter("@hashedPassword", hashedPassword)
                    };

                    DataTable rowsAffected = _databaseService.ExecuteStoredProcedure("SetUserPassword", parameters);

                    if(rowsAffected.Rows.Count > 0)
                    {
                        // Senden Sie die endgültige Antwort
                        await command.FollowupAsync($"Dein Benutzername: {command.User.Username}\nDein neues Passwort: {password}", ephemeral: true);
                    }
                    else
                    {
                        await command.FollowupAsync("Es gab ein Problem beim Setzen deines Passworts. Bist du sicher, dass du registriert bist?", ephemeral: true);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                await command.FollowupAsync("Ein Fehler ist aufgetreten. Bitte versuche es später erneut.", ephemeral: true);
            }
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