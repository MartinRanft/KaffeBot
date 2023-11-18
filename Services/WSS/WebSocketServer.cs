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
using Newtonsoft.Json;
using KaffeBot.Models.WSS.User;
using KaffeBot.Models.WSS;
using KaffeBot.Services.WSS.Functions;
using KaffeBot.Interfaces.DB;

namespace KaffeBot.Services.WSS
{
    internal class WebSocketServer : IHostedService
    {
        private readonly HttpListener _listener;
        private readonly IDatabaseService _databaseService;
        private WebSocket? _websocket;
        private readonly int _port;

        public WebSocketServer(int port, IDatabaseService database)
        {
            _port = port;
            _databaseService = database;

            _listener = new HttpListener();
#if !DEBUG
            _listener.Prefixes.Add($"http://*:{port}/");
#else
            _listener.Prefixes.Add($"http://localhost:{port}/");
#endif
        }

        public async Task Start()
        {
            _listener.Start();
            string localIP = GetLocalIPAddress();
            Console.WriteLine($"WebSocket Server gestartet auf {localIP}:{_port}");
            UserModel? user = null;
            int messageCount = 0;
            bool firstMessage = false;

            while(true)
            {
                var context = await _listener.GetContextAsync();

                try
                {
                    if(context.Request.IsWebSocketRequest)
                    {
                        await Console.Out.WriteLineAsync("Client auf dem WebSocket verbunden.");
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        _websocket = webSocketContext.WebSocket;

                        // Dauerschleife für die WebSocket-Kommunikation
                        while(_websocket.State == WebSocketState.Open)
                        {
                            if(messageCount == 0)
                            {
                                firstMessage = true;
                            }
                            messageCount++;
                            if(firstMessage)
                            {
                                await SendMessage("Send Auth Now.");
                            }

                            ArraySegment<byte> buffer = new(new byte[1024]);
                            WebSocketReceiveResult result = await _websocket.ReceiveAsync(buffer, CancellationToken.None);

                            if(result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
                            {
                                CommandModel receivedMessage = GetMessage(buffer, result)!;

                                if(receivedMessage.Command == "AUTH" && user is null)
                                {
                                    user = AuthUser.Authenticate(receivedMessage, _databaseService);
                                    if(user is null)
                                    {
                                        await SendMessage("User nicht gefunden.");
                                    }
                                }

                                if(user is not null)
                                {
                                    switch (receivedMessage.Command)
                                    {
                                        case "PING": await SendMessage("PONG");
                                            break;
                                    }
                                }

                            }
                            else if(result.MessageType == WebSocketMessageType.Close)
                            {
                                await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                                user = null;
                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        user = null;
                        messageCount = 0;
                    }
                }
                catch(Exception e)
                {
                    await Console.Out.WriteLineAsync(e.Message);
                    messageCount = 0;
                    user = null;
                }
            }
        }

        internal async Task SendMessage(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _websocket!.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        internal static CommandModel? GetMessage(ArraySegment<byte> buffer, WebSocketReceiveResult Message)
        {
            string JsonString = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, Message.Count);
            CommandModel? result;
            try
            {
                result = JsonConvert.DeserializeObject<CommandModel>(JsonString)!;
            }
            catch(Exception)
            {
                if(JsonString == "PING")
                {
                    result = new()
                    {
                        Command = JsonString
                    };
                }
                else
                {
                    result = new()
                    {
                        Command = "UNKNOW"
                    };
                }
            }
            return result;
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

        private static string GetLocalIPAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint!.Address.ToString();
        }
    }

}
