namespace Trcara.Parsers;

internal interface IParser
{
    Task<List<EventDetails>> GetEventsAsync(string[] knownRaces);
}