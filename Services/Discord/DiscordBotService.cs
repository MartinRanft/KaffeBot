using System.Data;
using System.Reflection;

using Discord;
using Discord.WebSocket;

using KaffeBot.Interfaces;
using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using MySqlConnector;

using Timer = System.Timers.Timer;

namespace KaffeBot.Services.Discord
{
    /// <summary>
    /// The DiscordBotService class is responsible for managing the Discord bot and its functionalities.
    /// </summary>
    internal sealed class DiscordBotService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;
        private readonly List<IBotModule> _modules = [];
        private bool _isReady;
        private Timer? _timer;

        /// <summary>
        /// Represents a Discord bot service.
        /// </summary>
        public DiscordBotService(IConfiguration configuration, IDatabaseService databaseService)
        {
            DiscordSocketConfig clientConfig = new()
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true
            };
            _configuration = configuration;
            _databaseService = databaseService;
            _client = new DiscordSocketClient(clientConfig);
            _client.Log += LogAsync;
            _client.Ready += ClientReadyAsync; // Registriere den Event Handler vor dem Login
            _client.GuildAvailable += CheckServerChannel;
            _client.JoinedGuild += OnGuildAvailableAsync; // Event-Handler für Server-Betreten
            SlashCommandHandler commandHandler = new(_client);
            ButtonCommandHandler buttonCommandHandler = new(_client);
            MenuSelectionHandler menuSelectionHandler = new(_client);
            ModularHandler modularHandler = new(_client);

            Task.Run(async () =>
            {
                // Für IBotModule
                await LoadAndRegisterModules<IBotModule>(module =>
                {
                    _modules.Add(module);
                    module.RegisterCommandsAsync(commandHandler).GetAwaiter().GetResult();
                });

                // Für IButtonModule
                await LoadAndRegisterModules<IButtonModule>(button => { button.RegisterButtonAsync(buttonCommandHandler).GetAwaiter().GetResult(); });

                // Für ICompounModule
                await LoadAndRegisterModules<ICompounModule>(menuSelection => { menuSelection.RegisterSelectionHandlerAsync(menuSelectionHandler).GetAwaiter().GetResult(); });

                // Für Modula anfragen
                await LoadAndRegisterModules<IInputModul>(inputModul => { inputModul.RegisterModularHandlerAsync(modularHandler).GetAwaiter().GetResult(); });
            });

