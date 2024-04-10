using Discord.WebSocket;

using KaffeBot.Services.Discord.Module;

namespace KaffeBot.Interfaces.Discord
{
    internal interface IButtonModule
    {
        Task HandleButtonAsync(SocketMessageComponent component);
        
        Task RegisterButtonAsync(ButtonCommandHandler commandHandler);
    }
}