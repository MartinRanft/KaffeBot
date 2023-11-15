using KaffeBot.Interfaces.DB;
using KaffeBot.Services.DB;
using KaffeBot.Services.Discord;
using KaffeBot.Services.TCplistner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KaffeBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Konfigurations-Builder einrichten
                    config.SetBasePath(Directory.GetCurrentDirectory()) // Definiert den Basispfad für die Konfiguration
                           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); // Lädt die Konfigurationsdatei
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Konfiguration laden
                    var configuration = hostContext.Configuration;

                    // Datenbankservice hinzufügen
                    services.AddSingleton<IDatabaseService, DatabaseService>();

                    // DiscordBotService als HostedService hinzufügen
                    services.AddHostedService<DiscordBotService>(provider =>
                        new DiscordBotService(configuration, provider.GetRequiredService<IDatabaseService>()));

                    // Erstellen Sie den TCP-Server mit der festgelegten IP und dem Port
                    services.AddSingleton<AesTcpServer>(provider =>
                    {
                        var configuration = provider.GetRequiredService<IConfiguration>();
#if !DEBUG
                        string ipAddress = configuration["TcpServer:Prod:IP"]!;
                        int port = int.Parse(configuration["TcpServer:Prod:Port"]!);
#else
                        string ipAddress = configuration["TCPServer:Test:IP"]!;
                        int port = int.Parse(configuration["TCPServer:Test:Port"]!);
#endif
                        return new AesTcpServer(ipAddress, port);
                    });
                    services.AddHostedService<TcpServerService>();
                });
    }
}