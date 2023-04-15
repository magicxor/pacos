namespace Pacos.Extensions;

public static class StringExtensions
{
    public static string Cut(this string src, int maxLength, string? defaultStr = null)
    {
        if (string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(defaultStr))
            return defaultStr;
        return src.Length <= maxLength ? src : src[..(maxLength - 1)];
    }

    public static (int lastIndex, string? foundValue) LastIndexOfAny(this string source, string[] values)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(values);

        var lastIndex = -1;
        string? foundValue = null;

        foreach (var value in values)
        {
            var currentIndex = source.LastIndexOf(value, StringComparison.InvariantCultureIgnoreCase);

            if (currentIndex > lastIndex)
            {
                lastIndex = currentIndex;
                foundValue = value;
            }
        }

        return (lastIndex, foundValue);
    }
}
