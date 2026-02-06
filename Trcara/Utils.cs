using System.Globalization;

namespace Trcara;

internal static class Utils
{
    public static readonly DateTime EmptyDate = DateTime.MinValue;

    public static DateTime ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return EmptyDate;
        }

        s = s.Trim(' ', '"');
        return DateTime.TryParse(s, CultureInfo.GetCultureInfo("ru-RU"), out var d) ? d : EmptyDate;
    }
}