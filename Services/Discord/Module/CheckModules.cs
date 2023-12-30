using System.Data;

using KaffeBot.Interfaces.DB;

using MySqlConnector;

namespace KaffeBot.Services.Discord.Module
{
    internal sealed class CheckModules(IDatabaseService databaseService)
    {
        private readonly IDatabaseService _databaseService = databaseService;

        internal int? GetModuleIdByName(string moduleName)
        {
            MySqlParameter[] parameters =
            [
                new MySqlParameter("@ModuleName", moduleName)
            ];

            var result = _databaseService.ExecuteSqlQuery("SELECT ID FROM discord_module WHERE ModuleName = @ModuleName", parameters);

            if(result.Rows.Count > 0)
            {
                return Convert.ToInt32(result.Rows[0]["ID"]);
            }

            return null;
        }

        public bool IsModuleActiveForChannel(ulong channelId, int moduleId)
        {
            MySqlParameter[] parameters =
            [
                new MySqlParameter("@ChannelID", channelId),
                new MySqlParameter("@ModulID", moduleId)
            ];

            DataTable result = _databaseService.ExecuteSqlQuery("SELECT isActive FROM discord_channel_module WHERE ChannelID = @ChannelID AND ModulID = @ModulID", parameters);

            // Wenn kein Eintrag vorhanden ist, ist das Modul standardmäßig inaktiv
            return result.Rows.Count != 0 && Convert.ToBoolean(result.Rows[0]["isActive"]);
        }

        internal bool AddModuleEntryForChannel(ulong channelId, int moduleId)
        {
            MySqlParameter[] checkParameters =
            [
                new MySqlParameter("@ChannelID", channelId),
                new MySqlParameter("@ModulID", moduleId)
            ];

            DataTable checkResult = _databaseService.ExecuteSqlQuery("SELECT * FROM discord_channel_module WHERE ChannelID = @ChannelID AND ModulID = @ModulID", checkParameters);

            if(checkResult.Rows.Count != 0)
            {
                return false; // Kein neuer Eintrag erforderlich
            }

            MySqlParameter[] insertParameters =
            [
                new MySqlParameter("@ChannelID", channelId),
                new MySqlParameter("@ModulID", moduleId),
                new MySqlParameter("@IsActive", 1) // Standardmäßig aktiv
            ];

            const string insertQuery = "INSERT INTO discord_channel_module (ChannelID, ModulID, isActive) VALUES (@ChannelID, @ModulID, @IsActive)";
            _databaseService.ExecuteSqlQuery(insertQuery, insertParameters);
            return true; // Neuer Eintrag wurde hinzugefügt
        }
    }
}