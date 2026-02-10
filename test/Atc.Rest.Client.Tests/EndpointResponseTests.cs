namespace Atc.Rest.Client.Tests;

public class EndpointResponseTests
{
    [Fact]
    public void InvalidContentAccessException_ContainsExpectedStatusCode()
    {
        // Arrange
        var sut = new TestableEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            "{\"error\":\"conflict\"}",
            new BadResponse(),
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Act
        var exception = sut.GetInvalidContentAccessException<SuccessResponse>(
            HttpStatusCode.Created,
            "CreatedContent");

        // Assert
        exception.Message.Should().Contain("201");
        exception.Message.Should().Contain("Created");
    }

    [Fact]
    public void InvalidContentAccessException_ContainsActualStatusCode()
    {
        // Arrange
        var sut = new TestableEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            "{\"error\":\"conflict\"}",
            new BadResponse(),
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Act
        var exception = sut.GetInvalidContentAccessException<SuccessResponse>(
            HttpStatusCode.Created,
            "CreatedContent");

        // Assert
        exception.Message.Should().Contain("409");
        exception.Message.Should().Contain("Conflict");
    }

    [Fact]
    public void InvalidContentAccessException_ContainsExpectedAndActualTypes()
    {
        // Arrange
        var sut = new TestableEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            "{\"error\":\"conflict\"}",
            new BadResponse(),
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Act
        var exception = sut.GetInvalidContentAccessException<SuccessResponse>(
            HttpStatusCode.Created,
            "CreatedContent");

        // Assert
        exception.Message.Should().Contain("SuccessResponse");
        exception.Message.Should().Contain("BadResponse");
    }

    [Fact]
    public void InvalidContentAccessException_ContainsPropertyName()
    {
        // Arrange
        var sut = new TestableEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            "{\"error\":\"conflict\"}",
            new BadResponse(),
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Act
        var exception = sut.GetInvalidContentAccessException<SuccessResponse>(
            HttpStatusCode.Created,
            "CreatedContent");

        // Assert
        exception.Message.Should().Contain("CreatedContent");
    }

    [Fact]
    public void InvalidContentAccessException_ContainsResponseContent()
    {
        // Arrange
        var responseContent = "{\"error\":\"conflict\"}";
        var sut = new TestableEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            responseContent,
            new BadResponse(),
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Act
        var exception = sut.GetInvalidContentAccessException<SuccessResponse>(
            HttpStatusCode.Created,
            "CreatedContent");

        // Assert
        exception.Message.Should().Contain(responseContent);
    }

    [Fact]
    public void InvalidContentAccessException_WithNullContentObject_ContainsNullType()
    {
        // Arrange
        var sut = new TestableEndpointResponse(
            isSuccess: false,
            HttpStatusCode.Conflict,
            string.Empty,
            null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Act
        var exception = sut.GetInvalidContentAccessException<SuccessResponse>(
            HttpStatusCode.Created,
            "CreatedContent");

        // Assert
        exception.Message.Should().Contain("null");
    }

    [Theory, AutoNSubstituteData]
    public void SuccessContent_Returns_Response_Upon_Success(
        SuccessResponse contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        var sut = new EndpointResponse<SuccessResponse>(
            true,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(contentObject),
            contentObject,
            headers);

        sut
            .SuccessContent
            .Should()
            .Be(contentObject);
    }

    [Fact]
    public void SuccessContent_Returns_Null_Upon_Failure()
    {
        var sut = new EndpointResponse<SuccessResponse>(
            false,
            HttpStatusCode.BadRequest,
            string.Empty,
            null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        sut
            .SuccessContent
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void FailureContent_Returns_Null_Upon_Success(
        SuccessResponse contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        var sut = new EndpointResponse<SuccessResponse, BadResponse>(
            true,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(contentObject),
            contentObject,
            headers);

        sut
            .ErrorContent
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void FailureContent_Returns_Response_Upon_Failure(
        BadResponse contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        var sut = new EndpointResponse<SuccessResponse, BadResponse>(
            false,
            HttpStatusCode.BadRequest,
            JsonSerializer.Serialize(contentObject),
            contentObject,
            headers);

        sut
            .ErrorContent
            .Should()
            .Be(contentObject);
    }

    [Fact]
    public void CopyConstructor_CopiesAllProperties()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
        {
            { "X-Test", new[] { "value" } },
        };
        var original = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.Created,
            "content",
            new SuccessResponse(),
            headers);

        // Act
        var copy = new EndpointResponse(original);

        // Assert
        copy.IsSuccess.Should().Be(original.IsSuccess);
        copy.StatusCode.Should().Be(original.StatusCode);
        copy.Content.Should().Be(original.Content);
        copy.ContentObject.Should().Be(original.ContentObject);
        copy.Headers.Should().BeSameAs(original.Headers);
    }

    [Fact]
    public void CopyConstructor_WithNull_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new EndpointResponse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public void GenericCopyConstructor_CopiesAllProperties()
    {
        // Arrange
        var contentObject = new SuccessResponse();
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
        var original = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            "content",
            contentObject,
            headers);

        // Act
        var copy = new EndpointResponse<SuccessResponse>(original);

        // Assert
        copy.IsSuccess.Should().Be(original.IsSuccess);
        copy.StatusCode.Should().Be(original.StatusCode);
        copy.Content.Should().Be(original.Content);
        copy.SuccessContent.Should().Be(contentObject);
    }

    [Fact]
    public void GenericTwoTypeCopyConstructor_CopiesAllProperties()
    {
        // Arrange
        var contentObject = new SuccessResponse();
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
        var original = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            "content",
            contentObject,
            headers);

        // Act
        var copy = new EndpointResponse<SuccessResponse, BadResponse>(original);

        // Assert
        copy.IsSuccess.Should().Be(original.IsSuccess);
        copy.StatusCode.Should().Be(original.StatusCode);
        copy.Content.Should().Be(original.Content);
        copy.SuccessContent.Should().Be(contentObject);
        copy.ErrorContent.Should().BeNull();
    }

