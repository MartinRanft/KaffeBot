using KaffeBot.Models.KI.Enums;

namespace KaffeBot.Models.KI
{
    internal sealed class UserAiSettings
    {
        public long? UserId { get; set; }

        public BildConfigEnums.LoraStack? Lora1 { get; set; }

        public double? Strength1 { get; set; }
        
        public BildConfigEnums.LoraStack? Lora2 { get; set; }
        
        public double? Strength2 { get; set; }
        
        public BildConfigEnums.LoraStack? Lora3 { get; set; }
        
        public double? Strength3 { get; set; }
        
        public BildConfigEnums.LoraStack? Lora4 { get; set; }
        
        public double? Strength4 { get; set; }
        
        public BildConfigEnums.LoraStack? Lora5 { get; set; }
        
        public double? Strength5 { get; set; }

        public BildConfigEnums.Modelle? Model { get; set; }
        
        public double? Cfg { get; set; }
    }
}