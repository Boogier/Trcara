namespace Trcara;

internal static class Log
{
    public const ConsoleColor WarningForegroundColor = ConsoleColor.Yellow;
    public const ConsoleColor DefaultForegroundColor = ConsoleColor.White;
    public const ConsoleColor ErrorBackgroundColor = ConsoleColor.DarkRed;

    public static void Error(string text)
    {
        ColorLine(text, ErrorBackgroundColor);
    }

    public static void Warning(string text)
    {
        ColorLine(text, foregroundColor: WarningForegroundColor);
    }

    public static void Info(string text)
    {
        Console.WriteLine(text);
    }

    public static void ColorLine(string text, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor foregroundColor = DefaultForegroundColor)
    {
        Console.BackgroundColor = backgroundColor;
        Console.ForegroundColor = foregroundColor;

        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
    }
}