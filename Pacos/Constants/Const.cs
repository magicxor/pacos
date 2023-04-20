namespace Pacos.Constants;

public static class Const
{
    public const int MaxTelegramMessageLength = 4096;
    public static readonly string[] ProgrammingMathPromptMarkers = { "{", "}", "[", "]", "==", "Console.", "static void", "public static", "public void", "private static", "private void", "public class", " int ", " const ", " var ", "<<", ">>", "&&", "|", "C#", "F#", "C++", "javascript", " js", "typescript", "yml", "yaml", "json", "xml", "html", " программу ", " код ", "code snippet" };
    public static readonly string[] Mentions = { "пакос,", "pacos," };
    public const string AutoCompletionMarker = "!complete";
    public const string InstructionMarker = "!";
}
