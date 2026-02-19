#pragma warning disable IDE0230
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

    [Fact]
    public void DuckTyping_IBrowserFileShape_PassesLongMaxValueAsMaxAllowedSize()
    {
        // Arrange
        var sut = CreateSut();
        var browserFile = new CapturingBrowserFileLike("doc.pdf", "application/pdf", [1, 2, 3]);

        // Act
        sut.WithBody(browserFile);
        sut.Build(HttpMethod.Post);

        // Assert — ReflectedFileContent must pass long.MaxValue, not the 512000 default
        browserFile.CapturedMaxAllowedSize.Should().Be(long.MaxValue);
    }

    [Fact]
    public void DuckTyping_IBrowserFileShape_PassesCancellationTokenNone()
    {
        // Arrange
        var sut = CreateSut();
        var browserFile = new CapturingBrowserFileLike("doc.pdf", "application/pdf", [1, 2, 3]);

        // Act
        sut.WithBody(browserFile);
        sut.Build(HttpMethod.Post);

        // Assert
        browserFile.CapturedCancellationToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void WithBody_IFileContent_NonSeekableStream_ProducesMultipartContent()
    {
        // Arrange
        var sut = CreateSut();
        var fileContent = new NonSeekableFileContent("stream.bin", "application/octet-stream", [10, 20, 30]);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert — CopyTo must work even when the stream does not support Length/Position
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task WithBody_IFileContent_NonSeekableStream_ContainsCorrectBytes()
    {
        // Arrange
        var sut = CreateSut();
        byte[] data = [10, 20, 30];
        var fileContent = new NonSeekableFileContent("stream.bin", "application/octet-stream", data);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = (MultipartFormDataContent)message.Content!;
        var bytes = await multipart.First().ReadAsByteArrayAsync();
        bytes.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WithBody_IFileContent_EmptyStream_ProducesEmptyContent()
    {
        // Arrange
        var sut = CreateSut();
        var fileContent = new NonSeekableFileContent("empty.bin", null, []);

        // Act
        sut.WithBody(fileContent);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = (MultipartFormDataContent)message.Content!;
        var bytes = await multipart.First().ReadAsByteArrayAsync();
        bytes.Should().BeEmpty();
    }

    [Fact]
    public async Task DuckTyping_NonSeekableBrowserFileLike_ContainsCorrectBytes()
    {
        // Arrange — end-to-end: duck-typing + long.MaxValue + non-seekable stream
        var sut = CreateSut();
        byte[] data = [99, 100, 101, 102];
        var browserFile = new NonSeekableBrowserFileLike("blob.dat", "application/octet-stream", data);

        // Act
        sut.WithBody(browserFile);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = (MultipartFormDataContent)message.Content!;
        var bytes = await multipart.First().ReadAsByteArrayAsync();
        bytes.Should().BeEquivalentTo(data);
    }
}