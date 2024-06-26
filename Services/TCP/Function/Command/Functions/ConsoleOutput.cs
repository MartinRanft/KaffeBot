using Discord.WebSocket;

using KaffeBot.Discord.grundfunktionen.Console;
using KaffeBot.Models.TCP;

using Newtonsoft.Json;

namespace KaffeBot.Services.TCP.Function.Command.Functions
{
    /// <summary>
    /// Represents a class that sends SocketMessages to a web service.
    /// </summary>
    internal abstract class ConsoleOutput
    {
        /// <summary>
        /// Sends the list of SocketMessages to a web service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result contains the serialized data to be sent.</returns>
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

        /// <summary>
        /// Sends the list of SocketMessages to a web service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result contains the serialized data to be sent.</returns>
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