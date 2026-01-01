namespace Trcara;

internal static class KnownRacesProvider
{
    private const string TrcaraColumnBExport = "https://docs.google.com/spreadsheets/d/1o3LivaIhBS0M1_bG9H8Pq_9K57AVFo0H40h0MzCOICs/gviz/tq?tqx=out:csv&tq=select%20B";
    // https://chatgpt.com/share/6956818c-713c-8005-ae06-2c265e11737a


    public static async Task<string[]> GetKnownRunsAsync()
    {
        Console.WriteLine($"Getting known races from {TrcaraColumnBExport}...");

        try
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(TrcaraColumnBExport);

            var knownRaces = html.Split('\n')
                .Skip(1) // Название
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().Replace("\"\"", "\""))
                .Select(s => s.Substring(1, s.Length - 2))
                .ToArray();

            Console.WriteLine($"\n{knownRaces.Length} races are known. Starting from '{knownRaces.FirstOrDefault()}' to '{knownRaces.LastOrDefault()}'\n");

            return knownRaces;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get known races: {ex.Message}");
            Console.WriteLine($"Empty list will be used.\n");
        }

        return [];
    }
}