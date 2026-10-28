namespace PdfProcessor;

/// <summary>The two S3 operations the processor needs. Lets tests run without AWS.</summary>
public interface IObjectStore
{
    Task<byte[]> ReadAsync(string bucket, string key);

    Task TagAsync(string bucket, string key, IReadOnlyDictionary<string, string> tags);
}
