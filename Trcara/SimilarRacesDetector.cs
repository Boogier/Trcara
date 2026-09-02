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
        , "ultramaraton"
        , "na"
        , "to"
        , "liga"
        , "i"
        , "de"
        , "za"
        , "трка"
    ];

    private static readonly char[] Separators = [' ', '-', '_', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'', '–'];

    public static List<KnownRace> FindSimilarRaces(EventDetails ev, KnownRace[] knownRaces)
    {
        var title = Utils.RemoveDiacritics(ev.Title);
        var eventNameWords = ExtractWords(title);

        // this is to track the words that are matched in the known races

        //foreach (var kr in knownRaces)
        //{
        //    foreach (var se in ExtractWords(Utils.RemoveDiacritics(kr.Name)).Where(word => eventNameWords.Contains(word)))
        //    {
        //        Console.WriteLine($"<{se}>");
        //    }
        //}

        return knownRaces
            .Where(kr => ExtractWords(Utils.RemoveDiacritics(kr.Name)).Any(word => eventNameWords.Contains(word)))
            .ToList();

        //var parsedDate = Utils.ParseDate(ev.Date);
        //return knownRaces1.Where(kr => Math.Abs(kr.Date.Subtract((DateTime)parsedDate).TotalDays) <= 3);
    }

    private static List<string> ExtractWords(string str)
    {
        return str
            .Split(Separators)
            .Select(s => s.ToLower())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Where(s => !int.TryParse(s, out _))
            .Where(s => !NoiceWords.Contains(s))
            .ToList();
    }
}