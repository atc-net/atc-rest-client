using System.Text.Json.Serialization;

namespace Atc.Rest.Client.Tests.Serialization;

public sealed class JsonSerializerOptionsExtensionsTests
{
    [Fact]
    public void WithoutConverter_WithNullSource_ThrowsArgumentNullException()
    {
        JsonSerializerOptions? source = null;

        var act = () => source!.WithoutConverter();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Fact]
    public void WithoutConverter_CopiesBasicProperties()
    {
        var source = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            DefaultBufferSize = 1024,
            MaxDepth = 10,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        var result = source.WithoutConverter();

        result.AllowTrailingCommas.Should().Be(source.AllowTrailingCommas);
        result.DefaultBufferSize.Should().Be(source.DefaultBufferSize);
        result.MaxDepth.Should().Be(source.MaxDepth);
        result.PropertyNameCaseInsensitive.Should().Be(source.PropertyNameCaseInsensitive);
        result.WriteIndented.Should().Be(source.WriteIndented);
        result.ReadCommentHandling.Should().Be(source.ReadCommentHandling);
    }

    [Fact]
    public void WithoutConverter_CopiesPropertyNamingPolicy()
    {
        var source = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var result = source.WithoutConverter();

        result.PropertyNamingPolicy.Should().BeSameAs(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void WithoutConverter_CopiesDictionaryKeyPolicy()
    {
        var source = new JsonSerializerOptions
        {
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        };

        var result = source.WithoutConverter();

        result.DictionaryKeyPolicy.Should().BeSameAs(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void WithoutConverter_SetsDefaultIgnoreConditionToWhenWritingNull()
    {
        var source = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };

        var result = source.WithoutConverter();

        result.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void WithoutConverter_CopiesAllConvertersWhenNoConvertersSpecified()
    {
        var source = new JsonSerializerOptions();
        source.Converters.Add(new JsonStringEnumConverter());

        var result = source.WithoutConverter();

        result.Converters.Should().HaveCount(1);
        result.Converters[0].Should().BeOfType<JsonStringEnumConverter>();
    }

    [Fact]
    public void WithoutConverter_ExcludesSpecifiedConverters()
    {
        var converterToRemove = new JsonStringEnumConverter();
        var source = new JsonSerializerOptions();
        source.Converters.Add(converterToRemove);

        var result = source.WithoutConverter(converterToRemove);

        result.Converters.Should().BeEmpty();
    }

    [Fact]
    public void WithoutConverter_ExcludesMultipleSpecifiedConverters()
    {
        var converter1 = new JsonStringEnumConverter();
        var converter2 = new CustomTestConverter();
        var converter3 = new AnotherTestConverter();

        var source = new JsonSerializerOptions();
        source.Converters.Add(converter1);
        source.Converters.Add(converter2);
        source.Converters.Add(converter3);

        var result = source.WithoutConverter(converter1, converter3);

        result.Converters.Should().HaveCount(1);
        result.Converters[0].Should().BeSameAs(converter2);
    }

    [Fact]
    public void WithoutConverter_ReturnsNewInstance()
    {
        var source = new JsonSerializerOptions();

        var result = source.WithoutConverter();

        result.Should().NotBeSameAs(source);
    }

    [Fact]
    public void WithoutConverter_SourceRemainsUnchanged()
    {
        var converter = new JsonStringEnumConverter();
        var source = new JsonSerializerOptions();
        source.Converters.Add(converter);

        _ = source.WithoutConverter(converter);

        source.Converters.Should().HaveCount(1);
        source.Converters[0].Should().BeSameAs(converter);
    }

    private sealed class CustomTestConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetString();

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }

    private sealed class AnotherTestConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetInt32();

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }
}