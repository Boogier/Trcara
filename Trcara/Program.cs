using System.Reflection;
using Trcara;

string csvPath = @"c:\d\z\events.csv";
var knownRuns = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "KnownRuns.txt"));

var trkaEvents = await TrkaParser.Get(knownRuns);
var runtraceEvents = await RunTraceParser.Get(knownRuns);

var events = trkaEvents.Union(runtraceEvents).ToList();

using (var writer = new StreamWriter(csvPath))
{
    //writer.WriteLine("Type,Title,Distance,ElevationGane,Date,Deadline,Link,Facebook,Instagram,eMail,Country");
    foreach (var e in events)
    {
        var eventType = GetEventType(e.Title);
        var date = e.Date.TrimEnd('.');
        var linkCleared = Uri.EscapeUriString(e.Link);

        writer.WriteLine($"\"{eventType}\",\"{e.Title}\",,,\"{date}\",\"{e.Deadline}\",\"{linkCleared}\",\"{e.Facebook}\",\"{e.Instagram}\",\"{e.Contact}\",\"Serbia\",\"{e.Location}\"");
    }
}

Console.WriteLine($"Extracted {events.Count} events → {csvPath}");

string GetEventType(string title)
{
    if (
        title.Has("trail")
        || title.Has("trejl")
        || title.Has("planinarski")
        )
    {
        return "Trail";
    }

    if (
        title.Has("marathon")
        || title.Has("maraton")
        || title.Has("desetka")
        )
    {
        return "шоссе";
    }

    if (
        title.Has("ocr")
        )
    {
        return "шоссе";
    }

    return "Other";
}

internal readonly record struct EventDetails(string Title, string Date, string Link, string Facebook, string Instagram, string Deadline, string Contact, string Location);
