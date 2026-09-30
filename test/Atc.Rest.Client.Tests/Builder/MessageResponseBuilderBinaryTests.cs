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
    public async Task BuildBinaryResponseAsync_RespectsHttpStatusCode(
        HttpStatusCode statusCode,
        bool expectedSuccess)
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
        result.Content.Should().BeNull();
        result.ContentType.Should().BeNull();
        result.FileName.Should().BeNull();
        result.ContentLength.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamBinaryResponseAsync_SuccessResponse_ReturnsContent()
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
        result.Content.Should().NotBeNull();
        result.ContentType.Should().Be("image/png");

        // Verify stream content
        using var memoryStream = new MemoryStream();
        await result.Content!.CopyToAsync(memoryStream);
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
    public async Task BuildStreamBinaryResponseAsync_RespectsHttpStatusCode(
        HttpStatusCode statusCode,
        bool expectedSuccess)
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
        await result.Content!.CopyToAsync(ms1);
        ms1.ToArray().Should().BeEquivalentTo(expectedContent);

        // Reset and read again
        result.Content.Position = 0;
        using var ms2 = new MemoryStream();
        await result.Content.CopyToAsync(ms2);
        ms2.ToArray().Should().BeEquivalentTo(expectedContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildBinaryResponse_ErrorWithRegisteredType_ReturnsTypedErrorContentObject(
        bool useStream)
    {
        // Arrange
        const string errorBody = """{"error":"not found"}""";
        var expectedError = new BadResponse { Error = "not found" };
        serializer.Deserialize<BadResponse>(errorBody).Returns(expectedError);

        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorBody, System.Text.Encoding.UTF8, "application/problem+json"),
        };

        var sut = CreateSut(response);
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound);

        // Act
        var result = await BuildAsync(sut, useStream);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorContent.Should().Be(errorBody);
        result.ErrorContentObject.Should().BeSameAs(expectedError);
        result.ContentType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildBinaryResponse_ErrorWithoutRegisteredType_ReturnsRawErrorAndMetadata(
        bool useStream)
    {
        // Arrange
        const string errorBody = "<html><body>Bad Gateway</body></html>";

        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(errorBody, System.Text.Encoding.UTF8, "text/html"),
        };

        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(sut, useStream);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorContent.Should().Be(errorBody);
        result.ErrorContentObject.Should().BeNull();
        result.ContentType.Should().Be("text/html");
        result.ContentLength.Should().Be(System.Text.Encoding.UTF8.GetByteCount(errorBody));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildBinaryResponse_ErrorWhenDeserializationFails_ReturnsNullErrorContentObject(
        bool useStream)
    {
        // Arrange
        const string errorBody = "Not Found";
        serializer.Deserialize<BadResponse>(Arg.Any<string>()).Throws(new JsonException("Parse error"));

        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorBody, System.Text.Encoding.UTF8, "text/plain"),
        };

        var sut = CreateSut(response);
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound);

        // Act
        var result = await BuildAsync(sut, useStream);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorContent.Should().Be(errorBody);
        result.ErrorContentObject.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildBinaryResponse_Error_ExposesResponseHeaders(
        bool useStream)
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("slow down"),
        };
        response.Headers.Add("Retry-After", "30");

        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(sut, useStream);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Headers.Should().ContainKey("Retry-After");
        result.Headers["Retry-After"].Should().ContainSingle().Which.Should().Be("30");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildBinaryResponse_Success_ExposesResponseAndContentHeaders(
        bool useStream)
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3]),
        };
        response.Headers.Add("ETag", "\"abc\"");
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(sut, useStream);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Headers["ETag"].Should().ContainSingle().Which.Should().Be("\"abc\"");
        result.Headers["Content-Type"].Should().ContainSingle().Which.Should().Be("application/pdf");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildBinaryResponse_WithFileNameStar_PrefersDecodedFileNameStar(
        bool useStream)
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3]),
        };
        response.Content.Headers.Add(
            "Content-Disposition",
            "attachment; filename=\"Rapport-_.pdf\"; filename*=UTF-8''Rapport-%C3%85.pdf");

        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(sut, useStream);

        // Assert
        result.FileName.Should().Be("Rapport-Å.pdf");
    }

    private static async Task<BinaryResult> BuildAsync(
        MessageResponseBuilder sut,
        bool useStream)
    {
        if (!useStream)
        {
            var bytes = await sut.BuildBinaryResponseAsync(CancellationToken.None);
            return new BinaryResult(
                bytes.IsSuccess,
                bytes.ContentType,
                bytes.ContentLength,
                bytes.FileName,
                bytes.ErrorContent,
                bytes.ErrorContentObject,
                bytes.Headers);
        }

        using var stream = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None);
        return new BinaryResult(
            stream.IsSuccess,
            stream.ContentType,
            stream.ContentLength,
            stream.FileName,
            stream.ErrorContent,
            stream.ErrorContentObject,
            stream.Headers);
    }

    private sealed record BinaryResult(
        bool IsSuccess,
        string? ContentType,
        long? ContentLength,
        string? FileName,
        string? ErrorContent,
        object? ErrorContentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> Headers);
}