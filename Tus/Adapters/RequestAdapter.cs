using tusdotnet.Constants;

namespace tusdotnet.Adapters;

/// <summary>
///     Request adapter that handles different pipeline requests.
/// </summary>
internal sealed class RequestAdapter
{
    private readonly string _configUrlPath;

    private readonly Lazy<string> _fileId;
    private readonly Lazy<long> _uploadLength;

    public RequestAdapter(string configUrlPath)
    {
        _fileId = new Lazy<string>(ParseFileId);
        _uploadLength = new Lazy<long>(ParseUploadLength);
        _configUrlPath = configUrlPath;
    }

    public string Method { get; set; }

    public Uri RequestUri { get; set; }

    public Stream Body { get; set; }

    public Dictionary<string, List<string>> Headers { get; set; }

    public string ContentType => GetHeader("Content-Type");

    public string FileId => _fileId.Value;

    public long UploadLength => _uploadLength.Value;

    public string GetHeader(string name)
    {
        return Headers?.ContainsKey(name) == true ? Headers[name][0] : null;
    }

    private string ParseFileId()
    {
        var startIndex = RequestUri.LocalPath.IndexOf(_configUrlPath, StringComparison.OrdinalIgnoreCase) +
                         _configUrlPath.Length;

        return RequestUri.LocalPath.Substring(startIndex).Trim('/');
    }

    private long ParseUploadLength()
    {
        return Headers.ContainsKey(HeaderConstants.UploadDeferLength)
            ? -1
            : long.Parse(GetHeader(HeaderConstants.UploadLength) ?? "-1");
    }
}