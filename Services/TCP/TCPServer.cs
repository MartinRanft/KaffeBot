using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using KaffeBot.Interfaces.DB;
using KaffeBot.Models.TCP.User;
using KaffeBot.Services.TCP.Function.Auth;
using KaffeBot.Services.TCP.Function.Command;

using Microsoft.Extensions.Hosting;

namespace KaffeBot.Services.TCP
{
    /// <summary>
    /// Represents a TCP server that listens for incoming connections on a specified port and handles client requests.
    /// </summary>
    internal sealed class TcpServer(int port, X509Certificate certificate, IDatabaseService databaseService) : TCPServerBase, IHostedService
    {
        private readonly TcpListener _listener = new(IPAddress.Any, port);
        private CancellationToken _stoppingToken;

        /// <summary>
        /// Starts the TCP server asynchronously, listening for incoming connections and handling client requests.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to stop the server.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppingToken = cancellationToken;
            _listener.Start();
            Console.WriteLine("TCP Server gestartet.");

            try
            {
                while(!_stoppingToken.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);

                    _ = Task.Run(() => HandleClient(client, _stoppingToken), cancellationToken);
                }
            }
            catch(Exception e)
            {
                await Console.Out.WriteLineAsync(e.Message);
            }
            finally
            {
                _listener.Stop();
            }
        }

        /// <summary>
        /// Handles a client connection, performing authentication and processing client requests.
        /// </summary>
        /// <param name="client">The TCP client representing the connected client.</param>
        /// <param name="stoppingToken">A cancellation token that can be used to stop the processing of the client.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleClient(TcpClient client, CancellationToken stoppingToken)
        {
            int messageCount = 0;
            AuthUser authUser = new(databaseService);

            // Erhalten der Client-IP-Adresse
            IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            IPAddress clientIp = clientEndPoint?.Address!;

            await using SslStream sslStream = new(client.GetStream(), false);

            try
            {
                await sslStream.AuthenticateAsServerAsync(certificate);

                byte[]? sharedKey = await GetSharedKeyAsync(sslStream, client);

                if(sharedKey is null)
                {
                    Console.WriteLine("Der Schlüssel konnte nicht ausgetauscht werden.");
                    sslStream.Close();
                    client.Close();
                    return;
                }

                CommandHandler handler = new(sslStream, sharedKey, client);

                while(client.Connected && !stoppingToken.IsCancellationRequested)
                {
                    if(messageCount == 0)
                    {
                        await SendMessage(sslStream, sharedKey, "Send AUTH NOW");
                    }

                    messageCount++;

                    string message = await ReceiveMessage(sslStream, sharedKey);

                    if(message != "AUTH")
                    {
                        continue;
                    }
                    await SendMessage(sslStream, sharedKey, "SEND KEY");
                    string keyMessage = await ReceiveMessage(sslStream, sharedKey);
                    message = string.Empty;
                    await SendMessage(sslStream, sharedKey, "SEND DATA");

                    message = await ReceiveMessage(sslStream, sharedKey);

                    UserModel? user = await authUser.Authenticate(message, keyMessage)!;

                    if(user is null)
                    {
                        UpdateFailedAttempts(clientIp);
                        await SendMessage(sslStream, sharedKey, "User check Fehlgeschlagen. \r \n Verbindung wird geschlossen.");
                        client.Close();
                    }
                    else
                    {
                        await SendMessage(sslStream, sharedKey, "AUTH completed. Awaiting Commands");

                        string command = await ReceiveMessage(sslStream, sharedKey);

                        await handler.Handling(command, stoppingToken);
                    }
                }
            }
            catch(Exception e)
            {
                await Console.Out.WriteLineAsync($"Fehler bei der Verarbeitung des Clients: {e.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        /// <summary>
        /// Stops the TCP server asynchronously, shutting down the listener and ending the server operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to stop the server.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("TCP Server wird heruntergefahren.");
            _listener.Stop();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the count of failed attempts for a client IP address.
        /// </summary>
        /// <param name="clientIP">The IP address of the client.</param>
        private static void UpdateFailedAttempts(IPAddress clientIP)
        {
            lock(FailedAttempts)
            {
                if(MainFrame.Contains(clientIP))
                {
                    return;
                }
                if(!FailedAttempts.TryGetValue(clientIP, out (int Attempts, DateTime LastAttempt) value))
                {
                    // Erster Fehlversuch für diese IP
                    FailedAttempts[clientIP] = (1, DateTime.UtcNow);
                }
                else
                {
                    (int attempts, DateTime lastAttempt) = value;

                    // Überprüfen, ob seit dem letzten Fehlversuch mehr als 5 Minuten vergangen sind
                    if(DateTime.UtcNow - lastAttempt > TimeSpan.FromMinutes(5))
                    {
                        // Reset der Fehlversuche, aber behalten der Gesamtanzahl im Auge
                        FailedAttempts[clientIP] = (attempts >= 5 ? attempts : 1, DateTime.UtcNow);
                    }
                    else
                    {
                        // Erhöhen der Fehlversuche
                        attempts++;

                        FailedAttempts[clientIP] = attempts switch
                        {
                            // Überprüfen auf dauerhafte Sperrung
                            >= 10 => throw new Exception("Diese IP wurde dauerhaft gesperrt."),
                            // Überprüfen auf temporäre Sperrung
                            >= 5 => throw new Exception("Diese IP wurde temporär gesperrt."),
                            _ => (attempts, DateTime.UtcNow)
                        };
                    }
                }
            }
        }
    }
}