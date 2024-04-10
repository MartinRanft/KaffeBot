namespace KaffeBot.Models.KI.Enums
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class AiAttribut(string beschreibung) : Attribute
    {
        public string Beschreibung { get; } = beschreibung;
    }
}