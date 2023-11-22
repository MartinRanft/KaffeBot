using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

using KaffeBot.Services.TCP.Function.Command.Functions;

namespace KaffeBot.Services.TCP.Function.Command
{
    internal class CommandHandler(SslStream stream, byte[] sharedKey, TcpClient client) : TCPServerBase
    {
        private readonly SslStream _stream = stream;
        private readonly byte[] _sharedKey = sharedKey;
        private readonly TcpClient _tcpClient = client;

        public Task Handling(string command, CancellationToken cancellationToken)
        {
            switch(command)
            {
                case "Console":
                    var consoleHandler = new ConsoleMessageHandler(_stream, _sharedKey, _tcpClient);
                    _ = consoleHandler.SendConsole(cancellationToken);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
