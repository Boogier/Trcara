namespace Trcara
{
    internal class PasrerProvider
    {
        public static IEnumerable<IParser> GetParsers() 
        {
            yield return new ItraParser();
            yield return new TrkaParser();
            yield return new RunTraceParser();
        }
    }
}
