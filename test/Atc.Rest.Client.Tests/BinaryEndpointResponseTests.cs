namespace Atc.Rest.Client.Tests;

public sealed class BinaryEndpointResponseTests
{
    [Fact]
    public void InvalidContentAccessException_ContainsExpectedStatusCode()
    {
        // Arrange
        var sut = new TestableBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            content: null,
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
        var sut = new TestableBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            content: null,
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
        var sut = new TestableBinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            content: null,
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
        var content = new byte[] { 1, 2, 3 };

        // Act
        var sut = new BinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            content,
            "application/octet-stream",
            "test.bin",
            contentLength: 3);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.OK);
        sut.Content.Should().BeEquivalentTo(content);
        sut.ContentType.Should().Be("application/octet-stream");
        sut.FileName.Should().Be("test.bin");
        sut.ContentLength.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithNullValues_SetsPropertiesToNull()
    {
        // Arrange & Act
        var sut = new BinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.NotFound,
            content: null,
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
        var sut = new BinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.NotFound,
            content: null,
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
        var content = new byte[] { 1, 2, 3 };

        // Act
        var sut = new BinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            content,
            "application/octet-stream",
            "test.bin",
            contentLength: 3,
            errorContent: null);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.ErrorContent.Should().BeNull();
    }

    private sealed class TestableBinaryEndpointResponse : BinaryEndpointResponse
    {
        public TestableBinaryEndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            byte[]? content,
            string? contentType,
            string? fileName,
            long? contentLength)
            : base(isSuccess, statusCode, content, contentType, fileName, contentLength)
        {
        }

        public InvalidOperationException GetInvalidContentAccessException(
            HttpStatusCode expectedStatusCode,
            string propertyName)
            => InvalidContentAccessException(expectedStatusCode, propertyName);
    }
}