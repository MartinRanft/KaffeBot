using Discord;
using Discord.WebSocket;

using KaffeBot.Discord.BotOwner;
using KaffeBot.Discord.grundfunktionen.Server;
using KaffeBot.Discord.grundfunktionen.User;
using KaffeBot.Interfaces.DB;
using KaffeBot.Interfaces.Discord;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace KaffeBot.Services.Discord
{
    public class DiscordBotService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;
        private readonly List<IBotModule> _modules = new List<IBotModule>();
        private bool _isReady;
        private System.Timers.Timer _timer;

        public DiscordBotService(IConfiguration configuration, IDatabaseService databaseService)
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
                await ActivateModuleAsync("ServerListModule", guild.Id);
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

            // Führe alle Module aus
            var moduleTasks = _modules.Select(module => module.ExecuteAsync(CancellationToken.None)).ToList();
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
                    await module.ExecuteAsync(CancellationToken.None);
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

        public async Task ActivateModuleAsync(string moduleName, ulong serverId)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht aktiviert werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.ActivateAsync(serverId);
            }
        }

        public async Task DeactivateModuleAsync(string moduleName, ulong serverId)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Die Module können nicht deaktiviert werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            if(module != null)
            {
                await module.DeactivateAsync(serverId);
            }
        }

        public bool IsModuleActive(string moduleName, ulong serverId)
        {
            if(!_isReady)
            {
                throw new InvalidOperationException("Der Modulstatus kann nicht überprüft werden, bevor der Bot bereit ist.");
            }

            var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName);
            return module?.IsActive(serverId) ?? false;
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
                await module.ExecuteAsync(CancellationToken.None);
            }
        }
    }
}
