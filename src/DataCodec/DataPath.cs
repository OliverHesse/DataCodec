namespace DataCodec;

/// <summary>Builds JSON-path style locations such as <c>$.items[0].name</c> for error messages.</summary>
public static class DataPath
{
    /// <summary>The path of the document root.</summary>
    public const string Root = "$";

    /// <summary>The path of a property: <c>$.name</c>, or <c>$['odd key']</c> if the key is not a plain identifier.</summary>
    public static string Key(string parent, string key)
    {
        bool isIdentifier = key.Length > 0
            && (char.IsLetter(key[0]) || key[0] == '_')
            && key.All(c => char.IsLetterOrDigit(c) || c == '_');

        return isIdentifier ? $"{parent}.{key}" : $"{parent}['{key.Replace("'", "\\'")}']";
    }

    /// <summary>The path of an array item: <c>$.items[2]</c>.</summary>
    public static string Index(string parent, int index) => $"{parent}[{index}]";

    /// <summary>
    /// Moves <paramref name="path"/>, which starts at its own <c>$</c>, under <paramref name="parentPath"/>:
    /// <c>Rebase("$.speed", "$.stats")</c> gives <c>$.stats.speed</c>.
    /// </summary>
    public static string Rebase(string path, string parentPath) => parentPath + path[Root.Length..];
}
