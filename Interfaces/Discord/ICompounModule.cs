using Discord.WebSocket;

using KaffeBot.Services.Discord.Module;

namespace KaffeBot.Interfaces.Discord
{
    internal interface ICompounModule
    {
        Task HandleMenuSelectionAsync(SocketMessageComponent component);
        
        Task RegisterSelectionHandlerAsync(MenuSelectionHandler commandHandler);
    }
}