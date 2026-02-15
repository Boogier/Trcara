namespace Trcara;

internal static class KnownRacesProvider
{
    private const string TrcaraColumnBExport = "https://docs.google.com/spreadsheets/d/1o3LivaIhBS0M1_bG9H8Pq_9K57AVFo0H40h0MzCOICs/gviz/tq?tqx=out:csv&tq=select%20B,E";
    // https://chatgpt.com/share/6956818c-713c-8005-ae06-2c265e11737a
    
    public static async Task<KnownRace[]> GetKnownRacesAsync()
    {
        Console.WriteLine($"Getting known races from {TrcaraColumnBExport}...");

        try
        {
            var httpClient = new HttpClient();
            var csv = await httpClient.GetStringAsync(TrcaraColumnBExport);

            var knownRaces = csv.Split('\n')
                .Skip(1) // Table header
                .Select(line => line.Split(","))
                .Select(arr => new KnownRace(CleanupName(arr[0]), Utils.ParseDate(arr[1])))
                .Where(race => !string.IsNullOrWhiteSpace(race.Name) && race.Date != Utils.EmptyDate)
                .ToArray();

            Console.WriteLine($"\n{knownRaces.Length} races are known. Starting from '{knownRaces.FirstOrDefault()}' to '{knownRaces.LastOrDefault()}'\n");

            return knownRaces;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get known races: {ex.Message}");
            Console.WriteLine("Empty list will be used.\n");
        }

        return [];
    }

    private static string CleanupName(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return "";
        }

        s = s.Trim(' ', '"').Replace("\"\"", "\"");

        return s;
    }
}