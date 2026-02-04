namespace Atc.Rest.Client.Tests.Serialization;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test helper types")]
public sealed class DefaultJsonContractSerializerTests
{
    private readonly DefaultJsonContractSerializer sut = new();

    [Fact]
    public void Should_Use_Default_Options_When_None_Provided()
    {
        var serializer = new DefaultJsonContractSerializer();
        var model = new TestModel("Test", 42);

        var json = serializer.Serialize(model);

        json.Should().Contain("\"name\":");
        json.Should().Contain("\"value\":");
    }

    [Fact]
    public void Should_Use_Custom_Options_When_Provided()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
        };
        var serializer = new DefaultJsonContractSerializer(options);
        var model = new TestModel("Test", 42);

        var json = serializer.Serialize(model);

        json.Should().Contain("\"Name\":");
        json.Should().Contain("\"Value\":");
    }

    [Fact]
    public void Serialize_Should_Convert_Object_To_Json()
    {
        var model = new TestModel("Test", 42);

        var json = sut.Serialize(model);

        json.Should().Contain("\"name\":");
        json.Should().Contain("\"Test\"");
        json.Should().Contain("\"value\":");
        json.Should().Contain("42");
    }

    [Fact]
    public void Serialize_Should_Use_CamelCase()
    {
        var model = new TestModel("Test", 42);

        var json = sut.Serialize(model);

        json.Should().Contain("\"name\"");
        json.Should().NotContain("\"Name\"");
    }

    [Fact]
    public void Serialize_Should_Ignore_Null_Values()
    {
        var model = new { Name = "Test", NullValue = (string?)null };

        var json = sut.Serialize(model);

        json.Should().NotContain("nullValue");
    }

    [Fact]
    public void Serialize_Should_Convert_Enum_To_String()
    {
        var model = new { Status = TestStatus.Active };

        var json = sut.Serialize(model);

        json.Should().Contain("\"Active\"");
    }

    [Fact]
    public void Deserialize_String_Should_Convert_Json_To_Object()
    {
        const string json = """{"name":"Test","value":42}""";

        var result = sut.Deserialize<TestModel>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_String_Should_Handle_CamelCase()
    {
        const string json = """{"name":"Test","value":42}""";

        var result = sut.Deserialize<TestModel>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public void Deserialize_String_Should_Return_Null_For_Null_Json()
    {
        const string json = "null";

        var result = sut.Deserialize<TestModel>(json);

        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_Bytes_Should_Convert_Utf8Json_To_Object()
    {
        var json = """{"name":"Test","value":42}"""u8.ToArray();

        var result = sut.Deserialize<TestModel>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_Bytes_Should_Return_Null_For_Null_Json()
    {
        var json = "null"u8.ToArray();

        var result = sut.Deserialize<TestModel>(json);

        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithType_Should_Convert_Json_To_Object()
    {
        const string json = """{"name":"Test","value":42}""";

        var result = sut.Deserialize(json, typeof(TestModel));

        result.Should().NotBeNull();
        result.Should().BeOfType<TestModel>();
        var model = (TestModel)result!;
        model.Name.Should().Be("Test");
        model.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_WithType_Bytes_Should_Convert_Utf8Json_To_Object()
    {
        var json = """{"name":"Test","value":42}"""u8.ToArray();

        var result = sut.Deserialize(json, typeof(TestModel));

        result.Should().NotBeNull();
        result.Should().BeOfType<TestModel>();
        var model = (TestModel)result!;
        model.Name.Should().Be("Test");
        model.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_Should_Handle_Enum_From_String()
    {
        const string json = """{"status":"Active"}""";

        var result = sut.Deserialize<StatusContainer>(json);

        result.Should().NotBeNull();
        result!.Status.Should().Be(TestStatus.Active);
    }

    [Fact]
    public async Task DeserializeAsyncEnumerable_Should_Stream_Items()
    {
        var json = """[{"name":"First","value":1},{"name":"Second","value":2}]""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var items = new List<TestModel?>();
        await foreach (var item in sut.DeserializeAsyncEnumerable<TestModel>(stream))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        items[0]!.Name.Should().Be("First");
        items[0]!.Value.Should().Be(1);
        items[1]!.Name.Should().Be("Second");
        items[1]!.Value.Should().Be(2);
    }

    [Fact]
    public async Task DeserializeAsyncEnumerable_Should_Handle_Empty_Array()
    {
        const string json = "[]";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var items = new List<TestModel?>();
        await foreach (var item in sut.DeserializeAsyncEnumerable<TestModel>(stream))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeserializeAsyncEnumerable_Should_Support_Cancellation()
    {
        var json = """[{"name":"First","value":1},{"name":"Second","value":2}]""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = async () =>
        {
            await foreach (var item in sut.DeserializeAsyncEnumerable<TestModel>(stream, cts.Token))
            {
                _ = item;
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Serialize_Should_Throw_For_Circular_Reference()
    {
        var circular = new CircularModel();
        circular.Self = circular;

        Action act = () => sut.Serialize(circular);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_Should_Throw_For_Invalid_Json()
    {
        const string invalidJson = "{invalid}";

        Action act = () => sut.Deserialize<TestModel>(invalidJson);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public async Task DeserializeAsyncEnumerable_WithPrimitiveTypes_Works()
    {
        const string json = "[1, 2, 3, 4, 5]";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var items = new List<int>();
        await foreach (var item in sut.DeserializeAsyncEnumerable<int>(stream))
        {
            items.Add(item);
        }

        items.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task DeserializeAsyncEnumerable_WithStringTypes_Works()
    {
        const string json = """["hello", "world", "test"]""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var items = new List<string?>();
        await foreach (var item in sut.DeserializeAsyncEnumerable<string>(stream))
        {
            items.Add(item);
        }

        items.Should().Equal("hello", "world", "test");
    }

    [Fact]
    public async Task DeserializeAsyncEnumerable_WithNullableTypes_HandlesNulls()
    {
        const string json = "[1, null, 3, null, 5]";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var items = new List<int?>();
        await foreach (var item in sut.DeserializeAsyncEnumerable<int?>(stream))
        {
            items.Add(item);
        }

        items.Should().BeEquivalentTo(new int?[] { 1, null, 3, null, 5 });
    }

    [Fact]
    public void Serialize_WithDateTimeOffset_UsesIso8601Format()
    {
        var dateTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var model = new DateTimeModel(dateTime);

        var json = sut.Serialize(model);

        json.Should().Contain("2024-06-15T10:30:00");
    }

    [Fact]
    public void Deserialize_WithNestedObjects_Works()
    {
        const string json = """{"parent":{"name":"Parent","value":1},"child":{"name":"Child","value":2}}""";

        var result = sut.Deserialize<NestedModel>(json);

        result.Should().NotBeNull();
        result!.Parent.Should().NotBeNull();
        result.Parent!.Name.Should().Be("Parent");
        result.Parent.Value.Should().Be(1);
        result.Child.Should().NotBeNull();
        result.Child!.Name.Should().Be("Child");
        result.Child.Value.Should().Be(2);
    }

    [Fact]
    public void Deserialize_WithMissingProperties_SetsDefaults()
    {
        const string json = """{"name":"Test"}""";

        var result = sut.Deserialize<TestModel>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Serialize_WithEmptyObject_ReturnsEmptyJson()
    {
        var model = new EmptyModel();

        var json = sut.Serialize(model);

        json.Should().Be("{}");
    }

    [Fact]
    public void Deserialize_WithExtraProperties_IgnoresThem()
    {
        const string json = """{"name":"Test","value":42,"extra":"ignored"}""";

        var result = sut.Deserialize<TestModel>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Serialize_WithArray_Works()
    {
        var models = new[] { new TestModel("First", 1), new TestModel("Second", 2) };

        var json = sut.Serialize(models);

        // JSON may be pretty-printed with spaces
        json.Should().Contain("\"name\":");
        json.Should().Contain("\"First\"");
        json.Should().Contain("\"Second\"");
    }

    [Fact]
    public void Deserialize_WithArray_Works()
    {
        const string json = """[{"name":"First","value":1},{"name":"Second","value":2}]""";

        var result = sut.Deserialize<TestModel[]>(json);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("First");
        result[1].Name.Should().Be("Second");
    }

    [Fact]
    public void Serialize_WithDictionary_Works()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["one"] = 1,
            ["two"] = 2,
        };

        var json = sut.Serialize(dict);

        // JSON may be pretty-printed with spaces
        json.Should().Contain("\"one\":");
        json.Should().Contain("1");
        json.Should().Contain("\"two\":");
        json.Should().Contain("2");
    }

    [Fact]
    public void Deserialize_WithDictionary_Works()
    {
        const string json = """{"one":1,"two":2}""";

        var result = sut.Deserialize<Dictionary<string, int>>(json);

        result.Should().NotBeNull();
        result.Should().ContainKey("one").WhoseValue.Should().Be(1);
        result.Should().ContainKey("two").WhoseValue.Should().Be(2);
    }

    [Fact]
    public void Serialize_AndDeserialize_RoundTrips_UnicodeCharacters()
    {
        // Arrange
        var original = new TestModel("日本語テスト", 42);

        // Act - serialize and deserialize
        var json = sut.Serialize(original);
        var deserialized = sut.Deserialize<TestModel>(json);

        // Assert - round-trip preserves the value
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("日本語テスト");
        deserialized.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_PreservesUnicodeCharacters()
    {
        const string json = """{"name":"日本語テスト","value":42}""";

        var result = sut.Deserialize<TestModel>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("日本語テスト");
    }

    public sealed record TestModel(string Name, int Value);

    public sealed record StatusContainer(TestStatus Status);

    public enum TestStatus
    {
        Inactive,
        Active,
        Pending,
    }

    public sealed class CircularModel
    {
        public CircularModel? Self { get; set; }
    }

    public sealed record DateTimeModel(DateTimeOffset Timestamp);

    public sealed record NestedModel(TestModel? Parent, TestModel? Child);

    [SuppressMessage("Major Code Smell", "S2094:Classes should not be empty", Justification = "Test helper type")]
    public sealed class EmptyModel
    {
    }
}