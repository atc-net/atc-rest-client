namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderMultipartTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut(string template = "/test") => new(template, serializer);

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