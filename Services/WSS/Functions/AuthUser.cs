using System.Data;

using KaffeBot.Interfaces.DB;
using KaffeBot.Models.WSS;
using KaffeBot.Models.WSS.User;

using MySqlConnector;

namespace KaffeBot.Services.WSS.Functions
{
    /// <summary>
    /// Authenticates a user based on a command model and a database service.
    /// </summary>
    /// <param name="Usercmd">The command model containing user information</param>
    /// <param name="database">The database service used to execute queries</param>
    /// <returns>
    /// The authenticated user's UserModel if the authentication is successful,
    /// otherwise null.
    /// </returns>
    internal class AuthUser
    {
        /// <summary>
        /// Authenticates a user based on a command model and a database service.
        /// </summary>
        /// <param name="Usercmd">The command model containing user information</param>
        /// <param name="database">The database service used to execute queries</param>
        /// <returns>
        /// The authenticated user's UserModel if the authentication is successful,
        /// otherwise null.
        /// </returns>
        public static UserModel? Authenticate(CommandModel Usercmd, IDatabaseService database)
        {
            string username;
            string password;
            UserModel? User = null;

            if(Usercmd.CMDfor is not null && Usercmd.CMDfor.Count != 0)
            {
                ServerObject firstCmd = Usercmd!.CMDfor!.FirstOrDefault()!;
                username = firstCmd.User!;
                password = firstCmd.Password!;
            }
            else
            {
                return null;
            }

            MySqlParameter[] param = [
                new("@user_name", username)
                ];

            DataRow? result = database.ExecuteStoredProcedure("GetUserPasswordByUsername", param).Rows
               .Cast<DataRow>()
               .FirstOrDefault();

            if(string.IsNullOrEmpty(result![0].ToString()))
            {
                return null;
            }

            if(result![0].ToString() != password)
            {
                return User;
            }

            param = [
                new MySqlParameter("@user_id", result["UserID"])
            ];

            DataTable? userData = database.ExecuteStoredProcedure("GetDiscordUserDetails", param);

            User!.DiscordID = (ulong)userData!.Rows[0]["UserID"];
            User!.DiscordName = (string)userData!.Rows[0]["DiscordName"];
            User!.IsAdmin = (bool)userData!.Rows[0]["isAdmin"];
            User!.IsActive = (bool)userData!.Rows[0]["isActive"];
            User!.IsServerMod = (bool)userData!.Rows[0]["IsServerMod"];
            User!.ApiUser = (int)userData!.Rows[0]["apiUser"];
            User!.ApiKey = (string)userData!.Rows[0]["ApiKey"];

            return User;
        }
    }
}