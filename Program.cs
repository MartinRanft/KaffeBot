﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using KaffeBot.Interfaces.DB;
using KaffeBot.Services.DB;
using KaffeBot.Services.Discord;

namespace KaffeBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
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
                });
    }
}