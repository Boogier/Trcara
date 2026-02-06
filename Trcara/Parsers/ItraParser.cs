using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Trcara.Parsers;

internal class ItraParser : IParser
{
    private const string BaseUrl = "https://itra.run";

    private static readonly Dictionary<string, string> CountryCodes = new()
    {
        ["TUR"] = "Turkey",
        ["MKD"] = "NMK",
        ["BIH"] = "Bosnia",
        ["GEO"] = "Georgia",
        ["MNE"] = "Montenegro"
    };

    private static readonly string[] CountriesToSearch =
    {
        "AL",
        "BA",
        "ME",
        "GE",
        "MK",
        "TR"
    };

    public async Task<List<EventDetails>> GetEventsAsync(KnownRace[] knownRaces)
    {
        Console.WriteLine($"Parsing {BaseUrl}");

        var responseText = await GetDataAsync();

        var events = ParseHtml(responseText, knownRaces);

        return events.Select(e => new EventDetails
        {
            Title = e.Name,
            Date = GetDate(e.Date),
            Location = GetLocation(e.Location),
            Country = GetCountry(e.Location),
            Type = RaceType.Trail,
            Link = $"{BaseUrl}{e.Link}",
            Distance = GetDistance(e.Races),
            Elevation = GetElevation(e.Races)
        }).ToList();
    }

    private static string GetElevation(List<RaceInfo> races)
    {
        return string.Join(", ", races.Select(r => r.Elevation.Replace("+", "").Replace("m", "").Trim()));
    }

    private static string GetDistance(List<RaceInfo> races)
    {
        return string.Join(", ", races.Select(r => r.Distance.Replace("k", "").Trim()));
    }

    private static string GetCountry(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "";
        }

        var code = location.Split(',').Last().Trim().ToUpper();

        return CountryCodes.GetValueOrDefault(code, code);
    }

    private static string GetLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "";
        }

        return location.Split(',')[0];
    }

    private static string GetDate(string date)
    {
        if (!DateTime.TryParse(date, CultureInfo.GetCultureInfo("en-US"), out var parsed))
        {
            // Define regex to match the pattern: startDay - endDay month year
            var regex = new Regex(@"(\d+)\s*-\s*(\d+)\s*(\w+)\s*(\d+)");
            var match = regex.Match(date);

            if (!match.Success)
            {
                return $"??? {date}";
            }

            var startDay = match.Groups[1].Value;
            var month = match.Groups[3].Value;
            var year = match.Groups[4].Value;

            var startDateStr = $"{startDay} {month} {year}";
            if (!DateTime.TryParse(startDateStr, CultureInfo.GetCultureInfo("en-US"), out parsed))
            {
                return $"?????? {date} -> {startDateStr}";
            }
        }

        return parsed.ToString("dd.MM.yyyy");
    }

    private static async Task<string> GetDataAsync()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All
        };

        using var client = new HttpClient(handler);

        // STEP 1 — GET PAGE (to obtain session + antiforgery cookies)
        var getUrl = $"{BaseUrl}/Races/RaceCalendar";

        var getHtml = await client.GetStringAsync(getUrl);

        // STEP 2 — Extract RequestVerificationToken from HTML
        var token = Regex.Match(getHtml,
                @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;

        var dateStart = Settings.FilterDateFrom > DateTime.Today ? Settings.FilterDateFrom : DateTime.Today;
        var body = $"Input.SearchTerms=&{string.Join('&', CountriesToSearch.Select(c => $"Input.Country={c}"))}&Input.DateStart={dateStart:dd-MM-yyyy}&__RequestVerificationToken={WebUtility.UrlEncode(token)}";

        Console.WriteLine(body);

        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        // STEP 4 — POST with cookies and token
        var postUrl = "https://itra.run/Races/RaceCalendar";

        var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
        request.Content = content;

        // ITRA expects this header (XHR)
        //request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        //Console.WriteLine("POST " + postUrl);
        var response = await client.SendAsync(request);

        var respText = await response.Content.ReadAsStringAsync();
        return respText;
    }


    private static List<EventInfo> ParseHtml(string fullScriptContent, KnownRace[] knownRaces)
    {
        var events = new List<EventInfo>();

        // 1. Extract HTML strings inside raceSearchJsonSidePopupNew = [ "...", "...", ... ]
        var regex = new Regex("\"(<div[\\s\\S]*?</div>)\"", RegexOptions.Multiline);
        var matches = regex.Matches(fullScriptContent);

        Console.WriteLine($"Found {matches.Count} events.");
        foreach (Match m in matches)
        {
            var html = m.Groups[1].Value.Replace("\\\"", "\"");
            var e = ParseOneEvent(html);
            if (!knownRaces.Any(kr => string.Equals(kr.Name, e.Name)))
            {
                events.Add(e);
            }
        }

        return events;
    }

    private static EventInfo ParseOneEvent(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var e = new EventInfo();

        // Event name
        e.Name = doc.DocumentNode
            .SelectSingleNode("//div[@class='event_name']//h4")
            ?.InnerText
            ?.Trim();

        // Event link
        e.Link = doc.DocumentNode
            .SelectSingleNode("//div[@class='event_name']//a")
            ?.GetAttributeValue("href", "")
            ?.Trim();

        // Date (full text inside date div)
        e.Date = doc.DocumentNode
            .SelectSingleNode("//div[@class='date']")
            ?.InnerText
            ?.Trim()
            .Replace("\n", " ");

        // Location (e.g. "RED TOWER, TUR")
        e.Location = doc.DocumentNode
            .SelectSingleNode("//div[@class='location']")
            ?.ChildNodes
            .Where(n => n.NodeType == HtmlNodeType.Text)
            .FirstOrDefault()
            ?.InnerText
            ?.Trim();

        // Races
        var raceNodes = doc.DocumentNode.SelectNodes("//div[@class='races-boxes']/div[@class='boxes']");

        if (raceNodes != null)
        {
            foreach (var race in raceNodes)
            {
                var r = new RaceInfo();

                r.Link = race.SelectSingleNode(".//a")?.GetAttributeValue("href", "").Trim();
                r.Distance = race.SelectSingleNode(".//div[@class='count']")?.InnerText?.Trim();
                r.Elevation = race.SelectSingleNode(".//div[@class='distance']")?.InnerText?.Trim();

                e.Races.Add(r);
            }
        }

        return e;
    }
}

public class EventInfo
{
    public string Name { get; set; }
    public string Date { get; set; }
    public string Link { get; set; }
    public string Location { get; set; }
    public List<RaceInfo> Races { get; set; } = new();
}

public class RaceInfo
{
    public string Distance { get; set; } // e.g. "18 k"
    public string Elevation { get; set; } // e.g. "+912 m"
    public string Link { get; set; }
}