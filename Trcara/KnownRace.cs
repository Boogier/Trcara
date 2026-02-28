namespace Trcara;

internal readonly record struct KnownRace(string Name, DateTime Date)
{
    public bool IsEqual(string? anotherName, DateTime anotherDate)
    {
        if (ReferenceEquals(Name, anotherName))
        {
            return true;
        }

        if (Name is null || anotherName is null)
        {
            return false;
        }

        var nameComparable = ToComparableName(Name);
        var anotherNameComparable = ToComparableName(anotherName);
        if (string.Equals(nameComparable, anotherNameComparable, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Date == anotherDate && (nameComparable.Contains(anotherNameComparable) || anotherNameComparable.Contains(nameComparable));
    }

    private static string ToComparableName(string name)
    {
        return Utils.RemoveDiacritics(name).ToLower().Replace("sky race", "skyrace");
    }
}