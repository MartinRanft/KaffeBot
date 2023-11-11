using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace KaffeBot.Interfaces.Discord
{
    public interface IBotModule
    {
        bool ShouldExecuteRegularly { get; set; }
        Task InitializeAsync(DiscordSocketClient client, IConfiguration configuration);
        Task ExecuteAsync(CancellationToken stoppingToken);
        Task ActivateAsync(ulong serverId); // Aktiviere das Modul für einen bestimmten Server
        Task DeactivateAsync(ulong serverId); // Deaktiviere das Modul für einen bestimmten Server
        bool IsActive(ulong serverId); // Überprüfe, ob das Modul für einen bestimmten Server aktiv ist
        Task RegisterModul(string modulename); //Regestriere das Modul bei der DB wenn nicht vorhanden.
    }
}
