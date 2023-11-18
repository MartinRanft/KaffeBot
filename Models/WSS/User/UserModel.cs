using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Models.WSS.User
{
    internal class UserModel
    {
        public ulong DiscordID { get; set; }
        public required string DiscordName { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public bool IsServerMod { get; set; }
        public int? ApiUser { get; set; }
        public string? ApiKey { get; set; }
    }
}
