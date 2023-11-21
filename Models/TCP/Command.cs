using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Models.TCP
{
    internal class CommandModel
    {
        public string? Command { get; set; }
        public List<ServerObject>? CMDfor { get; set; }
    }

    public class ServerObject
    {
        public string? User {  get; set; }
        public string? Password { get; set; }
        public string? IV { get; set; }
        public string? ServerID { get; set;}
        public string? ChannelID { get; set;}
    }
}
