using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Services.TCplistner
{
    internal class AesTcpServer
    {
        private TcpListener _listner;
        private Aes _aes;
        private ECDiffieHellmanCng _diffieHellman;

        public AesTcpServer(string ipAddress, int port)
        {
            _listner = new TcpListener(IPAddress.Parse(ipAddress), port);
            _aes = Aes.Create();
            _aes.KeySize = 256;
            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;

            _diffieHellman = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha384
            };
        }

        public void Start()
        {
            _listner.Start();
            AcceptClients();
            Console.WriteLine("TCP gestartet");
        }

        private async void AcceptClients()
        {
            while(true)
            {
                var client = await _listner.AcceptTcpClientAsync();
                HanleClient(client);
            }
        }

        private async void HanleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var clientPublicKey = new byte[_diffieHellman.PublicKey.ExportSubjectPublicKeyInfo().Length];
            await stream.ReadAsync(clientPublicKey, 0, clientPublicKey.Length);
            var commonKey = _diffieHellman.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
            _aes.Key = commonKey;
            var buffer = new byte[4096];
            int bytesRead = 0;

            try
            {
                while((bytesRead = await stream.ReadAsync(buffer)) != 0)
                {
                    var encryptedMessage = new byte[bytesRead];
                    Array.Copy(buffer, encryptedMessage, bytesRead);
                    var decryptedMessage = DecryptMessage(encryptedMessage);
                    
                    Console.WriteLine($"Received: {decryptedMessage}");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            } finally 
            { 
                stream.Close(); 
            }
        }

        private object DecryptMessage(byte[] encryptedMessage)
        {
            using var memoryStream = new MemoryStream(encryptedMessage);
            using var cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }

        public void Stop()
        {
            _listner.Stop();
        }
    }
}
