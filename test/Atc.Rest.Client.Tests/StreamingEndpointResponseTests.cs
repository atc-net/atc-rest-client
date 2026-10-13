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
        sut.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sut.Content.Should().BeNull();
        sut.ErrorContent.Should().Be(errorContent);
    }

    [Fact]
    public void Constructor_WithoutHeaders_HasEmptyCaseInsensitiveHeadersAndNoErrorContentObject()
    {
        // Act
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.BadRequest,
            content: null,
            errorContent: "Bad Request Error",
            httpResponse: null);

        // Assert
        sut.ErrorContentObject.Should().BeNull();
        sut.Headers.Should().BeEmpty();
        sut.Headers.Should().BeOfType<Dictionary<string, IEnumerable<string>>>()
            .Which.Comparer.Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithErrorContentObjectAndHeaders_SetsThem()
    {
        // Arrange
        var errorContentObject = new BadResponse { Error = "Bad Request Error" };
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Retry-After"] = ["30"],
        };

        // Act
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.BadRequest,
            content: null,
            errorContent: "{\"error\":\"Bad Request Error\"}",
            httpResponse: null,
            errorContentObject,
            headers);

        // Assert
        sut.ErrorContentObject.Should().BeSameAs(errorContentObject);
        sut.Headers.Should().BeSameAs(headers);
    }

    [Fact]
    public void Constructor_WithNullHeaders_HasEmptyHeaders()
    {
        // Act
        using var sut = new StreamingEndpointResponse<string>(
            isSuccess: false,
            HttpStatusCode.BadRequest,
            content: null,
            errorContent: null,
            httpResponse: null,
            errorContentObject: null,
            headers: null!);

        // Assert
        sut.Headers.Should().BeEmpty();
    }

    [Fact]
    public void CopyConstructor_CarriesEverythingOver()
    {
        // Arrange
        var content = AsyncEnumerable.Empty<string>();
        var errorContentObject = new BadResponse { Error = "Bad Request Error" };
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Retry-After"] = ["30"],
        };
        using var source = new StreamingEndpointResponse<string>(
            isSuccess: true,
            HttpStatusCode.Accepted,
            content,
            errorContent: "text",
            httpResponse: null,
            errorContentObject,
            headers);

        // Act
        using var sut = new DerivedStreamingEndpointResponse<string>(source);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.Accepted);
        sut.Content.Should().BeSameAs(content);
        sut.ErrorContent.Should().Be("text");
        sut.ErrorContentObject.Should().BeSameAs(errorContentObject);
        sut.Headers.Should().BeSameAs(headers);
    }

    [Fact]
    public void CopyConstructor_TakesOverTheHttpResponse()
    {
        // Arrange
        using var httpResponse = new DisposeCountingHttpResponseMessage(HttpStatusCode.OK);
        var source = new StreamingEndpointResponse<string>(
            isSuccess: true,
            HttpStatusCode.OK,
            content: null,
            errorContent: null,
            httpResponse);

        // Act
        var sut = new DerivedStreamingEndpointResponse<string>(source);

        // Assert
        source.Dispose();
        httpResponse.DisposeCount.Should().Be(0);

        sut.Dispose();
        httpResponse.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void CopyConstructor_NullResponse_Throws()
    {
        // Act
        var act = () => new DerivedStreamingEndpointResponse<string>(response: null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("response");
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
}