namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderMultipartTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut(string template = "/test")
        => new(template, serializer);

    private static MessageRequestBuilder CreateJsonSut()
    {
        var options = DefaultJsonContractSerializer.CreateDefaultOptions();
        options.WriteIndented = false;
        return new MessageRequestBuilder("/test", new DefaultJsonContractSerializer(options));
    }

    private static async Task<List<(string? Name, string? ContentType, string Body)>> ReadPartsAsync(
        HttpRequestMessage message)
    {
        var multipart = message.Content.Should().BeOfType<MultipartFormDataContent>().Subject;
        var parts = new List<(string? Name, string? ContentType, string Body)>();
        foreach (var part in multipart)
        {
            var body = await part.ReadAsStringAsync(TestContext.Current.CancellationToken);
            parts.Add((part.Headers.ContentDisposition?.Name?.Trim('"'), part.Headers.ContentType?.ToString(), body));
        }

        return parts;
    }

    [Fact]
    public void WithFile_AddsFileToRequest()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var result = sut.WithFile(stream, "file", "test.txt", "text/plain");

        // Assert
        result.Should().BeSameAs(sut);
        var message = sut.Build(HttpMethod.Post);
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public void WithFile_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFile(null!, "file", "test.txt");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public void WithFile_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act
        var act = () => sut.WithFile(stream, null!, "test.txt");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void WithFile_WithNullFileName_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act
        var act = () => sut.WithFile(stream, "file", null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fileName");
    }

    [Fact]
    public void WithFiles_AddsMultipleFilesToRequest()
    {
        // Arrange
        var sut = CreateSut();
        using var stream1 = new MemoryStream([1, 2, 3]);
        using var stream2 = new MemoryStream([4, 5, 6]);
        var files = new List<(Stream, string, string, string?)>
        {
            (stream1, "file1", "test1.txt", "text/plain"),
            (stream2, "file2", "test2.txt", null),
        };

        // Act
        var result = sut.WithFiles(files);

        // Assert
        result.Should().BeSameAs(sut);
        var message = sut.Build(HttpMethod.Post);
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public void WithFiles_WithNullFiles_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFiles(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("files");
    }

    [Fact]
    public void WithFormField_AddsFormFieldToRequest()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.WithFormField("key", "value");

        // Assert
        result.Should().BeSameAs(sut);
        var message = sut.Build(HttpMethod.Post);
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public void WithFormField_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormField(null!, "value");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void WithFormField_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormField("key", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public async Task WithFormField_WithContentType_SetsThePartContentType()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.WithFormField("address", "{\"street\":\"Main Street 1\"}", "application/json");

        // Assert
        result.Should().BeSameAs(sut);
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(("address", "application/json; charset=utf-8", "{\"street\":\"Main Street 1\"}"));
    }

    [Fact]
    public async Task WithFormField_WithNonJsonContentTypeWithoutCharset_AddsUtf8Charset()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithFormField("rows", "a;b", "text/csv");

        // Assert
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(("rows", "text/csv; charset=utf-8", "a;b"));
    }

    [Fact]
    public async Task WithFormField_WithContentTypeAndCharset_KeepsTheContentTypeAsGiven()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithFormField("address", "{}", "application/json; charset=UTF-8; profile=v1");

        // Assert
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(("address", "application/json; charset=UTF-8; profile=v1", "{}"));
    }

    [Fact]
    public async Task WithFormField_WithoutContentType_IsStillTextPlain()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithFormField("description", "My document");

        // Assert
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(("description", "text/plain; charset=utf-8", "My document"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithFormField_WithContentType_WithNullOrWhiteSpaceName_ThrowsArgumentException(
        string? name)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormField(name!, "value", "application/json");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(name));
    }

    [Fact]
    public void WithFormField_WithContentType_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormField("key", null!, "application/json");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithFormField_WithNullOrWhiteSpaceContentType_ThrowsArgumentException(
        string? contentType)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormField("key", "value", contentType!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(contentType));
    }

    [Fact]
    public void WithFormField_WithInvalidContentType_Throws()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormField("key", "value", "not a media type");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public async Task WithFormJsonField_SerializesWithTheContractSerializer()
    {
        // Arrange
        var sut = CreateJsonSut();

        // Act
        var result = sut.WithFormJsonField("task", new TaskItem(Priority.High, OptionalPriority: null, TestStatus.Active));

        // Assert
        result.Should().BeSameAs(sut);
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(("task", "application/json; charset=utf-8", "{\"priority\":3,\"status\":\"Active\"}"));
    }

    [Fact]
    public void WithFormJsonField_WithFileAndTextFields_KeepsCallOrderAmongFields()
    {
        // Arrange
        var sut = CreateJsonSut();
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        sut.WithFile(stream, "file", "invoice.pdf", "application/pdf");
        sut.WithFormField("first", "1");
        sut.WithFormJsonField("model", new TestModel("Test", 42));
        sut.WithFormField("last", "2");

        // Assert
        var multipart = sut.Build(HttpMethod.Post).Content.Should().BeOfType<MultipartFormDataContent>().Subject;
        multipart
            .Select(part => (part.Headers.ContentDisposition?.Name?.Trim('"'), part.Headers.ContentType?.MediaType))
            .Should()
            .Equal(
                ("first", "text/plain"),
                ("model", "application/json"),
                ("last", "text/plain"),
                ("file", "application/pdf"));
    }

    [Fact]
    public async Task WithFormJsonField_RepeatedName_SendsOnePartPerCall()
    {
        // Arrange
        var sut = CreateJsonSut();

        // Act
        sut.WithFormJsonField("items", new TestModel("A", 1));
        sut.WithFormJsonField("items", new TestModel("B", 2));

        // Assert
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(
            ("items", "application/json; charset=utf-8", "{\"name\":\"A\",\"value\":1}"),
            ("items", "application/json; charset=utf-8", "{\"name\":\"B\",\"value\":2}"));
    }

    [Fact]
    public async Task WithFormJsonField_NullValue_SendsJsonNull()
    {
        // Arrange
        var sut = CreateJsonSut();

        // Act
        sut.WithFormJsonField<TestModel?>("model", null);

        // Assert
        var parts = await ReadPartsAsync(sut.Build(HttpMethod.Post));
        parts.Should().Equal(("model", "application/json; charset=utf-8", "null"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithFormJsonField_WithNullOrWhiteSpaceName_ThrowsArgumentException(
        string? name)
    {
        // Arrange
        var sut = CreateJsonSut();

        // Act
        var act = () => sut.WithFormJsonField(name!, new TestModel("Test", 42));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(name));
    }

    [Fact]
    public void WithFormJsonField_WithWhiteSpaceName_ThrowsBeforeSerializing()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.WithFormJsonField(" ", new TestModel("Test", 42));

        // Assert
        act.Should().Throw<ArgumentException>();
        serializer.DidNotReceive().Serialize(Arg.Any<object>());
    }

    [Fact]
    public void Build_WithJsonFieldAndUrlEncodedField_Throws()
    {
        // Arrange
        var sut = CreateJsonSut();
        sut.WithFormJsonField("model", new TestModel("Test", 42));
        sut.WithUrlEncodedFormField("a", "1");

        // Act
        var act = () => sut.Build(HttpMethod.Post);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_WithJsonFieldAndBody_SendsTheBody()
    {
        // Arrange
        var sut = CreateJsonSut();
        sut.WithFormJsonField("model", new TestModel("Test", 42));
        sut.WithBody(new TestModel("Body", 1));

        // Act
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<StringContent>();
    }

    [Fact]
    public void WithHttpCompletionOption_SetsOption()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead);

        // Assert
        result.Should().BeSameAs(sut);
        sut.HttpCompletionOption.Should().Be(HttpCompletionOption.ResponseHeadersRead);
    }

    [Fact]
    public void HttpCompletionOption_DefaultsToResponseContentRead()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.HttpCompletionOption.Should().Be(HttpCompletionOption.ResponseContentRead);
    }
}