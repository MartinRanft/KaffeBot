using Discord.WebSocket;

using KaffeBot.Interfaces;

namespace KaffeBot.Services.Discord.Module
{
    internal sealed class ModularHandler
    {
        private readonly Dictionary<string, IInputModul> _inputModuls = [];

        internal ModularHandler(DiscordSocketClient client)
        {
            client.ModalSubmitted += HandleModalAsync;
        }

        internal void RegisterModularModul(string? customID, IInputModul inputModul)
        {
            if(!string.IsNullOrEmpty(customID))
            {
                _inputModuls[customID!] = inputModul;
            }
        }

        private async Task HandleModalAsync(SocketModal modal)
        {
            string[] part = modal.Data.CustomId.Split("_");

            if(_inputModuls.TryGetValue($"{part[0]}", out IInputModul? inputModul))
            {
                await inputModul.HandleModulaSelectionAsync(modal);
            }
        }
    }
}
