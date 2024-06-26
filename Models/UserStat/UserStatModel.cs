﻿namespace KaffeBot.Models.UserStat
{
    internal sealed class UserStatModel
    {
        public long DiscordID { get; set; }
        public long DiscordServerID { get; set; }
        public DateTime? Birthday { get; set; }
        public uint InternServerID { get; set; }
        public uint DBUserID { get; set; }
        public int ImageCount { get; set; }
        public int UrlCount { get; set; }
        public int WordCount { get; set; }
    }
}