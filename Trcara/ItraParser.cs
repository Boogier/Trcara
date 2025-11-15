using HtmlAgilityPack;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Trcara
{
    internal  static class ItraParser
    {
        private const string BaseUrl = "https://itra.run";
        private static Dictionary<string, string> CountryCodes = new Dictionary<string, string>()
        {
            ["TUR"] = "Turkey",
            ["MKD"] = "NMK"
        };

        internal static async Task<List<EventDetails>> GetAsync(string[] knownRuns)
        {
            var responseText = await GetDataAsync();

            var events = ParseHtml(responseText, knownRuns);

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

            if (CountryCodes.TryGetValue(code, out var country))
            {
                return country;
            }

            return "???";
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
                Regex regex = new Regex(@"(\d+)\s*-\s*(\d+)\s*(\w+)\s*(\d+)");
                Match match = regex.Match(date);

                if (!match.Success)
                {
                    return $"??? {date}";
                }

                string startDay = match.Groups[1].Value;
                string month = match.Groups[3].Value;
                string year = match.Groups[4].Value;

                string startDateStr = $"{startDay} {month} {year}";
                if (!DateTime.TryParse(startDateStr, CultureInfo.GetCultureInfo("en-US"), out parsed))
                {
                    return $"?????? {date} -> {startDateStr}";
                }
            }

            return parsed.ToString("dd.MM.yyyy");
        }

        static async Task<string> GetDataAsync()
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

            //Console.WriteLine("Token: " + token);

            // STEP 3 — Build POST body
            var body = $"Input.SearchTerms=&Input.Country=AL&Input.Country=BA&Input.Country=ME&Input.Country=MK&Input.Country=TR&Input.DateStart=01-01-2026&__RequestVerificationToken={WebUtility.UrlEncode(token)}";
            //var body = $"Input.SearchTerms=&Input.Country=MK&Input.DateStart=01-01-2026&__RequestVerificationToken={WebUtility.UrlEncode(token)}";

            var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

            // STEP 4 — POST with cookies and token
            var postUrl = "https://itra.run/Races/RaceCalendar";

            var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
            request.Content = content;

            // ITRA expects this header (XHR)
            //request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            //Console.WriteLine("POST " + postUrl);
            var response = await client.SendAsync(request);

            string respText = await response.Content.ReadAsStringAsync();
            return respText;
        }


        private static List<EventInfo> ParseHtml(string fullScriptContent, string[] knownRuns)
        {
            var events = new List<EventInfo>();

            // 1. Extract HTML strings inside raceSearchJsonSidePopupNew = [ "...", "...", ... ]
            var regex = new Regex("\"(<div[\\s\\S]*?</div>)\"", RegexOptions.Multiline);
            var matches = regex.Matches(fullScriptContent);

            foreach (Match m in matches)
            {
                string html = m.Groups[1].Value.Replace("\\\"", "\"");
                var e = ParseOneEvent(html);
                if (!knownRuns.Any(kr => string.Equals(kr, e.Name)))
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
        public string Distance { get; set; }   // e.g. "18 k"
        public string Elevation { get; set; }  // e.g. "+912 m"
        public string Link { get; set; }
    }
}
