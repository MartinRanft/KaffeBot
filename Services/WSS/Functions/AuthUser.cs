using System.Data;

using KaffeBot.Interfaces.DB;
using KaffeBot.Models.WSS;
using KaffeBot.Models.WSS.User;

using MySqlConnector;

namespace KaffeBot.Services.WSS.Functions
{
    internal class AuthUser
    {
        public static UserModel? Authenticate(CommandModel Usercmd, IDatabaseService database)
        {
            string Username = string.Empty;
            string Password = string.Empty;
            UserModel? User = null;

            if(Usercmd.CMDfor is not null && Usercmd.CMDfor.Count != 0)
            {
                ServerObject firstCmd = Usercmd!.CMDfor!.FirstOrDefault()!;
                Username = firstCmd.User!;
                Password = firstCmd.Password!;
            }
            else
            {
                return null;
            }

            MySqlParameter[] param = [
                new("@user_name", Username)
                ];

            DataRow? result = database.ExecuteStoredProcedure("GetUserPasswordByUsername", param).Rows
               .Cast<DataRow>()
               .FirstOrDefault();

            if(string.IsNullOrEmpty(result![0].ToString()))
            {
                return null;
            }

            if(result![0].ToString() != Password)
            {
                return User;
            }
            param = [];

            param = [
                new MySqlParameter("@user_id", result["UserID"])
            ];

            DataTable? UserData = database.ExecuteStoredProcedure("GetDiscordUserDetails", param);

            User!.DiscordID = (ulong)UserData!.Rows[0]["UserID"];
            User!.DiscordName = (string)UserData!.Rows[0]["DiscordName"];
            User!.IsAdmin = (bool)UserData!.Rows[0]["isAdmin"];
            User!.IsActive = (bool)UserData!.Rows[0]["isActive"];
            User!.IsServerMod = (bool)UserData!.Rows[0]["IsServerMod"];
            User!.ApiUser = (int)UserData!.Rows[0]["apiUser"];
            User!.ApiKey = (string)UserData!.Rows[0]["ApiKey"];

            return User;
        }
    }
}