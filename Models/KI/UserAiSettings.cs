using KaffeBot.Models.KI.Enums;

namespace KaffeBot.Models.KI
{
    internal sealed class UserAiSettings
    {
        internal ulong? UserId { get; set; }

        internal BildConfigEnums.LoraStack? Lora1 { get; set; }

        internal double? Strength1 { get; set; }
        
        internal BildConfigEnums.LoraStack? Lora2 { get; set; }
        
        internal double? Strength2 { get; set; }
        
        internal BildConfigEnums.LoraStack? Lora3 { get; set; }
        
        internal double? Strength3 { get; set; }
        
        internal BildConfigEnums.LoraStack? Lora4 { get; set; }
        
        internal double? Strength4 { get; set; }
        
        internal BildConfigEnums.LoraStack? Lora5 { get; set; }
        
        internal double? Strength5 { get; set; }

        internal BildConfigEnums.Modelle? Model { get; set; }
        
        internal double? Cfg { get; set; }
        
        internal DateTime? LoadedDateTime { get; set; }
        
    }
}