using KaffeBot.Interfaces.DB;

using MySqlConnector;

namespace KaffeBot.Services.Discord.Module
{
    internal class CheckModules(IDatabaseService databaseService)
    {
        private readonly IDatabaseService _databaseService = databaseService;

        internal int? GetModuleIdByName(string moduleName)
        {
            MySqlParameter[] parameters =
            [
                new("@ModuleName", moduleName)
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
                new("@ChannelID", channelId),
                new("@ModulID", moduleId)
            ];

            var result = _databaseService.ExecuteSqlQuery("SELECT isActive FROM discord_channel_module WHERE ChannelID = @ChannelID AND ModulID = @ModulID", parameters);

            // Wenn kein Eintrag vorhanden ist, ist das Modul standardmäßig inaktiv
            if(result.Rows.Count == 0)
            {
                return false;
            }

            return Convert.ToBoolean(result.Rows[0]["isActive"]);
        }

        internal bool AddModuleEntryForChannel(ulong channelId, int moduleId)
        {
            MySqlParameter[] checkParameters =
            [
                new("@ChannelID", channelId),
                new("@ModulID", moduleId)
            ];

            var checkResult = _databaseService.ExecuteSqlQuery("SELECT * FROM discord_channel_module WHERE ChannelID = @ChannelID AND ModulID = @ModulID", checkParameters);

            if(checkResult.Rows.Count == 0)
            {
                MySqlParameter[] insertParameters =
                [
                    new("@ChannelID", channelId),
                    new("@ModulID", moduleId),
                    new("@IsActive", 1) // Standardmäßig aktiv
                ];

                string insertQuery = "INSERT INTO discord_channel_module (ChannelID, ModulID, isActive) VALUES (@ChannelID, @ModulID, @IsActive)";
                _databaseService.ExecuteSqlQuery(insertQuery, insertParameters);
                return true; // Neuer Eintrag wurde hinzugefügt
            }

            return false; // Kein neuer Eintrag erforderlich
        }
    }
}