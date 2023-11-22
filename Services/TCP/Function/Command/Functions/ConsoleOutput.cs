using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

using KaffeBot.Discord.grundfunktionen.Console;
using KaffeBot.Models.TCP;

using Newtonsoft.Json;

namespace KaffeBot.Services.TCP.Function.Command.Functions
{
    internal class ConsoleOutput
    {
        public static Task<string> SendToWeb()
        {
            List<SocketMessage> messges = ConsoleToWeb.ToWeb!.SocketMessages!;

            string Jsonstring = JsonConvert.SerializeObject(messges);

            CommandModel model = new()
            {
                Command = "ConsoleOutput",
                CMDfor = []
            };

            model.CMDfor.Add(new ServerObject() { Data = Jsonstring });

            string result = JsonConvert.SerializeObject(model);

            return Task.FromResult(result);

        }
        
        public static Task<string> SendToWeb(List<SocketMessage> messges)
        {
            string Jsonstring = JsonConvert.SerializeObject(messges);

            CommandModel model = new()
            {
                Command = "ConsoleOutput",
                CMDfor = []
            };

            model.CMDfor.Add(new ServerObject() { Data = Jsonstring });

            string result = JsonConvert.SerializeObject(model);

            return Task.FromResult(result);
        }
    }
}
