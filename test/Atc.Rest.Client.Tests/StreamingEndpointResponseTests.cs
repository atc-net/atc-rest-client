namespace Atc.Rest.Client.Tests;

public sealed class StreamingEndpointResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var content = AsyncEnumerable.Empty<string>();
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: true,
            HttpStatusCode.OK,
            content,
            errorContent: null,
            httpResponse);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.IsOk.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.OK);
        sut.Content.Should().BeSameAs(content);
        sut.ErrorContent.Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsErrorProperties()
    {
        // Arrange
        const string errorContent = "Bad Request Error";
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        // Act
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.BadRequest,
            content: null,
            errorContent,
            httpResponse);

        // Assert
        sut.IsSuccess.Should().BeFalse();
        sut.IsOk.Should().BeFalse();
        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sut.Content.Should().BeNull();
        sut.ErrorContent.Should().Be(errorContent);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Created, false)]
    [InlineData(HttpStatusCode.NoContent, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void IsOk_ReturnsCorrectValue(HttpStatusCode statusCode, bool expected)
    {
        // Arrange & Act
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: false,
            statusCode,
            content: null,
            errorContent: null,
            httpResponse: null);

        // Assert
        sut.IsOk.Should().Be(expected);
    }

    [Fact]
    public async Task Dispose_DisposesHttpResponse()
    {
        // Arrange
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("test"),
        };
        var sut = new StreamingEndpointResponse<string>(
            isSuccess: true,
            HttpStatusCode.OK,
            content: null,
            errorContent: null,
            httpResponse);

        // Act
        sut.Dispose();

        // Assert - accessing disposed response should throw
        var act = () => httpResponse.Content.ReadAsStringAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        // Arrange
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: true,
            HttpStatusCode.OK,
            content: null,
            errorContent: null,
            httpResponse);

        // Act
        var act = () => sut.Dispose();

        // Assert - first dispose already happens via using, this is the second
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_HandlesNullHttpResponse()
    {
        // Arrange
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.InternalServerError,
            content: null,
            errorContent: null,
            httpResponse: null);

        // Act
        var act = () => sut.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidContentAccessException_ContainsExpectedStatusCode()
    {
        // Arrange
        using var sut = new TestableStreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.Conflict,
            content: null,
            errorContent: null,
            httpResponse: null);

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
        using var sut = new TestableStreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.Conflict,
            content: null,
            errorContent: null,
            httpResponse: null);

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
        using var sut = new TestableStreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.Conflict,
            content: null,
            errorContent: null,
            httpResponse: null);

        // Act
        var exception = sut.GetInvalidContentAccessException(HttpStatusCode.OK, "OKContent");

        // Assert
        exception.Message.Should().Contain("OKContent");
    }

    private sealed class TestableStreamingEndpointResponse<T> : StreamingEndpointResponse<T>
    {
        public TestableStreamingEndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            IAsyncEnumerable<T?>? content,
            string? errorContent,
            HttpResponseMessage? httpResponse)
            : base(isSuccess, statusCode, content, errorContent, httpResponse)
        {
        }

        public InvalidOperationException GetInvalidContentAccessException(
            HttpStatusCode expectedStatusCode,
            string propertyName)
            => InvalidContentAccessException(expectedStatusCode, propertyName);
    }
}