namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class AnotherTestConverter : JsonConverter<int>
{
    public override int Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        => reader.GetInt32();

    public override void Write(
        Utf8JsonWriter writer,
        int value,
        JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}