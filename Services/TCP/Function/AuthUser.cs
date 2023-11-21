using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using BCrypt.Net;

using Discord;

using KaffeBot.Interfaces.DB;
using KaffeBot.Models.TCP;
using KaffeBot.Models.TCP.User;

using MySqlConnector;

using Newtonsoft.Json;

namespace KaffeBot.Services.TCP.Function
{
    internal class AuthUser
    {
        private readonly IDatabaseService _databaseService;

        internal AuthUser(IDatabaseService database)
        {
            _databaseService = database;
        }

        public Task<UserModel?>? Authenticate(string message, string sharedKeyBase64)
        {
            string Username = String.Empty;
            string Password = String.Empty;
            byte[]? IV = null;
            byte[]? sharedKey = null;
            UserModel? User = null;

            CommandModel model = JsonConvert.DeserializeObject<CommandModel>(message)!;

            if(model.CMDfor != null && model.CMDfor.Count != 0)
            {
                var firstCmd = model.CMDfor.First();
                Username = firstCmd.User!;
                IV = Convert.FromBase64String(firstCmd.IV!);
                sharedKey = Convert.FromBase64String(sharedKeyBase64);

                byte[] encryptedPassword = Convert.FromBase64String(firstCmd.Password!);

                Password = Decrypt(encryptedPassword, sharedKey, IV);

                MySqlParameter[] parameters = 
                    [
                        new("@user_name", Username)
                    ];

                DataRow? result = _databaseService.ExecuteStoredProcedure("GetUserPasswordByUsername", parameters).Rows
                       .Cast<DataRow>()
                       .FirstOrDefault();

                if(String.IsNullOrEmpty(result![0].ToString()))
                {
                    return null;
                }

                if(BCrypt.Net.BCrypt.Verify(Password, result!["Password"].ToString()))
                {
                    parameters = [];

                    parameters = [
                        new("@user_id", result["UserID"])
                    ];

                    DataTable? UserData = _databaseService.ExecuteStoredProcedure("GetDiscordUserDetails", parameters);

                    User!.DiscordID = (ulong)UserData!.Rows[0]["UserID"];
                    User!.DiscordName = (string)UserData!.Rows[0]["DiscordName"];
                    User!.IsAdmin = (bool)UserData!.Rows[0]["isAdmin"];
                    User!.IsActive = (bool)UserData!.Rows[0]["isActive"];
                    User!.IsServerMod = (bool)UserData!.Rows[0]["IsServerMod"];
                    User!.ApiUser = (int)UserData!.Rows[0]["apiUser"];
                    User!.ApiKey = (string)UserData!.Rows[0]["ApiKey"];
                }
            }
            return Task.FromResult(User);
        }

        private static string Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
        {
            string? plaintext = null;

            using(Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using MemoryStream msDecrypt = new(cipherText);
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new(csDecrypt);
                plaintext = srDecrypt.ReadToEnd();
            }

            return plaintext;
        }

    }
}
