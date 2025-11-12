using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

static class Program
{
    static async Task Main()
    {
        var knownRuns = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "KnownRuns.txt"));


        string baseUrl = "https://www.trka.rs";
        //string url = baseUrl + "/events/_filter_by_race_type/7/"; // Example: "Трејл" events
        string csvPath = @"c:\d\z\trka_events.csv";

        var httpClient = new HttpClient();
        //var html = await httpClient.GetStringAsync(url);
        var html = await httpClient.GetStringAsync(baseUrl);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Select all event cards
        var eventNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'card') and contains(@class, 'event-tile')]");

        if (eventNodes == null)
        {
            Console.WriteLine("⚠️ No events found. Check the page or selector.");
            return;
        }

        var events = new List<(string Title, string Date, string Link, string Deadline, string Contact, string MoreDetailsLink)>();

        foreach (var card in eventNodes)
        {
            var titleNode = card.SelectSingleNode(".//h5[@class='card-title']");
            string title = titleNode?.InnerText?.Trim() ?? "";

            if (knownRuns.Any(kr => string.Equals(kr, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var dateNode = card.SelectSingleNode(".//p[@class='card-text']/small[contains(@class, 'text-body-secondary')]");
            var linkNode = card.SelectSingleNode(".//a[@href]");

            string date = dateNode?.InnerText?.Trim() ?? "";
            string trkaLink = linkNode?.GetAttributeValue("href", "") ?? "";

            (string Deadline, string Contact, string MoreDetailsLink) details = new();
            if (!string.IsNullOrEmpty(trkaLink) && !trkaLink.StartsWith("http"))
            {
                trkaLink = new Uri(new Uri(baseUrl), trkaLink).ToString();
                details = ParseEventDetails(await httpClient.GetStringAsync(trkaLink));
            }

            events.Add((title, date, trkaLink, details.Deadline, details.Contact, details.MoreDetailsLink));
            //if (!string.IsNullOrEmpty(title))
            //{
            //}
        }

        using (var writer = new StreamWriter(csvPath))
        {
            writer.WriteLine("Type,Title,Distance,ElevationGane,Date,Deadline,Link,Facebook,Instagram,eMail,Country");
            foreach (var e in events)
            {
                var link = !string.IsNullOrWhiteSpace(e.MoreDetailsLink) && e.MoreDetailsLink.Has("trka.rs")
                    ? e.MoreDetailsLink
                    : e.Link;

                var eventType = GetEventType(e.Title);
                var facebook = link.Has("facebook") ? link : "";
                var instagram = link.Has("instagram") ? link : "";
                var date = e.Date.TrimEnd('.');
                //var contact = string.IsNullOrWhiteSpace(e.Contact) ? "" : e.Contact.Replace("mailto:", "");
                var linkCleared = Uri.EscapeUriString(e.Link);

                writer.WriteLine($"\"{eventType}\",\"{e.Title}\",,,\"{date}\",\"{e.Deadline}\",\"{linkCleared}\",\"{facebook}\",\"{instagram}\",\"{e.Contact}\",\"Serbia\"");
            }
        }

        Console.WriteLine($"✅ Extracted {events.Count} events → {csvPath}");
    }

    private static string GetEventType(string title)
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

    private static bool Has(this string s, string what)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        return s.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Parses event HTML and extracts:
    /// - "Крајњи рок за пријаву"
    /// - "Контакт"
    /// - "Више детаља" (as a link)
    /// Returns (Deadline, Contact, MoreDetailsLink)
    /// </summary>
    public static (string Deadline, string Contact, string MoreDetailsLink) ParseEventDetails(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        string GetFieldValue(string labelText)
        {
            var labelNode = doc.DocumentNode
                .SelectSingleNode($"//label[normalize-space(text())='{labelText}']");

            if (labelNode == null)
                return string.Empty;

            // Get the sibling <p> or <div> that holds the value
            //var valueNode = labelNode
            //    .ParentNode?
            //    .ParentNode?
            //    .SelectSingleNode(".//p|.//div");
            //var valueNode2 = labelNode
            //    .ParentNode?
            //    .ParentNode?
            //    .SelectSingleNode(".//div|.//div");

            //if (valueNode == null)
            //    return string.Empty;

            //// Prefer link text if present
            //var linkNode = valueNode.SelectSingleNode(".//a");
            //if (linkNode != null)
            //    return linkNode.GetAttributeValue("href", "").Trim();


            var parentNode = labelNode
                .ParentNode?
                .ParentNode;
            var link = parentNode.SelectSingleNode(".//a");

            if (link != null)
                return link.GetAttributeValue("href", "").Trim();

            var sibling = labelNode
                .ParentNode.NextSibling;

            //var valueNode = labelNode
            //    .ParentNode?
            //    .ParentNode?
            //    .SelectSingleNode(".//p|.//div");
            var valueNode = labelNode.ParentNode.NextSibling.NextSibling;

            return valueNode.InnerText.Trim();
        }

        string deadline = GetFieldValue("Крајњи рок за пријаву:");
        string contact = GetFieldValue("Контакт:");
        string moreDetails = GetFieldValue("Више детаља:");

        return (deadline, contact, moreDetails);
    }
}
