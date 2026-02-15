namespace Trcara;

internal readonly record struct KnownRace(string Name, DateTime Date)
{
    public bool IsEqual(string? anotherName)
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
        return string.Equals(nameComparable, anotherNameComparable, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToComparableName(string name)
    {
        return name.ToLower().Replace("sky race", "skyrace");
    }
}