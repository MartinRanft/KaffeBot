using System.Data;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace KaffeBot.Discord.grundfunktionen.User
{
    /// <summary>
    /// Represents a module for registration functionalities in a web interface.
    /// </summary>
    public class WebInterfaceRegistrationModule(DiscordSocketClient client, IDatabaseService databaseService) : InteractionModuleBase<SocketInteractionContext>, IBotModule
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDatabaseService _databaseService = databaseService;

        public bool ShouldExecuteRegularly { get; set; } = false;

        /// <summary>
        /// Initializes the module asynchronously.
        /// </summary>
        /// <param name="client">The DiscordSocketClient instance.</param>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

            await RegisterModul(nameof(WebInterfaceRegistrationModule));
        }

        /// <summary>
        /// Executes the regular tasks for the module.
        /// </summary>
        /// <param name="stoppingToken">A cancellation token that can be used to stop the module execution.</param>
        /// <returns>A Task representing the execution of the module's regular tasks.</returns>
        public Task Execute(CancellationToken stoppingToken)
        {
            // Führen Sie hier regelmäßige Aufgaben aus, wenn ShouldExecuteRegularly true ist
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

        /// <summary>
        /// Deactivates a module asynchronously for a specific channelId and moduleName.
        /// </summary>
        /// <param name="channelId">The ID of the channel.</param>
        /// <param name="moduleName">The name of the module.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Checks if a module is active for a specific channel.
        /// </summary>
        /// <param name="channelId">The ID of the channel.</param>
        /// <param name="moduleName">The name of the module.</param>
        /// <returns>True if the module is active, otherwise false.</returns>
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

        /// Registers a password for the web interface.
        /// </summary>
        /// <param name="command">The SocketSlashCommand instance.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [SlashCommand("reg_webinterface", "Registriert ein Passwort für das Webinterface.")]
        private async Task RegisterWebInterfacePasswordAsync(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            ulong userId = command.User.Id;
            string password = (string)command.Data.Options.First().Value;
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            MySqlParameter[] parameters =
            [
                new MySqlParameter("@DiscordUserID", userId),
                new MySqlParameter("@hashedPassword", hashedPassword)
            ];

            try
            {
                const string query = "SELECT * FROM discord_user WHERE UserID = @DiscordID ";

                MySqlParameter[] userpara =
                [
                    new MySqlParameter("@DiscordID", userId)
                ];

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
                    await command.FollowupAsync(@"Du hast dich Bereits registriert für das WebInterface. Bitte Nutze /password_reset");
                }
            }
            catch(Exception ex)
            {
                // Logging der Ausnahme
                System.Console.WriteLine(ex.ToString());
                await command.FollowupAsync("Ein Fehler ist aufgetreten. Bitte versuche es später erneut.", ephemeral: true);
            }
        }

        /// <summary>
        /// Resets the password for the web interface asynchronously.
        /// </summary>
        /// <param name="command">The SocketSlashCommand instance representing the command.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task ResetWebInterfacePasswordAsync(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            ulong userId = command.User.Id;
            string password = (string)command.Data.Options.First().Value;
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            try
            {
                const string query = "SELECT * FROM discord_user WHERE UserID = @DiscordID ";

                MySqlParameter[] userpara =
                [
                    new MySqlParameter("@DiscordID", userId)
                ];

                DataTable userData = _databaseService.ExecuteSqlQuery(query, userpara);

                if(userData.Rows[0]["Password"] != DBNull.Value)
                {
                    MySqlParameter[] parameters =
                    [
                        new MySqlParameter("@DiscordUserID", userId),
                        new MySqlParameter("@hashedPassword", hashedPassword)
                    ];

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
                System.Console.WriteLine(e.ToString());
                await command.FollowupAsync("Ein Fehler ist aufgetreten. Bitte versuche es später erneut.", ephemeral: true);
            }
        }

        /// <summary>
        /// Registers a module in the database.
        /// </summary>
        /// <param name="moduleName">The name of the module to register.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task RegisterModul(string moduleName)
        {
            MySqlParameter[] parameters = [
                new MySqlParameter("@NameModul", moduleName)
            ];

            try
            {
                DataTable modules = await Task.Run(() => _databaseService.ExecuteSqlQuery(
                    "SELECT * FROM discord_module WHERE ModuleName = @NameModul",
                    parameters
                )).ConfigureAwait(false);

                if(modules.Rows.Count > 0)
                {
                    System.Console.WriteLine($"Modul ({moduleName}) in DB");
                }
                else
                {
                    MySqlParameter[] insertParameters =
                    [
                        new MySqlParameter("@NameModul", moduleName),
                        new MySqlParameter("@ServerModulIs", true),
                        new MySqlParameter("@ChannelModulIs", true)
                    ];

                    await Task.Run(() => _databaseService.ExecuteSqlQuery(
                        "INSERT INTO discord_module (ModuleName, IsServerModul, IsChannelModul) VALUES (@NameModul, @ServerModulIs, @ChannelModulIs)",
                        insertParameters
                    )).ConfigureAwait(false);

                    System.Console.WriteLine($"Modul {moduleName} der DB hinzugefügt");
                }
            }
            catch(Exception ex)
            {
                System.Console.WriteLine($"An error occurred: {ex.Message}");
                // Consider logging the full exception details and stack trace for debugging
            }
        }

        /// <summary>
        /// Registers the commands asynchronously.
        /// </summary>
        /// <param name="commandHandler">The SlashCommandHandler instance.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public Task RegisterCommandsAsync(SlashCommandHandler commandHandler)
        {
            commandHandler.RegisterModule("reg_webinterface", this);
            commandHandler.RegisterModule("password_reset", this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles a command received from a SocketSlashCommand.
        /// </summary>
        /// <param name="command">The SocketSlashCommand instance.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task HandleCommandAsync(SocketSlashCommand command)
        {
            await (command.Data.Name switch
            {
                "reg_webinterface" => RegisterWebInterfacePasswordAsync(command),
                "password_reset" => ResetWebInterfacePasswordAsync(command),
                _ => Task.CompletedTask
            }).ConfigureAwait(false);
        }
    }
}