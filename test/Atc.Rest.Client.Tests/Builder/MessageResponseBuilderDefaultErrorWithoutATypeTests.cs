namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderDefaultErrorWithoutATypeTests
{
    private const string ProblemXml = "<problem><title>I'm a teapot</title><status>418</status></problem>";
    private const string ProblemJson = "{\"title\":\"Not found\",\"status\":404,\"detail\":\"No item 7\"}";
    private const string BadJson = "{\"error\":\"No such item\"}";
    private const string ProblemMediaType = "application/problem+json";
    private const HttpStatusCode ImATeapot = (HttpStatusCode)418;

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_WithAnXmlBody_DeserializesNothingAndKeepsTheText(
        string kind)
    {
        // Arrange
        using var response = CreateResponse(ImATeapot, ProblemXml, "application/xml");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(ImATeapot);
        result.Text.Should().Be(ProblemXml);
        result.Object.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_GivesTheResponseOfTheStatusRegisteredWithoutAType(
        string kind)
    {
        // Arrange
        using var forDefault = CreateResponse(ImATeapot, ProblemXml, "application/xml");
        using var forStatus = CreateResponse(ImATeapot, ProblemXml, "application/xml");
        var withDefault = CreateSut(forDefault);
        withDefault.AddDefaultErrorResponse();
        var withStatus = CreateSut(forStatus);
        withStatus.AddErrorResponse(ImATeapot);

        // Act
        var result = await BuildAsync(kind, withDefault);
        var expected = await BuildAsync(kind, withStatus);

        // Assert
        result.IsSuccess.Should().Be(expected.IsSuccess);
        result.StatusCode.Should().Be(expected.StatusCode);
        result.Text.Should().Be(expected.Text);
        result.Object.Should().Be(expected.Object);
        result.Headers.Should().BeEquivalentTo(expected.Headers);
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKindsAndErrorStatusCodes), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_IsUsedForEveryErrorStatusWithoutARegistration(
        string kind,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Text.Should().Be(ProblemJson);
        result.Object.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_IsNotUsedForAStatusWithItsOwnRegistration(
        string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, BadJson, "application/json");
        var sut = CreateSut(response);
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound);
        sut.AddDefaultErrorResponse();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Object.Should().BeOfType<BadResponse>().Which.Error.Should().Be("No such item");
    }

    [Fact]
    public async Task DefaultWithoutAType_IsNotUsedForASuccessStatus()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.OK, "{\"name\":\"a\"}", "application/json");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse();

        // Act
        var result = await sut.BuildResponseAsync(x => x, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ContentObject.Should().Be("{\"name\":\"a\"}");
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_ARegistrationForTheMediaTypeWins(
        string kind)
    {
        // Arrange
        using var problem = CreateResponse(HttpStatusCode.NotFound, ProblemJson, ProblemMediaType);
        using var other = CreateResponse(HttpStatusCode.NotFound, ProblemJson, "application/json");

        // Act
        var problemResult = await BuildAsync(kind, Register(CreateSut(problem)));
        var otherResult = await BuildAsync(kind, Register(CreateSut(other)));

        // Assert
        problemResult.Object.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("No item 7");
        otherResult.Object.Should().BeNull();

        static MessageResponseBuilder Register(MessageResponseBuilder sut)
        {
            sut.AddDefaultErrorResponse();
            sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, ProblemMediaType);
            return sut;
        }
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_AfterATypedDefault_Wins(string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();
        sut.AddDefaultErrorResponse();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Object.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_BeforeATypedDefault_Loses(string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse();
        sut.AddDefaultErrorResponse<ProblemDetails>();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Object.Should().BeOfType<ProblemDetails>();
    }

    [Theory]
    [MemberData(nameof(MessageResponseBuilderRegistrationTests.ResponseKinds), MemberType = typeof(MessageResponseBuilderRegistrationTests))]
    public async Task DefaultWithoutAType_AfterATextDefault_Wins(string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.Conflict, "Already running", "text/plain");
        var sut = CreateSut(response);
        sut.AddDefaultErrorTextResponse();
        sut.AddDefaultErrorResponse();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Text.Should().Be("Already running");
        result.Object.Should().BeNull();
    }

    [Fact]
    public void DefaultWithoutAType_ReturnsTheBuilder()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act & Assert
        sut.AddDefaultErrorResponse().Should().BeSameAs(sut);
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string body,
        string mediaType)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        };
        response.Headers.TryAddWithoutValidation("X-Request-Id", "r-1");

        return response;
    }

    private static async Task<BuiltResponse> BuildAsync(
        string kind,
        MessageResponseBuilder sut)
    {
        switch (kind)
        {
            case "factory":
                return await sut.BuildResponseAsync(
                    x => new BuiltResponse(x.IsSuccess, x.StatusCode, x.Content, x.ContentObject, x.Headers),
                    CancellationToken.None);
            case "typed":
                var typed = await sut.BuildResponseAsync<SuccessResponse, ProblemDetails>(CancellationToken.None);
                return new BuiltResponse(typed.IsSuccess, typed.StatusCode, typed.Content, typed.ContentObject, typed.Headers);
            case "binary":
                var binary = await sut.BuildBinaryResponseAsync(CancellationToken.None);
                return new BuiltResponse(binary.IsSuccess, binary.StatusCode, binary.ErrorContent, binary.ErrorContentObject, binary.Headers);
            case "stream-binary":
                using (var streamBinary = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None))
                {
                    return new BuiltResponse(streamBinary.IsSuccess, streamBinary.StatusCode, streamBinary.ErrorContent, streamBinary.ErrorContentObject, streamBinary.Headers);
                }

            case "streaming":
                using (var streaming = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>())
                {
                    return new BuiltResponse(streaming.IsSuccess, streaming.StatusCode, streaming.ErrorContent, streaming.ErrorContentObject, streaming.Headers);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown response kind.");
        }
    }

    private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
        => new(response, serializer);

    private sealed record BuiltResponse(
        bool IsSuccess,
        HttpStatusCode StatusCode,
        string? Text,
        object? Object,
        IReadOnlyDictionary<string, IEnumerable<string>> Headers);
}