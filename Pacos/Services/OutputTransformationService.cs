using System.Text.RegularExpressions;
using Pacos.Constants;
using Pacos.Extensions;

namespace Pacos.Services;

public static class OutputTransformation
{
    private static readonly string[] ValidEndOfSentenceStrings = { ".", "!", "?", ";", ".)", "!)", "?)", ".\"", "!\"", "?\"" };

    // can't use #, /*, // because they sometimes occur in normal output too
    private static readonly string[] ProgrammingMathResponseMarkers = { "{", "}", "[", "]", "==", "Console.", "public static void", "public static", "public void", "public class", "<<", ">>", "&&", "|", "/>" };

    private static readonly Regex StartOfNewMessageRegex = new(@"(\n|^)((?!question|answer)\w{2,}):\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Transform(string botReply)
    {
        var generatedResult = botReply.Trim();

        // if the bot reply starts with a bot mention, remove it
        if (Const.Mentions
            .Select(m => m.Trim(',') + ':')
            .FirstOrDefault(m => generatedResult.StartsWith(m, StringComparison.InvariantCultureIgnoreCase))
            is { } mentionText
            && generatedResult.Length > mentionText.Length)
        {
            generatedResult = generatedResult[mentionText.Length..].Trim();
        }

        var startOfNewMessageMatch = StartOfNewMessageRegex.Match(generatedResult);

        if (startOfNewMessageMatch is { Success: true, Index: 0 })
        {
            // Reply is empty
            generatedResult = "🤔";
        }
        else if (startOfNewMessageMatch.Success)
        {
            // GPT thinks up the following dialogue, so we need to remove it
            generatedResult = generatedResult[..startOfNewMessageMatch.Index];
        }
        else
        {
            if (!ProgrammingMathResponseMarkers.Any(pm => generatedResult.Contains(pm)))
            {
                // it's not a code snippet, so we can clean the output using various rules
                generatedResult = generatedResult.Split("\n\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                generatedResult = generatedResult.Split("\r\n\r\n\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                generatedResult = generatedResult.Split("/*", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                generatedResult = generatedResult.Split("\n#", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                generatedResult = generatedResult.Replace("<br>", " ");

                if (!ValidEndOfSentenceStrings.Any(eos => generatedResult.EndsWith(eos)))
                {
                    // GPT couldn't complete the sentence, so we need to remove the incomplete sentence
                    var (eosIndex, eosValue) = generatedResult.LastIndexOfAny(ValidEndOfSentenceStrings);
                    if (eosIndex >= 0 && eosValue is not null)
                    {
                        generatedResult = generatedResult[..(eosIndex + eosValue.Length)];
                    }
                }
            }
        }

        generatedResult = generatedResult.Trim();

        return string.IsNullOrWhiteSpace(generatedResult) ? botReply : generatedResult;
    }
}
