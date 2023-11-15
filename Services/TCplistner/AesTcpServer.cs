using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using KaffeBot.Models.TCP;

using Newtonsoft.Json;

namespace KaffeBot.Services.TCplistner
{
    public class AesTcpServer
    {
        private TcpListener _listner;
        private Aes _aes;
        private ECDiffieHellman _diffieHellman;

        public AesTcpServer(string ipAddress, int port)
        {
            _listner = new TcpListener(IPAddress.Parse(ipAddress), port);
            _aes = Aes.Create();
            _aes.KeySize = 256;
            _aes.GenerateKey();
            _aes.GenerateIV();
            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;

            _diffieHellman = ECDiffieHellman.Create();
            _diffieHellman.KeySize = 256;
        }

        public void Start()
        {
            _listner.Start();
            Console.WriteLine("TCP Server gestartet");
            Console.WriteLine($"TCP Server gestartet auf {_listner.LocalEndpoint}");
            Task.Run(async () => await AcceptClientsAsync());
        }

        private async Task AcceptClientsAsync()
        {
            while(true)
            {
                TcpClient client = await _listner.AcceptTcpClientAsync();
                await Task.Run(async () => await HandleClientAsync(client));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            var clientPublicKeyBytes = await ReadPublicKeyAsync(stream);

            using var clientECDh = ECDiffieHellman.Create();
            clientECDh.ImportSubjectPublicKeyInfo(clientPublicKeyBytes, out _);
            var commonKey = _diffieHellman.DeriveKeyMaterial(clientECDh.PublicKey);

            await stream.WriteAsync(_aes.IV, 0, _aes.IV.Length);

            // Initialize AES with the common key
            _aes.Key = commonKey;

            using var timeoutTimer = new System.Timers.Timer(120000); // 2 Minuten
            timeoutTimer.AutoReset = false; // Stellen Sie sicher, dass der Timer nur einmal ausgelöst wird
            timeoutTimer.Elapsed += (sender, args) =>
            {
                Console.WriteLine("Inaktivität festgestellt, schließe Verbindung.");
                client.Close(); // Schließt die Clientverbindung
            };

            try
            {
                await SendEncryptedMessageAsync("Bitte Benutzername und Passwort angeben", stream);
                timeoutTimer.Start();

                while(true)
                {
                    string decryptedMessage = await ReadDecryptedMessageAsync(stream);
                    timeoutTimer.Stop();

                    CommandModel? commandModel = JsonConvert.DeserializeObject<CommandModel>(decryptedMessage);

                    if(commandModel is null)
                    {
                        await SendEncryptedMessageAsync($"Verbindung wird getrennt", stream);
                        break;
                    }

                    switch(commandModel.Command)
                    {
                        case "PING": await SendEncryptedMessageAsync("PONG", stream);
                            break;
                    }

                    Console.WriteLine($"Received: {decryptedMessage}");
                    timeoutTimer.Start();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
            finally
            {
                timeoutTimer.Stop();
                stream.Close();
                client.Close();
            }
        }

        private async Task<string> ReadDecryptedMessageAsync(NetworkStream stream)
        {
            // Lesen Sie zuerst die Länge der Nachricht
            byte[] lengthBuffer = new byte[sizeof(int)];
            int bytesRead = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if(bytesRead != sizeof(int))
                throw new InvalidOperationException("Fehler beim Lesen der Nachrichtenlänge.");

            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] encryptedMessage = new byte[messageLength];
            int totalBytesRead = 0;

            // Lesen Sie die verschlüsselte Nachricht basierend auf der Länge
            while(totalBytesRead < messageLength)
            {
                int read = await stream.ReadAsync(encryptedMessage, totalBytesRead, messageLength - totalBytesRead);
                if(read == 0)
                {
                    throw new InvalidOperationException("Verbindung wurde geschlossen, während die Nachricht gelesen wurde.");
                }
                totalBytesRead += read;
            }

            // Entschlüsseln Sie die Nachricht
            using var memoryStream = new MemoryStream(encryptedMessage);
            using var cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(_aes.Key, _aes.IV), CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
            return reader.ReadToEnd(); // Diese Methode gibt den entschlüsselten String zurück
        }

        private async Task SendEncryptedMessageAsync(string message, NetworkStream stream)
        {
            // Nachricht in ein Byte-Array umwandeln
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(message);

            // Nachricht verschlüsseln
            byte[] encryptedBytes;
            using(var encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV))
            {
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using(var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(message);
                }
                encryptedBytes = msEncrypt.ToArray();
            }

            // Senden Sie die Länge der verschlüsselten Nachricht, gefolgt von der verschlüsselten Nachricht selbst
            byte[] encryptedMessageLength = BitConverter.GetBytes(encryptedBytes.Length);
            await stream.WriteAsync(encryptedMessageLength, 0, encryptedMessageLength.Length);
            await stream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);
        }

        private async Task<byte[]> ReadPublicKeyAsync(NetworkStream stream)
        {
            byte[] lengthBuffer = new byte[sizeof(int)];
            await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            int publicKeyLength = BitConverter.ToInt32(lengthBuffer, 0);

            byte[] clientPublicKey = new byte[publicKeyLength];
            await stream.ReadAsync(clientPublicKey, 0, clientPublicKey.Length);
            return clientPublicKey;
        }

        public void Stop()
        {
            _listner.Stop();
            Console.WriteLine("TCP Server gestoppt");
        }
    }
}