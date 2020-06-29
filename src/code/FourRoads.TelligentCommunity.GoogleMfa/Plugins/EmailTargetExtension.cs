using Telligent.Evolution.Extensibility.Email.Version1;

namespace FourRoads.TelligentCommunity.Mfa.Plugins
{
    internal static class EmailTargetExtension
    {
        public static string ToTemplateTypeString(this EmailTarget emumValue)
        {
            return "email_" + emumValue.ToString().ToLower();
        }
    }
}