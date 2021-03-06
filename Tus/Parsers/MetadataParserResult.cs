using tusdotnet.Models;

namespace tusdotnet.Parsers;

/// <summary>
///     Result of a call to <c>MetadataParser.ParseAndValidate</c>.
/// </summary>
public sealed class MetadataParserResult
{
    private MetadataParserResult(bool success, string errorMessage, Dictionary<string, Metadata> metadata)
    {
        Success = success;
        ErrorMessage = errorMessage;
        Metadata = metadata ?? new Dictionary<string, Metadata>();
    }

    /// <summary>
    ///     True if the parsing was successful, otherwise false.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     Error mesage if <see cref="Success" /> is false, otherwise null.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    ///     Metadata that was parsed. If <see cref="Success" /> is false then this will contain an empty dictionary.
    /// </summary>
    public Dictionary<string, Metadata> Metadata { get; }

    internal static MetadataParserResult FromError(string errorMessage)
    {
        return new MetadataParserResult(false, errorMessage, new Dictionary<string, Metadata>());
    }

    internal static MetadataParserResult FromResult(Dictionary<string, Metadata> metadata)
    {
        return new MetadataParserResult(true, null, metadata);
    }

    internal static MetadataParserResult FromResult(string key, Metadata metadata)
    {
        return FromResult(new Dictionary<string, Metadata>
        {
            {key, metadata}
        });
    }
}