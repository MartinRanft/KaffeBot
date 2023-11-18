using Discord;
using Discord.Commands;
using Discord.WebSocket;

using KaffeBot.Discord.BotOwner;
using KaffeBot.Discord.grundfunktionen.Server;
using KaffeBot.Discord.grundfunktionen.User;
using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;
using KaffeBot.Services.Discord.Module;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using MySqlConnector;

namespace KaffeBot.Services.Discord
{
    public class DiscordBotService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;
        private readonly List<IBotModule> _modules = [];
        private bool _isReady;
        private System.Timers.Timer _timer;

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        public DiscordBotService(IConfiguration configuration, IDatabaseService databaseService)
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        {
            var clientConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true
            };

            _configuration = configuration;
            _databaseService = databaseService;
            _client = new DiscordSocketClient(clientConfig);
            _client.Log += LogAsync;
            _client.Ready += ClientReadyAsync; // Registriere den Event Handler vor dem Login
            _client.GuildAvailable += OnGuildAvailableAsync; // Event-Handler für Server-Betreten

            // Füge Module hier hinzu, damit sie initialisiert werden können, wenn der Client bereit ist
            _modules.Add(new ServerListModule(_client, _databaseService));
            _modules.Add(new UserListModule(_client, _databaseService));
            _modules.Add(new WebInterfaceRegistrationModule(_client, _databaseService));
            _modules.Add(new AiPicToChanel(_client, _databaseService));
        }

        private async Task OnGuildAvailableAsync(SocketGuild guild)
        {
            if(_isReady)
            {
                // Logik für das Ausführen von Modulen beim Betreten eines Servers

                foreach(var channel in guild.Channels)
                {
                    await ActivateModuleAsync("ServerListModule", channel.Id); 
                }
            }
        }

        private async Task ClientReadyAsync()
        {
            _isReady = true;
            // Initialisiere alle Module, da der Bot jetzt verbunden ist.
            foreach(var module in _modules)
            {
                await module.InitializeAsync(_client, _configuration);
            }

            InitializeTimer(); // Initialisiere den Timer für regelmäßige Ausführung

            // Hinzufügen der channels um bestimmte Module für einen channel zu deaktivieren.
            foreach(var Server in _client.Guilds)
            {
                foreach(var Kanal in Server.TextChannels) // Annahme, dass Sie Textkanäle überprüfen wollen
                {
                    MySqlParameter[] parameters =
                    [
                        new("@ChannelID", Kanal.Id)
                    ];

                    // Überprüfen, ob der Kanal bereits in der Datenbank ist
                    var result = _databaseService.ExecuteSqlQuery("SELECT * FROM discord_channel WHERE ChannelID = @ChannelID", parameters);

                    if(result.Rows.Count == 0)
                    {
                        // Kanal ist nicht in der Datenbank, also fügen Sie ihn hinzu
                        MySqlParameter[] insertParameters =
                        [
                            new("@ChannelID", Kanal.Id),
                            new("@ChannelName", Kanal.Name)
                        ];

                        string insertQuery = "INSERT INTO discord_channel (ChannelID, ChannelName) VALUES (@ChannelID, @ChannelName)";
                        _databaseService.ExecuteSqlQuery(insertQuery, insertParameters);
                        Console.WriteLine($"Kanal {Kanal.Name} zur Datenbank hinzugefügt");
                    }
                    else
                    {
                        Console.WriteLine($"Kanal {Kanal.Name} bereits in der Datenbank");
                    }
                }
            }

            CheckModules checkModules = new(_databaseService);

            foreach(var module in _modules)
            {
                int? moduleId = checkModules.GetModuleIdByName(module.GetType().Name);

                if(!moduleId.HasValue)
                {
                    continue; // Wenn das Modul nicht in der Datenbank registriert ist, überspringen
                }

                foreach(var Server in _client.Guilds)
                {
                    foreach(var Kanal in Server.TextChannels)
                    {
                        bool isActive = checkModules.IsModuleActiveForChannel(Kanal.Id, moduleId.Value);

                        if(!isActive)
                        {
                            // Fügen Sie den Eintrag hinzu, wenn er nicht existiert
                            isActive = checkModules.AddModuleEntryForChannel(Kanal.Id, moduleId.Value);
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
            var moduleTasks = _modules.Select(module => module.Execute(CancellationToken.None)).ToList();
            await Task.WhenAll(moduleTasks);
        }

        private void InitializeTimer()
        {
            _timer = new System.Timers.Timer(60000); // Setzt den Timer auf 60 Sekunden
            _timer.Elapsed += async (sender, e) => await OnTimerTickAsync();
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async Task OnTimerTickAsync()
        {
            // Logik für das regelmäßige Ausführen von Modulen
            foreach(var module in _modules)
            {
                // Überprüfen Sie, ob das Modul regelmäßig ausgeführt werden soll
                if(module.ShouldExecuteRegularly)
                {
                    await module.Execute(CancellationToken.None);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
#if !DEBUG
            string token = _configuration["Discord:Token"];
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

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Stop(); // Stoppe den Timer
            _timer?.Dispose(); // Entsorge den Timer
            await _client.LogoutAsync();
            await _client.StopAsync();
            await base.StopAsync(stoppingToken);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        public async Task ActivateModuleAsync(string moduleName, ulong channelID)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht aktiviert werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.ActivateAsync(channelID, moduleName);
            }
        }

        public async Task DeactivateModuleAsync(string moduleName, ulong channelID)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht deaktiviert werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.DeactivateAsync(channelID, moduleName);
            }
        }

        public bool IsModuleActive(string moduleName, ulong channelId)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Der Modulstatus kann nicht überprüft werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            
            if(module == null)
            {
                return false; // Modul nicht gefunden
            }

            return module.IsActive(channelId, moduleName);
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

        public async Task TriggerModuleAsync(string moduleName)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht getriggert werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.Execute(CancellationToken.None);
            }
        }
    }
}