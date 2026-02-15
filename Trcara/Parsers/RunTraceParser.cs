using System.Globalization;
using HtmlAgilityPack;

namespace Trcara.Parsers;

internal class RunTraceParser : IParser
{
    public async Task<List<EventDetails>> GetEventsAsync(KnownRace[] knownRaces)
    {
        var baseUrl = "https://runtrace.net";

        Console.WriteLine($"Parsing {baseUrl}");

        var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync(baseUrl);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var races = new List<EventDetails>();

        // Select all race blocks
        var raceNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'grid__item') and contains(@class, 'js-event_info')]");
        if (raceNodes == null)
        {
            return races;
        }

        Console.WriteLine($"Found {raceNodes.Count} events.");

        foreach (var race in raceNodes)
        {
            var infoNode = race.SelectSingleNode(".//div[contains(@class, 'grid__race__info')]");
            if (infoNode == null)
            {
                continue;
            }

            var title = infoNode.SelectSingleNode(".//a[contains(@class, 'race-title')]")?.InnerText?.Trim();
            if (string.IsNullOrWhiteSpace(title) || knownRaces.Any(kr => kr.IsEqual(title)))
            {
                continue;
            }

            var date = infoNode.SelectSingleNode(".//div[contains(@class, 'race-date')]")?.InnerText?.Trim();
            var location = infoNode.SelectSingleNode(".//div[contains(@class, 'race-location')]/span")?.InnerText?.Trim();

            // Find sign-up link (if exists)
            var slug = race.GetAttributeValue("data-slug_event", null);
            var signup = infoNode.SelectSingleNode(".//a[@title='Sign up']")?.GetAttributeValue("href", null);
            var participants = infoNode.SelectSingleNode(".//a[@title='Participants']")?.GetAttributeValue("href", null);

            races.Add(new EventDetails
            {
                Source = Source.RunTrace,
                Title = title,
                DateString = GetDate(date),
                Country = "Serbia",
                Location = location,
                Link = !string.IsNullOrWhiteSpace(slug) ? new Uri(new Uri(baseUrl), $"?event={slug}").ToString()
                    : !string.IsNullOrWhiteSpace(signup) ? new Uri(new Uri(baseUrl), signup).ToString()
                    : !string.IsNullOrWhiteSpace(participants) ? new Uri(new Uri(baseUrl), participants).ToString()
                    : baseUrl
            });
        }

        return races;
    }

    private static string GetDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return string.Empty;
        }

        if (!DateTime.TryParse(dateStr + ":00", CultureInfo.GetCultureInfo("ru-RU"), out var date))
        {
            return dateStr;
        }

        return date.ToString("dd.MM.yyyy");
        //return date.ToString(@"dd\/MM\/yyyy HH:mm:ss");
    }
}