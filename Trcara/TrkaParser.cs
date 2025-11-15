using HtmlAgilityPack;

namespace Trcara
{
    internal static class TrkaParser
    {
        public static async Task<List<EventDetails>> Get(string[] knownRuns)
        {
            string baseUrl = "https://www.trka.rs";

            var httpClient = new HttpClient();
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

            var events = new List<EventDetails>();

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

                var facebook = details.MoreDetailsLink.Has("facebook") ? details.MoreDetailsLink : "";
                var instagram = details.MoreDetailsLink.Has("instagram") ? details.MoreDetailsLink : "";

                var link = !string.IsNullOrWhiteSpace(details.MoreDetailsLink) && string.IsNullOrWhiteSpace(facebook) && string.IsNullOrWhiteSpace(instagram)
                   ? details.MoreDetailsLink
                   : trkaLink;

                events.Add(new EventDetails(RaceType.Trail, title, "", "", date, link, facebook, instagram, details.Deadline, details.Contact, "Serbia", ""));
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
}
