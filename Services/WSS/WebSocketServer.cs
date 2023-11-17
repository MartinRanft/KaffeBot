using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;

namespace KaffeBot.Services.WSS
{
    internal class WebSocketServer : IHostedService
    {
        private HttpListener _listener;
        private int _port;

        public WebSocketServer(int port)
        {
            _port = port;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");
        }

        public async Task Start()
        {
            _listener.Start();
            string localIP = GetLocalIPAddress();
            Console.WriteLine($"WebSocket Server gestartet auf {localIP}:{_port}");

            while(true)
            {
                var context = await _listener.GetContextAsync();

                try
                {
                    if(context.Request.IsWebSocketRequest)
                    {
                        await Console.Out.WriteLineAsync("Client auf dem WebSocket verbunden.");
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        var webSocket = webSocketContext.WebSocket;

                        // Dauerschleife für die WebSocket-Kommunikation
                        while(webSocket.State == WebSocketState.Open)
                        {
                            var buffer = new ArraySegment<byte>(new byte[1024]);
                            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                            if(result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
                            {
                                string receivedMessage = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, result.Count);
                                if(receivedMessage == "PING")
                                {
                                    var pongBuffer = Encoding.UTF8.GetBytes("PONG");
                                    await webSocket.SendAsync(new ArraySegment<byte>(pongBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                            else if(result.MessageType == WebSocketMessageType.Close)
                            {
                                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch(Exception e)
                {
                    await Console.Out.WriteLineAsync(e.Message);
                }
            }
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _listener.Stop();
            return Task.CompletedTask;
        }

        private string GetLocalIPAddress()
        {
            using(var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
    }

}
