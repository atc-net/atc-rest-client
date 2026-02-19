namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class CustomTestConverter : JsonConverter<string>
{
    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        => reader.GetString();

    public override void Write(
        Utf8JsonWriter writer,
        string value,
        JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}