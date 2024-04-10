using Discord.WebSocket;

using KaffeBot.Interfaces.Discord;

namespace KaffeBot.Services.Discord.Module
{
    internal sealed class ButtonCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, IButtonModule> _buttonModules = [];

        internal ButtonCommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.ButtonExecuted += HandleComponentAsync;
        }
        
        internal void RegisterButtonModul(string? customId, IButtonModule buttonModule)
        {
            if(!string.IsNullOrEmpty(customId))
            {
                _buttonModules[customId] = buttonModule;
            }
        }
        
        private async Task HandleComponentAsync(SocketMessageComponent component)
        {
            if(_buttonModules.TryGetValue(component.Data.CustomId, out IButtonModule? button))
            {
                await button.HandleButtonAsync(component);
            }
        }
    }
}