using HtmlAgilityPack;
using System.Net;

namespace Trcara.Parsers;

internal class TrkaParser : IParser
{
    public async Task<List<EventDetails>> GetEventsAsync(KnownRace[] knownRaces)
    {
        var baseUrl = new Uri("https://www.trka.rs");
        Console.WriteLine($"Parsing {baseUrl}");

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All
        };
        handler.CookieContainer.Add(new Cookie
        {
            Name = "django_language",
            Value = "sr-RS",
            Domain = baseUrl.Host
        });

        var httpClient = new HttpClient(handler);
        var html = await httpClient.GetStringAsync(baseUrl);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Select all event cards
        var eventNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'card') and contains(@class, 'event-tile')]");

        if (eventNodes == null)
        {
            Console.WriteLine("⚠️ No events found. Check the page or selector.");
            return [];
        }

        Console.WriteLine($"Found {eventNodes.Count} events.");

        var events = new List<EventDetails>();

        foreach (var card in eventNodes)
        {
            var titleNode = card.SelectSingleNode(".//h5[contains(@class, 'card-title')]");
            var title = WebUtility.HtmlDecode(titleNode?.InnerText?.Trim() ?? "");

            if (knownRaces.Any(kr => string.Equals(kr.Name, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var dateNode = card.SelectSingleNode(".//p[contains(@class, 'card-text')]/small[contains(@class, 'text-body-secondary')]");
            var linkNode = card.SelectSingleNode(".//a[@href]");

            var date = dateNode?.InnerText?.Trim() ?? "";
            var trkaLink = linkNode?.GetAttributeValue("href", "") ?? "";

            (string Deadline, string Contact, string MoreDetailsLink) details = new();
            if (!string.IsNullOrEmpty(trkaLink) && !trkaLink.StartsWith("http"))
            {
                trkaLink = new Uri(baseUrl, trkaLink).ToString();
                details = ParseEventDetails(await httpClient.GetStringAsync(trkaLink));
            }

            var facebook = details.MoreDetailsLink.Has("facebook") ? details.MoreDetailsLink : "";
            var instagram = details.MoreDetailsLink.Has("instagram") ? details.MoreDetailsLink : "";

            var link = !string.IsNullOrWhiteSpace(details.MoreDetailsLink) && string.IsNullOrWhiteSpace(facebook) && string.IsNullOrWhiteSpace(instagram)
                ? details.MoreDetailsLink
                : trkaLink;

            events.Add(new EventDetails("", title, "", "", date, link, facebook, instagram, details.Deadline, details.Contact, "Serbia", ""));
        }

        return events;
    }

    /// <summary>
    /// Parses event HTML and extracts:
    /// - "Крајњи рок за пријаву"
    /// - "Контакт"
    /// - "Више детаља" (as a link)
    /// Returns (Deadline, Contact, MoreDetailsLink)
    /// </summary>
    public static (string? Deadline, string? Contact, string? MoreDetailsLink) ParseEventDetails(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var deadline = GetFieldValue("Крајњи рок за пријаву:") ?? GetFieldValue("Registrations deadline:");
        var contact = GetFieldValue("Контакт:") ?? GetFieldValue("Contact:");
        var moreDetails = GetFieldValue("Више детаља:") ?? GetFieldValue("More details:");

        return (deadline, contact, moreDetails);

        string? GetFieldValue(string labelText)
        {
            string? result = null;
            try
            {
                var labelNode = doc.DocumentNode
                    .SelectSingleNode($"//label[normalize-space(text())='{labelText}']");

                if (labelNode == null) // can be null
                {
                    return null;
                }

                var parentNode = labelNode
                    .ParentNode
                    .ParentNode;
                var link = parentNode.SelectSingleNode(".//a");

                if (link != null) // can be null
                {
                    result = link.GetAttributeValue("href", "").Trim();
                }
                else
                {
                    var valueNode = labelNode.ParentNode.NextSibling.NextSibling;

                    result = valueNode.InnerText.Trim();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing event details: {e.Message}");
            }

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
    }
}