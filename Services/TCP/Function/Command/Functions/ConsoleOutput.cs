using Discord.WebSocket;

using KaffeBot.Discord.grundfunktionen.Console;
using KaffeBot.Models.TCP;

using Newtonsoft.Json;

namespace KaffeBot.Services.TCP.Function.Command.Functions
{
    internal abstract class ConsoleOutput
    {
        public static Task<string> SendToWeb()
        {
            List<SocketMessage> messages = ConsoleToWeb.ToWeb!.SocketMessages!;

            string jsonstring = JsonConvert.SerializeObject(messages);

            CommandModel model = new()
            {
                Command = "ConsoleOutput",
                CmDfor =
                [
                    new ServerObject()
                    {
                        Data = jsonstring
                    }
                ]
            };

            string result = JsonConvert.SerializeObject(model);

            return Task.FromResult(result);
        }

        public static Task<string> SendToWeb(List<SocketMessage> messges)
        {
            string jsonstring = JsonConvert.SerializeObject(messges);

            CommandModel model = new()
            {
                Command = "ConsoleOutput",
                CmDfor =
                [
                    new ServerObject()
                    {
                        Data = jsonstring
                    }
                ]
            };

            string result = JsonConvert.SerializeObject(model);

            return Task.FromResult(result);
        }
    }
}