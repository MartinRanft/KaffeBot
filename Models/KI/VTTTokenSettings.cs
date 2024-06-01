using Newtonsoft.Json;

namespace KaffeBot.Models.KI
{
    public class VTTTokenSettings
    {
         [JsonProperty("userId")]
         public string? UserId { get; set; }
         
         [JsonProperty("positivePrompt")]
         public string? PositivePrompt { get; set; }
    }
    
    public sealed class TokenGenerated
    {
        public List<byte[]>? Image { get; set; }
    }
}