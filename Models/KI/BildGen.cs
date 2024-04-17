using Newtonsoft.Json;

namespace KaffeBot.Models.KI
{
    public sealed class ImagesSettings
    {
        [JsonProperty("userSettings")]
        public UserAiSettings? UserAiSettings { get; set; }

        [JsonProperty("positivePrompt")]
        public string? PositivePrompt { get; set; }

        [JsonProperty("negativePrompt")]
        public string? NegativePrompt { get; set; }
    }

    public sealed class ImageGenerated
    {
        public List<byte[]>? Image { get; set; }
    }
}