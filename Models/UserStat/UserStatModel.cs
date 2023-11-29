using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Models.UserStat
{
    internal class UserStatModel
    {
        public ulong DiscordID { get; set; }
        public ulong DiscordServerID { get; set; }
        public DateTime? Birthday { get; set; }
        public int InternServerID { get; set; }
        public ulong DBUserID { get; set; }
        public int ImageCount { get; set; }
        public int UrlCount { get; set; }
        public int WordCount { get; set; }
    }
}
