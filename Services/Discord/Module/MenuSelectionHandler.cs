using Discord.WebSocket;

using KaffeBot.Interfaces.Discord;

namespace KaffeBot.Services.Discord.Module
{
    public class MenuSelectionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, ICompounModule> _menuSelectionModules = [];

        internal MenuSelectionHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.SelectMenuExecuted += HandleMenuSelectionAsync;
        }

        internal void RegisterSelectionModul(string? customID, ICompounModule selectionModule)
        {
            if(!string.IsNullOrEmpty(customID))
            {
                _menuSelectionModules[customID!] = selectionModule;
            }
        }
        
        private async Task HandleMenuSelectionAsync(SocketMessageComponent component)
        {
            string[] part = component.Data.CustomId.Split("_");
            
            if(_menuSelectionModules.TryGetValue($"{part[0]}_{part[1]}", out ICompounModule? menuSelectionValue))
            {
                await menuSelectionValue.HandleMenuSelectionAsync(component);
            }
        }
    }
}