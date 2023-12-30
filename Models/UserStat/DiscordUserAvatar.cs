namespace KaffeBot.Models.UserStat
{
    internal sealed class DiscordUserAvatar
    {
        public int ID { get; set; }
        public long UserID { get; set; }
        public string DiscordName { get; set; } = "";
        public byte[]? DiscordAvatar { get; set; }
    }
}