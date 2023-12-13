namespace KaffeBot.Models.Api.NAS
{
    internal abstract class FtpDataModel
    {
        public string? FileName { get; set; }
        public byte[]? Data { get; set; }
    }
}