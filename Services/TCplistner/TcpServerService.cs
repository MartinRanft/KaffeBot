using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KaffeBot.Services.Discord;
using Microsoft.Extensions.Hosting;

namespace KaffeBot.Services.TCplistner
{
    public class TcpServerService : BackgroundService
    {
        private readonly AesTcpServer _tcpServer;

        public TcpServerService(AesTcpServer tcpServer)
        {
            _tcpServer = tcpServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Passen Sie die IP-Adresse und den Port an Ihre Bedürfnisse an
            _tcpServer.Start();

            // Hier können Sie auf Ereignisse vom TCP-Server warten und Aktionen im Discord-Bot ausführen
            // Beispiel: _tcpServer.OnMessageReceived += (sender, message) => _discordBotService.ExecuteModule(message);

            await Task.Delay(Timeout.Infinite, stoppingToken);

            // Vergessen Sie nicht, den Server ordnungsgemäß zu stoppen
            _tcpServer.Stop();
        }
    }
}
