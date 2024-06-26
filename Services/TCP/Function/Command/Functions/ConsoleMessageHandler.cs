using System.Net.Security;
using System.Net.Sockets;

using Discord.WebSocket;

using KaffeBot.Discord.grundfunktionen.Console;

namespace KaffeBot.Services.TCP.Function.Command.Functions
{
    /// <summary>
    /// Handles sending console output to a web service and listening for new console messages from the web service.
    /// </summary>
    internal sealed class ConsoleMessageHandler(SslStream stream, byte[] sharedKey, TcpClient client) : TCPServerBase
    {
        private readonly SslStream _stream = stream;
        private readonly byte[] _sharedKey = sharedKey;
        private readonly TcpClient _tcpClient = client;

        /// <summary>
        /// Sends console output to a web service and listens for new console messages from the web service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendConsole(CancellationToken cancellationToken)
        {
            string message = await ConsoleOutput.SendToWeb();

            await SendMessage(_stream, _sharedKey, message);

            ConsoleToWeb.ToWeb!.OnNewMessages += EventConsoleToWeb;

            try
            {
                while(_tcpClient.Connected && !cancellationToken.IsCancellationRequested)
                {
                    // Überwachungslogik
                }
            }
            finally
            {
                if(!_tcpClient.Connected || cancellationToken.IsCancellationRequested)
                {
                    ConsoleToWeb.ToWeb!.OnNewMessages -= EventConsoleToWeb;
                }
            }
        }

        /// <summary>
        /// Sends console output to a web service and listens for new console messages from the web service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async void EventConsoleToWeb(List<SocketMessage> socketMessages)
        {
            try
            {
                string message = await ConsoleOutput.SendToWeb(socketMessages);
                await SendMessage(_stream, _sharedKey, message);
            }
            catch(Exception ex)
            {
                // Fehlerbehandlung, z.B. Protokollierung des Fehlers
                Console.WriteLine($"Fehler im EventConsoleToWeb: {ex.Message}");
            }
        }
    }
}