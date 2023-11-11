using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaffeBot.Models.Api.NAS
{
    internal class FtpDataModel
    {
        public string? FileName { get; set; }
        public byte[]? Data { get; set; }
    }
}
