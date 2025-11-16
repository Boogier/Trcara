namespace Trcara
{
    internal static class KnownRacesProvider
    {
        private const string TrcaraColumnBExport = @"https://docs.google.com/spreadsheets/d/1o3LivaIhBS0M1_bG9H8Pq_9K57AVFo0H40h0MzCOICs/gviz/tq?tqx=out:csv&tq=select%20B";
        
        public static async Task<string[]> GetKnownRunsAsync()
        {
            Console.WriteLine($"Getting known races from {TrcaraColumnBExport}...");

            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(TrcaraColumnBExport);

            var knownRaces = html.Split('\n').Select(s => s.Trim('"')).Skip(1).ToArray();

            Console.WriteLine($"\n{knownRaces.Length} races are known. Starting from '{knownRaces.FirstOrDefault()}' to '{knownRaces.LastOrDefault()}'\n");

            return knownRaces;
        }
    }
}
