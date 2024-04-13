using Discord.WebSocket;

using KaffeBot.Services.Discord.Module;

namespace KaffeBot.Interfaces
{
    internal interface IInputModul
    {
        Task HandleModulaSelectionAsync(SocketModal modal);
        
        Task RegisterModularHandlerAsync(ModularHandler modularHandler);
    }
}