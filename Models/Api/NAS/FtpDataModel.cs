using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Models.Api.NAS
{
    internal class FtpDataModel
    {
        public string? fileName { get; set; }
        public byte[]? data { get; set; }
    }
}
