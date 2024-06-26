using System.Net.Security;
using System.Net.Sockets;

using KaffeBot.Services.TCP.Function.Command.Functions;

namespace KaffeBot.Services.TCP.Function.Command
{
    /// <summary>
    /// Represents a handler for different commands.
    /// </summary>
    internal sealed class CommandHandler(SslStream stream, byte[] sharedKey, TcpClient client) : TCPServerBase
    {
        private readonly SslStream _stream = stream;
        private readonly byte[] _sharedKey = sharedKey;
        private readonly TcpClient _tcpClient = client;

        /// <summary>
        /// Handles the given command.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Handling(string command, CancellationToken cancellationToken)
        {
            switch(command)
            {
                case "Console":
                    ConsoleMessageHandler consoleHandler = new(_stream, _sharedKey, _tcpClient);
                    _ = consoleHandler.SendConsole(cancellationToken);
                    break;

                default:
                    break;
            }
            return Task.CompletedTask;
        }
    }
}