using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Models.WSS
{
    internal abstract class CommandModel
    {
        public string? Command { get; set; }
        public List<ServerObject>? CMDfor { get; set; }
    }

    internal abstract class ServerObject
    {
        public string? User {  get; set; }
        public string? Password { get; set; }
        public string? ServerID { get; set;}
        public string? ChannelID { get; set;}
    }
}
