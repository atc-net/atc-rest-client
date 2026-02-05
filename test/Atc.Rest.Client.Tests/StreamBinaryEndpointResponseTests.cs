namespace Atc.Rest.Client.Tests;

public sealed class StreamBinaryEndpointResponseTests
{
    [Fact]
    public void InvalidContentAccessException_ContainsExpectedStatusCode()
    {
        // Arrange
        using var sut = new TestableStreamBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        var exception = sut.GetInvalidContentAccessException(HttpStatusCode.OK, "OKContent");

        // Assert
        exception.Message.Should().Contain("200");
        exception.Message.Should().Contain("OK");
    }

    [Fact]
    public void InvalidContentAccessException_ContainsActualStatusCode()
    {
        // Arrange
        using var sut = new TestableStreamBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        var exception = sut.GetInvalidContentAccessException(HttpStatusCode.OK, "OKContent");

        // Assert
        exception.Message.Should().Contain("409");
        exception.Message.Should().Contain("Conflict");
    }

    [Fact]
    public void InvalidContentAccessException_ContainsPropertyName()
    {
        // Arrange
        using var sut = new TestableStreamBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        var exception = sut.GetInvalidContentAccessException(HttpStatusCode.OK, "OKContent");

        // Assert
        exception.Message.Should().Contain("OKContent");
    }

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            "application/octet-stream",
            "test.bin",
            contentLength: 3);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.OK);
        sut.Content.Should().BeSameAs(stream);
        sut.ContentType.Should().Be("application/octet-stream");
        sut.FileName.Should().Be("test.bin");
        sut.ContentLength.Should().Be(3);
    }

    [Fact]
    public void Dispose_DisposesContent()
    {
        // Arrange
        var stream = new MemoryStream([1, 2, 3]);
        var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        sut.Dispose();

        // Assert
        var act = () => stream.ReadByte();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        var act = () => sut.Dispose();

        // Assert - first dispose already happens via using, this is the second
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenContentIsNull_DoesNotThrow()
    {
        // Arrange
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.InternalServerError,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        var act = () => sut.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullValues_SetsPropertiesToNull()
    {
        // Arrange & Act
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.NotFound,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Assert
        sut.IsSuccess.Should().BeFalse();
        sut.Content.Should().BeNull();
        sut.ContentType.Should().BeNull();
        sut.FileName.Should().BeNull();
        sut.ContentLength.Should().BeNull();
        sut.ErrorContent.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithErrorContent_SetsProperty()
    {
        // Arrange
        const string errorContent = "Error: Not Found";

        // Act
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.NotFound,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null,
            errorContent);

        // Assert
        sut.ErrorContent.Should().Be(errorContent);
    }

    [Fact]
    public void ErrorContent_IsNull_WhenSuccessful()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            "application/octet-stream",
            "test.bin",
            contentLength: 3,
            errorContent: null);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.ErrorContent.Should().BeNull();
    }

    private sealed class TestableStreamBinaryEndpointResponse : StreamBinaryEndpointResponse
    {
        public TestableStreamBinaryEndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            Stream? contentStream,
            string? contentType,
            string? fileName,
            long? contentLength)
            : base(isSuccess, statusCode, contentStream, contentType, fileName, contentLength)
        {
        }

        public InvalidOperationException GetInvalidContentAccessException(
            HttpStatusCode expectedStatusCode,
            string propertyName)
            => InvalidContentAccessException(expectedStatusCode, propertyName);
    }
}