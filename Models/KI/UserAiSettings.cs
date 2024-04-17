using KaffeBot.Models.KI.Enums;

using Newtonsoft.Json;

namespace KaffeBot.Models.KI
{
    public sealed class UserAiSettings
    {
        [JsonProperty("userId")]
        public ulong? UserId { get; set; }

        [JsonProperty("lora1")]
        public BildConfigEnums.LoraStack? Lora1 { get; set; }

        [JsonProperty("strength1")]
        public double? Strength1 { get; set; }
        
        [JsonProperty("lora2")]
        public BildConfigEnums.LoraStack? Lora2 { get; set; }
        
        [JsonProperty("strength2")]
        public double? Strength2 { get; set; }
        
        [JsonProperty("lora3")]
        public BildConfigEnums.LoraStack? Lora3 { get; set; }
        
        [JsonProperty("strength3")]
        public double? Strength3 { get; set; }
        
        [JsonProperty("lora4")]
        public BildConfigEnums.LoraStack? Lora4 { get; set; }
        
        [JsonProperty("strength4")]
        public double? Strength4 { get; set; }
        
        [JsonProperty("lora5")]
        public BildConfigEnums.LoraStack? Lora5 { get; set; }
        
        [JsonProperty("strength5")]
        public double? Strength5 { get; set; }

        [JsonProperty("model")]
        public BildConfigEnums.Modelle? Model { get; set; }
        
        [JsonProperty("cfg")]
        public double? Cfg { get; set; }
        
        [JsonProperty("loadedDateTime")]
        public DateTime? LoadedDateTime { get; set; }
        
    }
}