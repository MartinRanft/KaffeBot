using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;
using KaffeBot.Interfaces.Discord;

namespace KaffeBot.Services.Discord.Module
{
    public class SlashCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, IBotModule> _commandModules = [];

        public SlashCommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.SlashCommandExecuted += HandleSlashCommandAsync;
        }

        public void RegisterModule(string? command, IBotModule module)
        {
            if(!string.IsNullOrEmpty(command))
            {
                _commandModules[command] = module; 
            }
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            if(_commandModules.TryGetValue(command.Data.Name, out var module))
            {
                await module.HandleCommandAsync(command);
            }
        }
    }

}
