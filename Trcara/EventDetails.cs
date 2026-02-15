namespace Trcara;

internal readonly record struct EventDetails(
    string Type,
    string Title,
    string? Distance,
    string? Elevation,
    string DateString,
    string Link,
    string Facebook,
    string Instagram,
    string? Deadline,
    string? Contact,
    string Country,
    string Location,
    Source Source
)
{
    public DateTime Date => Utils.ParseDate(DateString);
}