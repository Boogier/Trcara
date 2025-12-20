using Trcara.Parsers;

namespace Trcara;

internal class ParserProvider
{
    public static IEnumerable<IParser> GetParsers()
    {
        yield return new ItraParser();
        yield return new TrkaParser();
        yield return new RunTraceParser();
    }
}