namespace KaffeBot.Models.TCP
{
    internal sealed class CommandModel
    {
        public string? Command { get; set; }
        public List<ServerObject>? CmDfor { get; init; }
    }

    internal sealed class ServerObject
    {
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? IV { get; set; }
        public string? ServerID { get; set; }
        public string? ChannelID { get; set; }
        public string? Data { get; set; }
    }
}