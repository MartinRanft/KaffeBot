using Discord.WebSocket;

using KaffeBot.Interfaces.Discord;

namespace KaffeBot.Services.Discord.Module
{
    public sealed class SlashCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, IBotModule> _commandModules = [];

        internal SlashCommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.SlashCommandExecuted += HandleSlashCommandAsync;
        }

        internal void RegisterModule(string? command, IBotModule module)
        {
            if(!string.IsNullOrEmpty(command))
            {
                _commandModules[command] = module;
            }
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            if(_commandModules.TryGetValue(command.Data.Name, out IBotModule? module))
            {
                await module.HandleCommandAsync(command);
            }
        }
    }
}