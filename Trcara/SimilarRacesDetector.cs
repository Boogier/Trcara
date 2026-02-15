namespace Trcara;

internal static class SimilarRacesDetector
{
    private static readonly HashSet<string> NoiceWords = 
    [
        "kolo"
        , "vtl"
        , "втл"
        , "trail"
        , "ttls"
        , "ultra"
        , "maraton"
        , "polumaraton"
        , "marathon"
        , "halfmarathon"
        , "half"
        , "race"
        , "run"
        , "challenge"
        , "ocr"
        , "trka"
        , "skyrace"
        , "na"
        , "ultramaraton"
    ];

    public static List<KnownRace> FindSimilarRaces(EventDetails ev, KnownRace[] knownRaces)
    {
        var eventNameWords = ExtractWords(ev.Title);

        return knownRaces
            .Where(kr => ExtractWords(kr.Name).Any(word => eventNameWords.Contains(word)))
            .ToList();

        //var parsedDate = Utils.ParseDate(ev.Date);
        //return knownRaces1.Where(kr => Math.Abs(kr.Date.Subtract((DateTime)parsedDate).TotalDays) <= 3);
    }

    private static List<string> ExtractWords(string str)
    {
        return str
            .Split(' ', ',', '.', '-', '&')
            .Select(s => s.Trim(' ', '"', '\'').ToLower())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Where(s => !int.TryParse(s, out _))
            .Where(s => !NoiceWords.Contains(s))
            .ToList();
    }
}