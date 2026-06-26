using System;
using System.Text;

namespace AdvancedTimeIsland.Helpers;

public static class SensitiveWordHelper
{
    private static readonly string[] EncodedSensitiveWords = 
    {
        "5Zug5aW9" 
    };

    private static string DecodeWord(string encoded)
    {
        var bytes = Convert.FromBase64String(encoded);
        return Encoding.UTF8.GetString(bytes);
    }

    public static bool ContainsSensitiveContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var encodedWord in EncodedSensitiveWords)
        {
            var decodedWord = DecodeWord(encodedWord);
            if (text.Contains(decodedWord))
            {
                return true;
            }
        }
        return false;
    }
}