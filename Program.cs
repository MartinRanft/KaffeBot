using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using KaffeBot.Interfaces.DB;
using KaffeBot.Services.DB;
using KaffeBot.Services.Discord;
using KaffeBot.Services.TCP;
using KaffeBot.Services.WSS;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KaffeBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Konfigurations-Builder einrichten
                    config.SetBasePath(Directory.GetCurrentDirectory()) // Definiert den Basispfad für die Konfiguration
                           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); // Lädt die Konfigurationsdatei
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Konfiguration laden
                    var configuration = hostContext.Configuration;

                    // Datenbankservice hinzufügen
                    services.AddSingleton<IDatabaseService, DatabaseService>();

                    // DiscordBotService als HostedService hinzufügen
                    services.AddHostedService<DiscordBotService>(provider =>
                        new DiscordBotService(configuration, provider.GetRequiredService<IDatabaseService>()));

                    X509Certificate2? certificate = null;
#if DEBUG
                    certificate = GenerateSelfSignedCertificate();
#else
                    certificate = LoadCertificates("/cert/", "IhrPasswort");

                    if (certificate == null)
	                {
                        certificate = GenerateSelfSignedCertificate();
                        global::System.Console.WriteLine("ACHTUNG SSL WURDE NICHT GELADEN!!!!!!");
	                }
#endif

                    services.AddHostedService<TCPServer>(provider =>
                        new TCPServer(8080, certificate, provider.GetRequiredService<IDatabaseService>()));
                });

        public static X509Certificate2 GenerateSelfSignedCertificate()
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest("cn=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

            // Exportieren Sie das Zertifikat mit einem privaten Schlüssel und einem optionalen Passwort
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "testpassword"), "testpassword", X509KeyStorageFlags.DefaultKeySet);
        }

        public static X509Certificate2? LoadCertificates(string directoryPath, string password)
        {
            X509Certificate2? certificate = null;
            var directoryInfo = new DirectoryInfo(directoryPath);
            FileInfo[] files = directoryInfo.GetFiles("*.pfx"); // Annahme, dass es sich um .pfx-Dateien handelt

            foreach(FileInfo file in files)
            {
                try
                {
                    certificate = new X509Certificate2(file.FullName, password);
                    Console.WriteLine($"Zertifikat geladen: {certificate.Subject}");
                    // Hier können Sie zusätzliche Aktionen mit dem geladenen Zertifikat durchführen
                }
                catch(Exception)
                {

                }
            }

            return certificate;
        }
    }
}