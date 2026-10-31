namespace DataCodec;

/// <summary>Builds JSON-path style locations such as <c>$.items[0].name</c> for error messages.</summary>
internal static class DataPath
{
    public const string Root = "$";

    /// <summary>The path of a property: <c>$.name</c>, or <c>$['odd key']</c> if the key is not a plain identifier.</summary>
    public static string Key(string parent, string key)
    {
        bool isIdentifier = key.Length > 0
            && (char.IsLetter(key[0]) || key[0] == '_')
            && key.All(c => char.IsLetterOrDigit(c) || c == '_');

        return isIdentifier ? $"{parent}.{key}" : $"{parent}['{key.Replace("'", "\\'")}']";
    }

    public static string Index(string parent, int index) => $"{parent}[{index}]";
}
