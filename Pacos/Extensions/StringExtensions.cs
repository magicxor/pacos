namespace Pacos.Extensions;

public static class StringExtensions
{
    public static string Cut(this string src, int maxLength)
    {
        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), $"{nameof(maxLength)} must be greater than 0");
        }

        return src.Length <= maxLength
            ? src
            : src[..maxLength];
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
