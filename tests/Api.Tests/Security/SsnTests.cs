using System.Text.Json;
using Api.Security;
using Xunit;

namespace Api.Tests.Security;

public class SsnValidatorTests
{
    [Theory]
    [InlineData("123-45-6789")]
    [InlineData("123 45 6789")]
    [InlineData("123456789")]
    public void Validate_AcceptsWellFormedNumbers(string input) =>
        Assert.Equal(SsnValidationError.None, SsnValidator.Validate(input));

    [Theory]
    [InlineData(null, SsnValidationError.Empty)]
    [InlineData("   ", SsnValidationError.Empty)]
    [InlineData("12345678", SsnValidationError.InvalidFormat)]
    [InlineData("1234567890", SsnValidationError.InvalidFormat)]
    [InlineData("123-45-678A", SsnValidationError.InvalidFormat)]
    [InlineData("000-12-3456", SsnValidationError.InvalidArea)]
    [InlineData("666-12-3456", SsnValidationError.InvalidArea)]
    [InlineData("900-12-3456", SsnValidationError.InvalidArea)]
    [InlineData("987-65-4320", SsnValidationError.InvalidArea)]
    [InlineData("123-00-4567", SsnValidationError.InvalidGroup)]
    [InlineData("123-45-0000", SsnValidationError.InvalidSerial)]
    [InlineData("078-05-1120", SsnValidationError.KnownInvalid)]
    public void Validate_RejectsInvalidNumbers(string? input, SsnValidationError expected) =>
        Assert.Equal(expected, SsnValidator.Validate(input));
}

public class SsnTests
{
    [Fact]
    public void TryParse_ValidInput_ReturnsSsnWithLast4()
    {
        var ok = Ssn.TryParse("123-45-6789", out var ssn);

        Assert.True(ok);
        Assert.NotNull(ssn);
        Assert.Equal("6789", ssn!.Last4);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        var ok = Ssn.TryParse("000-12-3456", out var ssn);

        Assert.False(ok);
        Assert.Null(ssn);
    }

    [Fact]
    public void Equality_IgnoresFormatting()
    {
        Ssn.TryParse("123-45-6789", out var a);
        Ssn.TryParse("123456789", out var b);

        Assert.Equal(a, b);
        Assert.Equal(a!.GetHashCode(), b!.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsMaskedValue()
    {
        Ssn.TryParse("123-45-6789", out var ssn);

        Assert.Equal("***-**-6789", ssn!.ToString());
        Assert.DoesNotContain("123", ssn.ToString());
    }

    [Fact]
    public void JsonSerialization_WritesMaskedValue()
    {
        Ssn.TryParse("123-45-6789", out var ssn);
        var options = new JsonSerializerOptions { Converters = { new SsnJsonConverter() } };

        var json = JsonSerializer.Serialize(ssn, options);

        Assert.Equal("\"***-**-6789\"", json);
    }

    [Fact]
    public void JsonDeserialization_AcceptsFullValue()
    {
        var options = new JsonSerializerOptions { Converters = { new SsnJsonConverter() } };

        var ssn = JsonSerializer.Deserialize<Ssn>("\"123-45-6789\"", options);

        Assert.Equal("***-**-6789", ssn!.Masked);
    }

    [Fact]
    public void JsonDeserialization_RejectsInvalidValue()
    {
        var options = new JsonSerializerOptions { Converters = { new SsnJsonConverter() } };

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Ssn>("\"000-12-3456\"", options));
    }
}

public class SsnMaskerTests
{
    [Theory]
    [InlineData("123-45-6789", "***-**-6789")]
    [InlineData("123456789", "***-**-6789")]
    [InlineData("6789", "***-**-6789")]
    public void Mask_KeepsOnlyLast4(string input, string expected) =>
        Assert.Equal(expected, SsnMasker.Mask(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("abc")]
    public void Mask_ShortOrEmptyInput_ReturnsFullyMasked(string? input) =>
        Assert.Equal(SsnMasker.FullyMasked, SsnMasker.Mask(input));
}
