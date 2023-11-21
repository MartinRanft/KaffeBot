using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using KaffeBot.Interfaces.DB;
using KaffeBot.Models.TCP.User;
using KaffeBot.Services.TCP.Function;

using Microsoft.Extensions.Hosting;

namespace KaffeBot.Services.TCP
{
    internal class TCPServer : IHostedService
    {
        private readonly TcpListener _listener;
        private readonly int _port;
        private CancellationToken _stoppingToken;
        private readonly IDatabaseService _database;
        private readonly X509Certificate2 _certificate;
        private static readonly Dictionary<IPAddress, (int Attempts, DateTime LastAttempt)> FailedAttempts = [];
        private static readonly List<IPAddress> MainFrame =
            [
                IPAddress.Parse("192.168.178.201"),
                IPAddress.Parse("135.125.188.200"),
                IPAddress.Parse("172.31.0.1"),
                IPAddress.Parse("127.0.0.1")
            ];

        public TCPServer(int port, X509Certificate2 certificate, IDatabaseService databaseService)
        {
            _port = port;
            _listener = new(IPAddress.Any, _port);
            _database = databaseService;
            _certificate = certificate;
        }

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
            finally 
            { 
                _listener.Stop(); 
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken stoppingToken)
        {
            byte[]? sharedKey;
            int messageCount = 0;
            string message;
            AuthUser authUser = new(_database) ;
            UserModel? user;

            // Erhalten der Client-IP-Adresse
            IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            IPAddress clientIP = clientEndPoint?.Address!;

            using var sslStream = new SslStream(client.GetStream(), false);

            try
            {
                await sslStream.AuthenticateAsServerAsync(_certificate);

                sharedKey = await GetSharedKeyAsync(sslStream, client);

                if(sharedKey is null)
                {
                    Console.WriteLine("Der Schlüssel konnte nicht ausgetauscht werden.");
                    sslStream.Close();
                    client.Close();
                    return;
                }

                while(client.Connected && !stoppingToken.IsCancellationRequested)
                {

                    if(messageCount == 0)
                    {
                        await SendMessage(sslStream, sharedKey, "Send AUTH NOW");
                    }

                    messageCount++;

                    message = await ReceiveMessage(sslStream, sharedKey);

                    if(message == "AUTH")
                    {
                        await SendMessage(sslStream, sharedKey, "SEND KEY");
                        string keyMessage = await ReceiveMessage(sslStream, sharedKey);
                        message = String.Empty;
                        await SendMessage(sslStream, sharedKey, "SEND DATA");

                        message = await ReceiveMessage(sslStream, sharedKey);

                        user = await authUser.Authenticate(message, keyMessage)!;

                        if(user is null)
                        {
                            UpdateFailedAttempts(clientIP);
                            await SendMessage(sslStream, sharedKey, "User check Fehlgeschlagen. \r \n Verbindung wird geschlossen.");
                            client.Close();
                        }
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

        private static async Task<byte[]?> GetSharedKeyAsync(SslStream sslStream, TcpClient client)
        {
            byte[] sharedKey;
            try
            {
                using ECDiffieHellman ecdh = ECDiffieHellman.Create();
                ecdh.KeySize = 256;

                byte[] publicKey = ecdh.ExportSubjectPublicKeyInfo();

                await sslStream.WriteAsync(BitConverter.GetBytes(publicKey.Length).AsMemory(0, sizeof(int)));
                await sslStream.WriteAsync(publicKey);

                byte[] clientKeyLengthBytes = new byte[sizeof(int)];
                await sslStream.ReadAsync(clientKeyLengthBytes.AsMemory(0, sizeof(int)));
                int clientKeyLength = BitConverter.ToInt32(clientKeyLengthBytes, 0);

                byte[] clientPublicKey = new byte[clientKeyLength];
                await sslStream.ReadAsync(clientPublicKey.AsMemory(0, clientKeyLength));

                using var clientECDh = ECDiffieHellman.Create();
                clientECDh.ImportSubjectPublicKeyInfo(clientPublicKey, out _);
                sharedKey = ecdh.DeriveKeyMaterial(clientECDh.PublicKey);

                if(client?.Client != null && client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
                {
                    await Console.Out.WriteLineAsync("(Client: " + remoteEndPoint.Address.ToString() +") Gemeinsamer AES-Schlüssel erfolgreich abgeleitet.");
                }
                
            }
            catch(Exception)
            {
                return null;
            }

            return sharedKey;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("TCP Server wird heruntergefahren.");
            _listener.Stop();
            return Task.CompletedTask;
        }

        private static async Task SendMessage(SslStream stream, byte[] sharedKey, string message)
        {
            byte[] dataToSend = Encrypt(message, sharedKey);
            await stream.WriteAsync(dataToSend);
        }

        private static byte[] Encrypt(string message, byte[] sharedKey)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = sharedKey;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.GenerateIV();

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msEncrypt = new();
            msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length); // Prepend IV to the ciphertext
            using(CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
            using(StreamWriter swEncrypt = new(csEncrypt))
            {
                swEncrypt.Write(message);
            }
            return msEncrypt.ToArray();
        }

        private static async Task<string> ReceiveMessage(SslStream stream, byte[] sharedKey)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer);
            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, receivedData, bytesRead);

            // Decrypt the received data
            string decryptedMessage = Decrypt(receivedData, sharedKey);
            return decryptedMessage;
        }

        private static string Decrypt(byte[] receivedData, byte[] sharedKey)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = sharedKey;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            // Extract the IV from the beginning of the ciphertext
            byte[] iv = new byte[aesAlg.BlockSize / 8];
            Array.Copy(receivedData, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new(receivedData, iv.Length, receivedData.Length - iv.Length);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);
            return srDecrypt.ReadToEnd();
        }

        private static void UpdateFailedAttempts(IPAddress clientIP)
        {
            lock(FailedAttempts)
            {
                if(!MainFrame.Contains(clientIP))
                {
                    if(!FailedAttempts.TryGetValue(clientIP, out (int Attempts, DateTime LastAttempt) value))
                    {
                        // Erster Fehlversuch für diese IP
                        FailedAttempts[clientIP] = (1, DateTime.UtcNow);
                    }
                    else
                    {
                        var (attempts, lastAttempt) = value;

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

                            // Überprüfen auf dauerhafte Sperrung
                            if(attempts >= 10)
                            {
                                throw new Exception("Diese IP wurde dauerhaft gesperrt.");
                            }

                            // Überprüfen auf temporäre Sperrung
                            if(attempts >= 5)
                            {
                                throw new Exception("Diese IP wurde temporär gesperrt.");
                            }

                            FailedAttempts[clientIP] = (attempts, DateTime.UtcNow);
                        }
                    }
                }
            }
        }
    }
}