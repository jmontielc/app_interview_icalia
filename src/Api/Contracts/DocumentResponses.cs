namespace Api.Contracts;

public sealed record DocumentResponse(
    int Id,
    int ApplicantId,
    string FileName,
    long SizeBytes,
    string ProcessingStatus,
    DateTimeOffset UploadedAt);

public sealed record DownloadLinkResponse(string Url, double ExpiresInSeconds);
