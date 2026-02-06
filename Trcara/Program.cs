using System.Globalization;
using System.Reflection;
using System.Text;
using Trcara;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine($"Trčara version {Assembly.GetExecutingAssembly().GetName().Version}");
Console.WriteLine($"Filtering races starting from {Settings.FilterDateFrom:dd.MM.yyyy}.");

var knownRaces = await KnownRacesProvider.GetKnownRacesAsync();

var events = new List<EventDetails>();
var parsers = ParserProvider.GetParsers();
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
    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "trcara.txt");
    using (var writer = new StreamWriter(filePath))
    {
        //writer.WriteLine("Type,Title,Distance,ElevationGain,Date,Deadline,Link,Facebook,Instagram,eMail,Country");
        foreach (var e in events)
        {
            var eventType = string.IsNullOrWhiteSpace(e.Type) ? GetEventType(e.Title) : e.Type;
            var date = e.Date.TrimEnd('.');
            var linkCleared = string.IsNullOrWhiteSpace(e.Link) ? "" : Uri.EscapeUriString(e.Link);

            writer.WriteLine($"{eventType}\t{e.Title}\t{e.Distance}\t{e.Elevation}\t{date}\t{e.Deadline}\t{linkCleared}\t{e.Facebook}\t{e.Instagram}\t{e.Contact}\t{e.Country}\t{e.Location}");
        }
    }

    Console.WriteLine(@$"
Extracted {events.Count} events to file {filePath}.");
    EnumerateFoundEvents(events, knownRaces);
    Console.WriteLine(@"

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

static void EnumerateFoundEvents(List<EventDetails> events, KnownRace[] knownRaces)
{
    foreach (var ev in events.OrderBy(e => Utils.ParseDate(e.Date)))
    {
        Console.WriteLine($"    {ev.Date} {ev.Title}");
        var similarRaces = FindSimilarRaces(ev, knownRaces);
        if (similarRaces.Count > 0)
        {
            Log.Warning("      Similar races found, check maybe they already exist in the list:");
            foreach (var knownRace in similarRaces)
            {
                Log.Warning($"          {knownRace.Date:dd.MM.yyyy} {knownRace.Name}");
            }
        }
    }
}

static string GetEventType(string title)
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

static List<KnownRace> FindSimilarRaces(EventDetails ev, KnownRace[] knownRaces)
{
    var eventNameWords = ExtractWords(ev.Title);

    return knownRaces
        .Where(kr => ExtractWords(kr.Name).Any(word => eventNameWords.Contains(word)))
        .ToList();

    //var parsedDate = Utils.ParseDate(ev.Date);
    //return knownRaces1.Where(kr => Math.Abs(kr.Date.Subtract((DateTime)parsedDate).TotalDays) <= 3);
}

static List<string> ExtractWords(string s)
{
    return s
        .Split(' ', ',', '.', '-', '&')
        .Select(s => s.Trim(' ', '"', '\'').ToLower())
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Where(s => !int.TryParse(s, out _))
        .Where(s => s is not ("kolo" or "vtl" or "втл" or "trail" or "ttls" or "ultra" or "maraton" or "marathon" or "race" or "run" or "challenge" or "ocr"))
        .ToList();
}