            //DiscordBot = this;
        }

        //private DiscordBotService DiscordBot { get; }

        /// <summary>
        /// Checks if the server channel exists in the database and adds it if it does not.
        /// </summary>
        /// <param name="guild">The Discord guild to check the channels for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task CheckServerChannel(SocketGuild guild)
        {
            MySqlParameter outParameter = new()
            {
                ParameterName = "@p_DbId",
                MySqlDbType = MySqlDbType.Int32,
                Direction = ParameterDirection.Output
            };

            _databaseService.ExecuteStoredProcedure("GetServerDbId",
            [
                new MySqlParameter("@p_ServerID", guild.Id),
                outParameter
            ]);

            int serverInternId = Convert.ToInt32(outParameter.Value);

            foreach(SocketGuildChannel? channel in guild.Channels)
            {
                // Überprüfen, ob der Kanal in der Datenbank vorhanden ist
                MySqlParameter[] checkChannelParameters =
                [
                    new MySqlParameter("@ChannelID", channel.Id)
                ];
                DataTable channelResult = _databaseService.ExecuteSqlQuery("SELECT ID FROM discord_channel WHERE ChannelID = @ChannelID", checkChannelParameters);

                int channelIdDb;
                if(channelResult.Rows.Count == 0)
                {
                    // Füge den Kanal in die Datenbank ein
                    MySqlParameter[] insertParameters =
                    [
                        new MySqlParameter("@ChannelID", channel.Id),
                        new MySqlParameter("@ChannelName", channel.Name)
                    ];
                    _databaseService.ExecuteSqlQuery("INSERT INTO discord_channel (ChannelID, ChannelName) VALUES (@ChannelID, @ChannelName)", insertParameters);

                    // Hole die ID des neu eingefügten Kanals
                    channelIdDb = Convert.ToInt32(_databaseService.ExecuteSqlQuery("SELECT LAST_INSERT_ID()", []).Rows[0][0]);
                }
                else
                {
                    // Hole die ID des vorhandenen Kanals aus der Datenbank
                    channelIdDb = Convert.ToInt32(channelResult.Rows[0]["ID"]);
                }

                // Überprüfen, ob der Eintrag in discord_server_channel bereits existiert
                MySqlParameter[] checkServerChannelParameters =
                [
                    new MySqlParameter("@ServerID", serverInternId),
                    new MySqlParameter("@ChannelID", channelIdDb)
                ];
                DataTable serverChannelResult =
                _databaseService.ExecuteSqlQuery("SELECT ID FROM discord_server_channel WHERE ServerID = @ServerID AND ChannelID = @ChannelID", checkServerChannelParameters);

                if(serverChannelResult.Rows.Count != 0)
                {
                    continue;
                }
                // Füge den Server-Kanal-Eintrag in die Datenbank ein, wenn er nicht existiert
                MySqlParameter[] serverChannelParameters =
                [
                    new MySqlParameter("@ServerID", serverInternId),
                    new MySqlParameter("@ChannelID", channelIdDb)
                ];
                _databaseService.ExecuteSqlQuery("INSERT INTO discord_server_channel (ServerID, ChannelID) VALUES (@ServerID, @ChannelID)", serverChannelParameters);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the event when the bot joins a guild and becomes available in that guild.
        /// </summary>
        /// <param name="guild">The guild where the bot is available.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task OnGuildAvailableAsync(SocketGuild guild)
        {
            if(_isReady)
            {
                // Logik für das Ausführen von Modulen beim Betreten eines Servers

                foreach(SocketGuildChannel? channel in guild.Channels)
                {
                    await ActivateModuleAsync("ServerListModule", channel.Id);
                }
                await CheckServerChannel(guild);
            }
        }

        /// <summary>
        /// Called when the Discord client is ready to start receiving events.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ClientReadyAsync()
        {
            _isReady = true;
            // Initialisiere alle Module, da der Bot jetzt verbunden ist.
            foreach(IBotModule module in _modules)
            {
                await module.InitializeAsync(_client, _configuration);
            }

            InitializeTimer(); // Initialisiere den Timer für regelmäßige Ausführung

            // Hinzufügen der channels um bestimmte Module für einen channel zu deaktivieren.
            foreach(SocketGuild? server in _client.Guilds)
            {
                foreach(SocketTextChannel? kanal in server.TextChannels) // Annahme, dass Sie Textkanäle überprüfen wollen
                {
                    MySqlParameter[] parameters =
                    [
                        new MySqlParameter("@ChannelID", kanal.Id)
                    ];

                    // Überprüfen, ob der Kanal bereits in der Datenbank ist
                    DataTable result = _databaseService.ExecuteSqlQuery("SELECT * FROM discord_channel WHERE ChannelID = @ChannelID", parameters);

                    if(result.Rows.Count == 0)
                    {
                        // Kanal ist nicht in der Datenbank, also fügen Sie ihn hinzu
                        MySqlParameter[] insertParameters =
                        [
                            new MySqlParameter("@ChannelID", kanal.Id),
                            new MySqlParameter("@ChannelName", kanal.Name)
                        ];

                        const string insertQuery = "INSERT INTO discord_channel (ChannelID, ChannelName) VALUES (@ChannelID, @ChannelName)";
                        _databaseService.ExecuteSqlQuery(insertQuery, insertParameters);
                        Console.WriteLine($"Kanal {kanal.Name} zur Datenbank hinzugefügt");
                    }
                    else
                    {
                        Console.WriteLine($"Kanal {kanal.Name} bereits in der Datenbank");
                    }
                }
            }

            CheckModules checkModules = new(_databaseService);

            foreach(IBotModule module in _modules)
            {
                int? moduleId = checkModules.GetModuleIdByName(module.GetType().Name);

                if(!moduleId.HasValue)
                {
                    continue; // Wenn das Modul nicht in der Datenbank registriert ist, überspringen
                }

                foreach(SocketGuild? server in _client.Guilds)
                {
                    foreach(SocketTextChannel? kanal in server.TextChannels)
                    {
                        bool isActive = checkModules.IsModuleActiveForChannel(kanal.Id, moduleId.Value);

                        if(!isActive)
                        {
                            // Fügen Sie den Eintrag hinzu, wenn er nicht existiert
                            isActive = checkModules.AddModuleEntryForChannel(kanal.Id, moduleId.Value);
                        }

                        if(isActive)
                        {
                            // Führe das Modul für diesen Kanal aus, wenn es aktiv ist
                            await module.Execute(CancellationToken.None);
                        }
                    }
                }
            }

            // Führe alle Module aus
            List<Task> moduleTasks = _modules.Select(module => module.Execute(CancellationToken.None)).ToList();
            await Task.WhenAll(moduleTasks);
        }

        /// <summary>
        /// Initializes the timer for regular execution of modules.
        /// </summary>
        private void InitializeTimer()
        {
            _timer = new Timer(60000); // Setzt den Timer auf 60 Sekunden
            _timer.Elapsed += async (sender, e) => await OnTimerTickAsync();
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        /// <summary>
        /// Executes the logic for regularly executing modules.
        /// </summary>
        /// <returns>
        /// The task representing the asynchronous operation.
        /// </returns>
        private async Task OnTimerTickAsync()
        {
            // Logik für das regelmäßige Ausführen von Modulen
            foreach(IBotModule module in _modules.Where(module => module.ShouldExecuteRegularly))
            {
                await module.Execute(CancellationToken.None);
            }
        }

        /// <summary>
        /// Executes the Discord bot service asynchronously.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token that can be used to stop the execution of the service.</param>
        /// <returns>A task representing the asynchronous execution of the service.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
#if !DEBUG
            string token = _configuration["Discord:Token"]!;
#else
            string token = _configuration["Discord:TestToken"]!;
#endif
            if(string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Der Discord-Bot-Token wurde nicht in den App-Einstellungen gefunden.");
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Blockiert diesen Task bis der Bot gestoppt wird
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        /// <summary>
        /// Writes the log message to the console.
        /// </summary>
        /// <param name="log">The log message to be logged.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the Discord bot service asynchronously.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token that can be used to stop the execution of the service.</param>
        /// <returns>A task representing the asynchronous execution of the service.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Stop(); // Stoppe den Timer
            _timer?.Dispose(); // Entsorge den Timer
            await _client.LogoutAsync();
            await _client.StopAsync();
            await base.StopAsync(stoppingToken);
        }

        /// <summary>
        /// Activates a module for a specific channel.
        /// </summary>
        /// <param name="moduleName">The name of the module to activate.</param>
        /// <param name="channelId">The ID of the channel.</param>
        /// <returns>A task representing the asynchronous activation of the module.</returns>
        private async Task ActivateModuleAsync(string moduleName, ulong channelId)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht aktiviert werden, bevor der Bot bereit ist.");
            }

            IBotModule? module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.ActivateAsync(channelId, moduleName);
            }
        }

        /// <summary>
        /// Deactivates a module in the Discord bot service.
        /// </summary>
        /// <param name="moduleName">The name of the module to deactivate.</param>
        /// <param name="channelID">The ID of the channel where the module needs to be deactivated.</param>
        /// <returns>A task representing the asynchronous operation of deactivating the module.</returns>
        public async Task DeactivateModuleAsync(string moduleName, ulong channelID)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht deaktiviert werden, bevor der Bot bereit ist.");
            }

            IBotModule? module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.DeactivateAsync(channelID, moduleName);
            }
        }

        /// <summary>
        /// Checks if a module is active for a specific channel.
        /// </summary>
        /// <param name="moduleName">The name of the module.</param>
        /// <param name="channelId">The ID of the channel.</param>
        /// <returns>True if the module is active for the specified channel; otherwise, false.</returns>
        public bool IsModuleActive(string moduleName, ulong channelId)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Der Modulstatus kann nicht überprüft werden, bevor der Bot bereit ist.");
            }

            IBotModule? module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);

            return module != null &&
                   // Modul nicht gefunden
                   module.IsActive(channelId, moduleName);
        }

        // Diese Methode kann aufgerufen werden, um ein Modul zur Laufzeit hinzuzufügen.
        public async Task LoadModuleAsync(IBotModule module)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Module können nicht geladen werden, bevor der Bot bereit ist.");
            }

            _modules.Add(module);
            await module.InitializeAsync(_client, _configuration);
        }

        /// <summary>
        /// Executes a module with the specified name.
        /// </summary>
        /// <param name="moduleName">The name of the module to trigger.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task TriggerModuleAsync(string moduleName)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht getriggert werden, bevor der Bot bereit ist.");
            }

            IBotModule? module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.Execute(CancellationToken.None);
            }
        }

        /// <summary>
        /// Loads and registers modules that implement the specified interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="registerAction">The action to be performed on each module instance.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task LoadAndRegisterModules<TInterface>(Action<TInterface> registerAction) where TInterface : class
        {
            // Finde alle Typen, die das gegebene Interface implementieren, und nicht abstrakt sind
            IEnumerable<Type> moduleTypes = Assembly.GetExecutingAssembly()
                                                    .GetTypes()
                                                    .Where(t => typeof(TInterface).IsAssignableFrom(t) && !t.IsAbstract);

            // Instanziiere jedes gefundene Modul und führe die übergebene Aktion darauf aus
            foreach (Type type in moduleTypes)
            {
                TInterface moduleInstance = (TInterface)Activator.CreateInstance(type, _client, _databaseService)!;
                registerAction(moduleInstance);
            }
            return Task.CompletedTask;
        }

    }
}