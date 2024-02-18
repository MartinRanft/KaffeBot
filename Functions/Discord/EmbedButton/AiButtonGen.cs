using Discord;

using KaffeBot.Models.KI.Enums;

namespace KaffeBot.Functions.Discord.EmbedButton
{
    /// <summary>
    /// Class to Generate a Button and Field for AI 
    /// </summary>
    internal class AiButtonGen
    {
        /// <summary>
        /// Lora Button and field generator
        /// </summary>
        /// <param name="embed"></param>
        /// <param name="component"></param>
        /// <param name="fieldName"></param>
        /// <param name="loraSetting"></param>
        /// <param name="loraNumber"></param>
        internal static void AddSettingFieldAndButton(EmbedBuilder embed, ComponentBuilder component, string fieldName, BildConfigEnums.LoraStack? loraSetting, int loraNumber)
        {
            string settingValue = loraSetting.HasValue ? loraSetting.Value.ToString() : "Nicht gesetzt";
            embed.AddField(fieldName, settingValue, true);

            // Erstelle einen Button für jede Lora-Einstellung mit einer eindeutigen CustomId
            component.WithButton($"Ändere {fieldName}", $"change_lora_{loraNumber}", ButtonStyle.Secondary);
        }        
        
        /// <summary>
        /// Model Button and field Generator
        /// </summary>
        /// <param name="embed"></param>
        /// <param name="component"></param>
        /// <param name="fieldName"></param>
        /// <param name="model"></param>
        /// <param name="loraNumber"></param>
        internal static void AddSettingFieldAndButton(EmbedBuilder embed, ComponentBuilder component, string fieldName, BildConfigEnums.Modelle? model, int loraNumber)
        {
            string settingValue = model.HasValue ? model.Value.ToString() : "Nicht gesetzt";
            embed.AddField(fieldName, settingValue, true);

            // Erstelle einen Button für jede Lora-Einstellung mit einer eindeutigen CustomId
            component.WithButton($"Ändere {fieldName}", $"change_model_{loraNumber}", ButtonStyle.Secondary);
        }
    }
}