using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.BlazorShared.Models.Json;

/// <summary>
/// Reads enums as their string name (the API's global JsonStringEnumConverter
/// serializes single-value enums as names, e.g. <c>"EntityChange"</c>) and also
/// accepts integer forms from legacy/flag payloads. React parity: the SPA mirrors
/// every server enum as a string union, so Blazor must deserialize the same wire
/// shape. Apply with <c>[JsonConverter(typeof(FlexibleEnumJsonConverter&lt;T&gt;))]</c>.
/// </summary>
public sealed class FlexibleEnumJsonConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var raw = reader.GetString();
                if (int.TryParse(raw, out var numeric))
                {
                    return (T)(object)numeric;
                }

                return Enum.TryParse<T>(raw, ignoreCase: true, out var parsed)
                    ? parsed
                    : default;
            case JsonTokenType.Number:
                return (T)(object)reader.GetInt32();
            default:
                throw new JsonException($"Cannot convert JSON token {reader.TokenType} to enum {typeof(T).Name}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
