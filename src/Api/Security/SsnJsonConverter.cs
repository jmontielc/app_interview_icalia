using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Security;

/// <summary>
/// Writes SSNs as masked text so full values never leave the API in responses.
/// Reading accepts a full SSN so request bodies can carry one.
/// </summary>
public sealed class SsnJsonConverter : JsonConverter<Ssn>
{
    public override Ssn? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (!Ssn.TryParse(text, out var ssn))
        {
            throw new JsonException("Invalid SSN.");
        }

        return ssn;
    }

    public override void Write(Utf8JsonWriter writer, Ssn value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Masked);
}
