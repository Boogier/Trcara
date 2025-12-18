namespace Trcara;

internal interface IParser
{
    Task<List<EventDetails>> GetEventsAsync(string[] knownRaces);
}