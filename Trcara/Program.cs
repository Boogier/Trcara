using System.Globalization;
using System.Text;
using Trcara;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine($"Filtering races starting from {Settings.FilterDateFrom:dd.MM.yyyy}.");

var knownRaces = await KnownRacesProvider.GetKnownRunsAsync();

var events = new List<EventDetails>();
var parsers = PasrerProvider.GetParsers();
foreach (var parser in parsers)
{
    try
    {
        var parsedEvents = await parser.GetEventsAsync(knownRaces);
        var filteredEvents = parsedEvents
            .Where(e =>
            !DateTime.TryParse(e.Date, CultureInfo.GetCultureInfo("ru-RU"), out var date)
            || date >= Settings.FilterDateFrom)
            .ToList();

        events.AddRange(filteredEvents);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to get events from {parser.GetType().Name}: {ex.Message}.");
    }
}

if (events.Count > 0)
{
    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "trcara.txt");
    using (var writer = new StreamWriter(filePath))
    {
        //writer.WriteLine("Type,Title,Distance,ElevationGane,Date,Deadline,Link,Facebook,Instagram,eMail,Country");
        foreach (var e in events)
        {
            var eventType = string.IsNullOrWhiteSpace(e.Type) ? GetEventType(e.Title) : e.Type;
            var date = e.Date.TrimEnd('.');
            var linkCleared = string.IsNullOrWhiteSpace(e.Link) ? "" : Uri.EscapeUriString(e.Link);

            writer.WriteLine($"\"{eventType}\"\t\"{e.Title}\"\t\"{e.Distance}\"\t\"{e.Elevation}\"\t\"{date}\"\t\"{e.Deadline}\"\t\"{linkCleared}\"\t\"{e.Facebook}\"\t\"{e.Instagram}\"\t\"{e.Contact}\"\t\"{e.Country}\"\t\"{e.Location}\"");
        }
    }

    Console.WriteLine(@$"
Extracted {events.Count} events to file {filePath}.

Now you can open it with notepad and copy/paste the events to Trcara spreadsheet.
Then select 'Дата' column and format it as Date. 
Then you can sort by 'Дата' column.");
}
else
{
    Console.WriteLine(@"
No new events found.");
}

Console.WriteLine(@"
Press any key to exit.");

Console.ReadKey();

string GetEventType(string title)
{
    if (
        title.Has("trail")
        || title.Has("trejl")
        || title.Has("planinarski")
        || title.Has("ultra")
        )
    {
        return RaceType.Trail;
    }

    if (
        title.Has("marathon")
        || title.Has("maraton")
        || title.Has("desetka")
        )
    {
        return RaceType.Asphalt;
    }

    if (
        title.Has("ocr")
        )
    {
        return RaceType.Ocr;
    }

    return RaceType.Other;
}
