using Microsoft.VisualBasic;

namespace Testura.Code.Tests;

public static class Utility
{
    public static string RemoveWhitespace(this string original)
    {
        return original
            .Replace(" ", string.Empty)
            .Replace("\t", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }
}
