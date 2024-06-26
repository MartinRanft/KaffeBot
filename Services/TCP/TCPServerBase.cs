using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace KaffeBot.Services.TCP
{
    /// <summary>
    /// Represents a base class for TCP server functionality.
    /// </summary>
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

        /// <summary>
        /// Decrypts the given ciphertext using the specified shared key.
        /// </summary>
        /// <param name="receivedData">The ciphertext to decrypt.</param>
        /// <param name="sharedKey">The shared key used for decryption.</param>
        /// <returns>The decrypted plaintext.</returns>
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

        /// <summary>
        /// Encrypts the given message using the specified shared key.
        /// </summary>
        /// <param name="message">The message to encrypt.</param>
        /// <param name="sharedKey">The shared key used for encryption.</param>
        /// <returns>The encrypted ciphertext.</returns>
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

        /// <summary>
        /// Asynchronously retrieves the shared key used for encryption from the client.
        /// </summary>
        /// <param name="sslStream">The SslStream used for communication with the client.</param>
        /// <param name="client">The TcpClient representing the client connection.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the shared key
        /// as a byte array if the key exchange was successful, null otherwise.
        /// </returns>
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

        /// <summary>
        /// Receives a message from the specified SSL stream using the provided shared key for decryption.
        /// </summary>
        /// <param name="stream">The SSL stream from which to receive the message.</param>
        /// <param name="sharedKey">The shared key used for decryption.</param>
        /// <returns>The received message.</returns>
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

        /// <summary>
        /// Sends a message over a TCP connection using the specified SSL stream and shared key.
        /// </summary>
        /// <param name="stream">The SSL stream to use for sending the message.</param>
        /// <param name="sharedKey">The shared key used for encryption.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal static async Task SendMessage(SslStream stream, byte[] sharedKey, string message)
        {
            byte[] dataToSend = Encrypt(message, sharedKey);
            await stream.WriteAsync(dataToSend);
        }
    }
}