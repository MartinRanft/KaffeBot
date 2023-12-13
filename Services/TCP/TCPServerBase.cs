using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace KaffeBot.Services.TCP
{
    internal class TCPServerBase
    {
        internal static readonly Dictionary<IPAddress, (int Attempts, DateTime LastAttempt)> FailedAttempts = [];
        internal static readonly List<IPAddress> MainFrame =
            [
                IPAddress.Parse("192.168.178.201"),
                IPAddress.Parse("135.125.188.200"),
                IPAddress.Parse("172.31.0.1"),
                IPAddress.Parse("127.0.0.1")
            ];

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

        internal static async Task<byte[]?> GetSharedKeyAsync(SslStream sslStream, TcpClient client)
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
                int readAsync = await sslStream.ReadAsync(clientKeyLengthBytes.AsMemory(0, sizeof(int)));
                int clientKeyLength = BitConverter.ToInt32(clientKeyLengthBytes, 0);

                byte[] clientPublicKey = new byte[clientKeyLength];
                int async = await sslStream.ReadAsync(clientPublicKey.AsMemory(0, clientKeyLength));

                using ECDiffieHellman clientECDh = ECDiffieHellman.Create();
                clientECDh.ImportSubjectPublicKeyInfo(clientPublicKey, out _);
                sharedKey = ecdh.DeriveKeyMaterial(clientECDh.PublicKey);

                if(client?.Client is { RemoteEndPoint: IPEndPoint remoteEndPoint })
                {
                    await Console.Out.WriteLineAsync("(Client: " + remoteEndPoint.Address.ToString() + ") Gemeinsamer AES-Schlüssel erfolgreich abgeleitet.");
                }

            }
            catch(Exception)
            {
                return null;
            }

            return sharedKey;
        }

        internal static async Task<string> ReceiveMessage(SslStream stream, byte[] sharedKey)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer);
            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, receivedData, bytesRead);

            // Decrypt the received data
            string decryptedMessage = Decrypt(receivedData, sharedKey);
            return decryptedMessage;
        }

        internal static async Task SendMessage(SslStream stream, byte[] sharedKey, string message)
        {
            byte[] dataToSend = Encrypt(message, sharedKey);
            await stream.WriteAsync(dataToSend);
        }
    }
}