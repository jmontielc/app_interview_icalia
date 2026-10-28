namespace Api.Domain;

/// <summary>Metadata for an uploaded PDF. The file itself lives in S3. Processing status is read from object tags.</summary>
public sealed class DocumentRecord
{
    private DocumentRecord()
    {
    }

    public DocumentRecord(int applicantId, string objectKey, string fileName, long sizeBytes, string uploadedBy, DateTimeOffset uploadedAt)
    {
        ApplicantId = applicantId;
        ObjectKey = objectKey;
        FileName = fileName;
        SizeBytes = sizeBytes;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
    }

    public int Id { get; private set; }

    public int ApplicantId { get; private set; }

    /// <summary>Server-generated S3 key. Never derived from the client's file name.</summary>
    public string ObjectKey { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string UploadedBy { get; private set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; private set; }
}
