namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderBinaryTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
        => new(response, serializer);

    [Fact]
    public async Task BuildBinaryResponseAsync_NullResponse_ReturnsInternalServerError()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act
        var result = await sut.BuildBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Content.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();
        result.ContentLength.Should().BeNull();
    }

    [Fact]
    public async Task BuildBinaryResponseAsync_SuccessResponse_ReturnsContent()
    {
        // Arrange
        var expectedContent = new byte[] { 1, 2, 3, 4, 5 };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedContent),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Should().BeEquivalentTo(expectedContent);
        result.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task BuildBinaryResponseAsync_WithContentDisposition_ReturnsFileName()
    {
        // Arrange
        var expectedContent = new byte[] { 1, 2, 3 };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedContent),
        };
        response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        {
            FileName = "\"document.pdf\"",
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.FileName.Should().Be("document.pdf");
    }

    [Fact]
    public async Task BuildBinaryResponseAsync_WithContentLength_ReturnsContentLength()
    {
        // Arrange
        var expectedContent = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedContent),
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.ContentLength.Should().Be(10);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Created, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task BuildBinaryResponseAsync_RespectsHttpStatusCode(HttpStatusCode statusCode, bool expectedSuccess)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode)
        {
            Content = new ByteArrayContent([1, 2, 3]),
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().Be(expectedSuccess);
        result.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task BuildStreamBinaryResponseAsync_NullResponse_ReturnsInternalServerError()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act
        var result = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.ContentStream.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();
        result.ContentLength.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamBinaryResponseAsync_SuccessResponse_ReturnsContentStream()
    {
        // Arrange
        var expectedContent = new byte[] { 1, 2, 3, 4, 5 };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedContent),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ContentStream.Should().NotBeNull();
        result.ContentType.Should().Be("image/png");

        // Verify stream content
        using var memoryStream = new MemoryStream();
        await result.ContentStream!.CopyToAsync(memoryStream);
        memoryStream.ToArray().Should().BeEquivalentTo(expectedContent);
    }

    [Fact]
    public async Task BuildStreamBinaryResponseAsync_WithContentDisposition_ReturnsFileName()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3]),
        };
        response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        {
            FileName = "\"image.png\"",
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.FileName.Should().Be("image.png");
    }

    [Fact]
    public async Task BuildStreamBinaryResponseAsync_WithContentLength_ReturnsContentLength()
    {
        // Arrange
        var content = new byte[100];
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content),
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.ContentLength.Should().Be(100);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Created, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task BuildStreamBinaryResponseAsync_RespectsHttpStatusCode(HttpStatusCode statusCode, bool expectedSuccess)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode)
        {
            Content = new ByteArrayContent([1, 2, 3]),
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().Be(expectedSuccess);
        result.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task BuildStreamBinaryResponseAsync_StreamCanBeReadMultipleTimes()
    {
        // Arrange - using MemoryStream which supports seeking
        var expectedContent = new byte[] { 10, 20, 30, 40, 50 };
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedContent),
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);

        // Assert - read once
        using var ms1 = new MemoryStream();
        await result.ContentStream!.CopyToAsync(ms1);
        ms1.ToArray().Should().BeEquivalentTo(expectedContent);

        // Reset and read again
        result.ContentStream.Position = 0;
        using var ms2 = new MemoryStream();
        await result.ContentStream.CopyToAsync(ms2);
        ms2.ToArray().Should().BeEquivalentTo(expectedContent);
    }
}