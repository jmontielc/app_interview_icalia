using System.Text;
using AppInterview.Pdf;
using Xunit;

namespace Api.Tests.Pdf;

public class PdfRulesTests
{
    [Fact]
    public void ValidPdfHeader_IsAccepted() =>
        Assert.Equal(PdfValidationResult.Valid, PdfRules.Validate(Encoding.ASCII.GetBytes("%PDF-1.7 body")));

    [Fact]
    public void EmptyContent_IsRejected() =>
        Assert.Equal(PdfValidationResult.Empty, PdfRules.Validate(ReadOnlySpan<byte>.Empty));

    [Fact]
    public void WrongMagicBytes_AreRejected() =>
        Assert.Equal(PdfValidationResult.NotPdf, PdfRules.Validate(Encoding.ASCII.GetBytes("MZ\x90\x00")));

    [Fact]
    public void ShorterThanSignature_IsRejected() =>
        Assert.Equal(PdfValidationResult.NotPdf, PdfRules.Validate(Encoding.ASCII.GetBytes("%PD")));

    [Fact]
    public void OverSizeLimit_IsRejected()
    {
        var content = new byte[PdfRules.MaxBytes + 1];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(content, 0);

        Assert.Equal(PdfValidationResult.TooLarge, PdfRules.Validate(content));
    }
}
