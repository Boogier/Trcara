using System.Reflection;
using Trcara;

string csvPath = @"c:\d\z\events.csv";
var knownRuns = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "KnownRuns.txt"));

var events = new List<EventDetails>();
var parsers = PasrerProvider.GetParsers();
foreach (var parser in parsers)
{
    events.AddRange(await parser.GetEventsAsync(knownRuns));
}

using (var writer = new StreamWriter(csvPath))
{
    //writer.WriteLine("Type,Title,Distance,ElevationGane,Date,Deadline,Link,Facebook,Instagram,eMail,Country");
    foreach (var e in events)
    {
        var eventType = string.IsNullOrWhiteSpace(e.Type) ? GetEventType(e.Title) : e.Type;
        var date = e.Date.TrimEnd('.');
        var linkCleared = string.IsNullOrWhiteSpace(e.Link) ? "" : Uri.EscapeUriString(e.Link);

        writer.WriteLine($"\"{eventType}\",\"{e.Title}\",\"{e.Distance}\",\"{e.Elevation}\",\"{date}\",\"{e.Deadline}\",\"{linkCleared}\",\"{e.Facebook}\",\"{e.Instagram}\",\"{e.Contact}\",\"{e.Country}\",\"{e.Location}\"");
    }
}

Console.WriteLine($"Extracted {events.Count} events → {csvPath}");

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
