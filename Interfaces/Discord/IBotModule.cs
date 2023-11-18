using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace KaffeBot.Interfaces.Discord
{
    public interface IBotModule
    {
        bool ShouldExecuteRegularly { get; set; }

        Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration);

        Task Execute(CancellationToken stoppingToken);

        Task ActivateAsync(ulong channelId, string moduleName); // Aktiviere das Modul für einen bestimmten Channel

        Task DeactivateAsync(ulong channelId, string moduleName); // Deaktiviere das Modul für einen bestimmten Channel

        bool IsActive(ulong channelId, string moduleName); // Überprüfe, ob das Modul für einen bestimmten Channel aktiv ist

        Task RegisterModul(string modulename); //Regestriere das Modul bei der DB wenn nicht vorhanden.
    }
}