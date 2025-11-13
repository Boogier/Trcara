namespace Trcara
{
    internal static class StringExtensions
    {
        public static bool Has(this string s, string what)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            return s.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
