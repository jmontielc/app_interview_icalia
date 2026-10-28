namespace AppInterview.Pdf;

public enum PdfValidationResult
{
    Valid,
    Empty,
    TooLarge,
    NotPdf
}

/// <summary>
/// Content rules for uploaded PDFs. Shared by the API (fail fast) and the PdfProcessor Lambda
/// (checks what actually landed in S3), so both enforce the same rules.
/// </summary>
public static class PdfRules
{
    public const int MaxBytes = 10 * 1024 * 1024;

    private static readonly byte[] Signature = "%PDF-"u8.ToArray();

    public static PdfValidationResult Validate(ReadOnlySpan<byte> content)
    {
        if (content.IsEmpty)
        {
            return PdfValidationResult.Empty;
        }

        if (content.Length > MaxBytes)
        {
            return PdfValidationResult.TooLarge;
        }

        // Checks the magic bytes, not the file name or the client's Content-Type.
        if (content.Length < Signature.Length || !content[..Signature.Length].SequenceEqual(Signature))
        {
            return PdfValidationResult.NotPdf;
        }

        return PdfValidationResult.Valid;
    }
}