    [Fact]
    public void SuccessContent_WhenContentObjectIsWrongType_ThrowsInvalidCastException()
    {
        // Arrange - Use copy constructor to inject wrong type via base EndpointResponse
        var wrongContent = new BadResponse();
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
        var baseResponse = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            "content",
            wrongContent,
            headers);
        var sut = new EndpointResponse<SuccessResponse>(baseResponse);

        // Act
        var act = () => _ = sut.SuccessContent;

        // Assert
        act.Should().Throw<InvalidCastException>()
            .WithMessage("*SuccessResponse*");
    }

    [Fact]
    public void ErrorContent_WhenContentObjectIsWrongType_ThrowsInvalidCastException()
    {
        // Arrange - Use copy constructor to inject wrong type via base EndpointResponse
        var wrongContent = new SuccessResponse();
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
        var baseResponse = new EndpointResponse(
            isSuccess: false,
            HttpStatusCode.BadRequest,
            "content",
            wrongContent,
            headers);
        var sut = new EndpointResponse<SuccessResponse, BadResponse>(baseResponse);

        // Act
        var act = () => _ = sut.ErrorContent;

        // Assert
        act.Should().Throw<InvalidCastException>()
            .WithMessage("*BadResponse*");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    public void Constructor_SetsStatusCodeCorrectly(HttpStatusCode statusCode)
    {
        // Arrange & Act
        var sut = new EndpointResponse(
            isSuccess: true,
            statusCode,
            string.Empty,
            null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Assert
        sut.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void Headers_ReturnsEmptyDictionary_WhenNoHeadersProvided()
    {
        // Arrange
        var emptyHeaders = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);

        // Act
        var sut = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            string.Empty,
            null,
            emptyHeaders);

        // Assert
        sut.Headers.Should().BeEmpty();
    }

    [Fact]
    public void Headers_ReturnsMultipleHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
        {
            { "Content-Type", new[] { "application/json" } },
            { "X-Request-Id", new[] { "12345" } },
            { "X-Multi-Value", new[] { "a", "b", "c" } },
        };

        // Act
        var sut = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            string.Empty,
            null,
            headers);

        // Assert
        sut.Headers.Should().HaveCount(3);
        sut.Headers["X-Multi-Value"].Should().BeEquivalentTo("a", "b", "c");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSuccess_ReturnsCorrectValue(bool isSuccess)
    {
        // Arrange & Act
        var sut = new EndpointResponse(
            isSuccess,
            HttpStatusCode.OK,
            string.Empty,
            null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Assert
        sut.IsSuccess.Should().Be(isSuccess);
    }

    [Fact]
    public void Content_ReturnsRawStringContent()
    {
        // Arrange
        const string expectedContent = "{\"name\":\"test\",\"value\":42}";

        // Act
        var sut = new EndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            expectedContent,
            null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        // Assert
        sut.Content.Should().Be(expectedContent);
    }

    private sealed class TestableEndpointResponse : EndpointResponse
    {
        public TestableEndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            string content,
            object? contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
            : base(isSuccess, statusCode, content, contentObject, headers)
        {
        }

        public InvalidOperationException GetInvalidContentAccessException<TExpected>(
            HttpStatusCode expectedStatusCode,
            string propertyName)
            where TExpected : class
            => InvalidContentAccessException<TExpected>(expectedStatusCode, propertyName);
    }
}