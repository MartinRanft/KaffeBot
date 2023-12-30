namespace KaffeBot.Models.TCP.User
{
    internal class UserModel
    {
        public ulong DiscordID { get; set; }
        public string? DiscordName { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public bool IsServerMod { get; set; }
        public int? ApiUser { get; set; }
        public string? ApiKey { get; set; }
    }
}