namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderFileContentTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut(string template = "/test")
        => new(template, serializer);

    [Fact]
    public void WithBody_IFileContent_Single_ProducesMultipartContent()
    {
        // Arrange
        var sut = CreateSut();
        var fileContent = new TestFileContent("report.pdf", "application/pdf", [1, 2, 3]);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task WithBody_IFileContent_Single_ContainsFileBytes()
    {
        // Arrange
        var sut = CreateSut();
        byte[] data = [10, 20, 30];
        var fileContent = new TestFileContent("data.bin", null, data);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = message.Content.Should().BeOfType<MultipartFormDataContent>().Subject;
        var parts = multipart.ToList();
        parts.Should().HaveCount(1);
        var bytes = await parts[0].ReadAsByteArrayAsync();
        bytes.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void WithBody_IFileContent_Single_SetsAcceptToOctetStream()
    {
        // Arrange
        var sut = CreateSut();
        var fileContent = new TestFileContent("file.txt", "text/plain", [1]);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Headers.Accept.Should().Contain(h => h.MediaType == "application/octet-stream");
    }

    [Fact]
    public void WithBody_IFileContent_Single_SetsContentType()
    {
        // Arrange
        var sut = CreateSut();
        var fileContent = new TestFileContent("image.png", "image/png", [1, 2]);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = (MultipartFormDataContent)message.Content!;
        var part = multipart.First();
        part.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact]
    public void WithBody_IFileContentList_ProducesMultipartContent()
    {
        // Arrange
        var sut = CreateSut();
        var files = new List<IFileContent>
        {
            new TestFileContent("a.txt", "text/plain", [1]),
            new TestFileContent("b.txt", "text/plain", [2]),
        };

        // Act
        sut.WithBody(files);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = message.Content.Should().BeOfType<MultipartFormDataContent>().Subject;
        multipart.Should().HaveCount(2);
    }

    [Fact]
    public void DuckTyping_IFormFileShape_ProducesMultipartContent()
    {
        // Arrange — synthetic class matching IFormFile shape (FileName + parameterless OpenReadStream)
        var sut = CreateSut();
        var formFileLike = new FormFileLike("upload.csv", "text/csv", [7, 8, 9]);

        // Act
        sut.WithBody(formFileLike);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task DuckTyping_IFormFileShape_ContainsCorrectBytes()
    {
        // Arrange
        var sut = CreateSut();
        byte[] data = [7, 8, 9];
        var formFileLike = new FormFileLike("upload.csv", "text/csv", data);

        // Act
        sut.WithBody(formFileLike);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = (MultipartFormDataContent)message.Content!;
        var bytes = await multipart.First().ReadAsByteArrayAsync();
        bytes.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void DuckTyping_IBrowserFileShape_ProducesMultipartContent()
    {
        // Arrange — synthetic class matching IBrowserFile shape (Name + OpenReadStream(long, CancellationToken))
        var sut = CreateSut();
        var browserFileLike = new BrowserFileLike("photo.jpg", "image/jpeg", [4, 5, 6]);

        // Act
        sut.WithBody(browserFileLike);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task DuckTyping_IBrowserFileShape_ContainsCorrectBytes()
    {
        // Arrange
        var sut = CreateSut();
        byte[] data = [4, 5, 6];
        var browserFileLike = new BrowserFileLike("photo.jpg", "image/jpeg", data);

        // Act
        sut.WithBody(browserFileLike);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = (MultipartFormDataContent)message.Content!;
        var bytes = await multipart.First().ReadAsByteArrayAsync();
        bytes.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void DuckTyping_ListOfFormFileLike_ProducesMultipartContent()
    {
        // Arrange
        var sut = CreateSut();
        var files = new List<FormFileLike>
        {
            new("a.txt", "text/plain", [1]),
            new("b.txt", "text/plain", [2]),
        };

        // Act
        sut.WithBody(files);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = message.Content.Should().BeOfType<MultipartFormDataContent>().Subject;
        multipart.Should().HaveCount(2);
    }

    [Fact]
    public void PlainObject_StillJsonSerializes()
    {
        // Arrange
        var sut = CreateSut();
        var poco = new { Name = "test", Value = 42 };

        // Act
        sut.WithBody(poco);
        sut.Build(HttpMethod.Post);

        // Assert
        serializer.Received(1).Serialize(poco);
    }

    [Fact]
    public void DuckTyping_ObjectWithoutOpenReadStream_FallsBackToJson()
    {
        // Arrange — has FileName but no OpenReadStream
        var sut = CreateSut();
        var notAFile = new { FileName = "trick.txt" };

        // Act
        sut.WithBody(notAFile);
        sut.Build(HttpMethod.Post);

        // Assert
        serializer.Received(1).Serialize(notAFile);
    }

    private sealed class TestFileContent : IFileContent
    {
        private readonly byte[] data;

        public TestFileContent(
            string fileName,
            string? contentType,
            byte[] data)
        {
            FileName = fileName;
            ContentType = contentType;
            this.data = data;
        }

        public string FileName { get; }

        public string? ContentType { get; }

        public Stream OpenReadStream() => new MemoryStream(data);
    }

    /// <summary>
    /// Mimics the shape of IFormFile: FileName property + parameterless OpenReadStream().
    /// </summary>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Mimics IFormFile.OpenReadStream() method shape for duck-typing test")]
    internal sealed class FormFileLike
    {
        private readonly byte[] data;

        public FormFileLike(
            string fileName,
            string contentType,
            byte[] data)
        {
            FileName = fileName;
            ContentType = contentType;
            this.data = data;
        }

        public string FileName { get; }

        public string ContentType { get; }

        public Stream OpenReadStream() => new MemoryStream(data);
    }

    /// <summary>
    /// Mimics the shape of IBrowserFile: Name property + OpenReadStream(long, CancellationToken) with defaults.
    /// </summary>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Mimics IBrowserFile.OpenReadStream() method shape for duck-typing test")]
    internal sealed class BrowserFileLike
    {
        private readonly byte[] data;

        public BrowserFileLike(
            string name,
            string contentType,
            byte[] data)
        {
            Name = name;
            ContentType = contentType;
            this.data = data;
        }

        public string Name { get; }

        public string ContentType { get; }

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) => new MemoryStream(data);
    }
}