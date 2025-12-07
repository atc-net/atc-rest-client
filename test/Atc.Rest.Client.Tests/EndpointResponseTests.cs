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