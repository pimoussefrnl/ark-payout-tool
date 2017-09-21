using System.Text.RegularExpressions;

namespace Ark.Payout.UI.Helpers
{
    public class StaticMethods
    {
        public static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.]");
            return !regex.IsMatch(text);
        }
    }
}