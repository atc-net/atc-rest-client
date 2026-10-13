namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderRegistrationTests
{
    private const string ProblemJson = "{\"title\":\"Not found\",\"status\":404,\"detail\":\"No item 7\"}";
    private const string BadJson = "{\"error\":\"No such item\"}";
    private const string ProblemMediaType = "application/problem+json";

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    public static TheoryData<string> ResponseKinds => new()
    {
        "factory",
        "typed",
        "binary",
        "stream-binary",
        "streaming",
    };

    public static TheoryData<string, HttpStatusCode> ResponseKindsAndErrorStatusCodes
    {
        get
        {
            var data = new TheoryData<string, HttpStatusCode>();
            foreach (var kind in ResponseKinds)
            {
                foreach (var statusCode in Enum.GetValues<HttpStatusCode>().Distinct().Where(x => (int)x >= 400))
                {
                    data.Add(kind, statusCode);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ResponseKindsAndErrorStatusCodes))]
    public async Task DefaultErrorResponse_IsUsedForEveryErrorStatusWithoutARegistration(
        string kind,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Object.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("No item 7");
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task DefaultErrorResponse_IsNotUsedForAStatusWithItsOwnRegistration(
        string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, BadJson, "application/json");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Object.Should().BeOfType<BadResponse>().Which.Error.Should().Be("No such item");
    }

    [Theory]
    [InlineData("binary")]
    [InlineData("stream-binary")]
    [InlineData("streaming")]
    public async Task DefaultErrorResponse_IsNotUsedForAStatusRegisteredWithoutAType(
        string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();
        sut.AddErrorResponse(HttpStatusCode.NotFound);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Object.Should().BeNull();
    }

    [Fact]
    public async Task DefaultErrorResponse_IsNotUsedForASuccessStatus()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.OK, "{\"name\":\"a\"}", "application/json");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();

        // Act
        var result = await sut.BuildResponseAsync(x => x, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ContentObject.Should().BeOfType<string>();
    }

    [Fact]
    public async Task DefaultErrorResponse_IsNotUsedForAStatusRegisteredAsSuccess()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, "{\"name\":\"a\"}", "application/json");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();
        sut.AddSuccessResponse<SuccessResponse>(HttpStatusCode.NotFound);

        // Act
        var result = await sut.BuildResponseAsync(x => x, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ContentObject.Should().BeOfType<SuccessResponse>();
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task DefaultErrorResponse_WithABodyThatIsNotTheType_DoesNotThrow(
        string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.BadGateway, "<html>502</html>", "text/html");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Text.Should().Be("<html>502</html>");
        (result.Object is ProblemDetails).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task DefaultErrorTextResponse_GivesTheText(string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.Conflict, "Already running", "text/plain");
        var sut = CreateSut(response);
        sut.AddDefaultErrorTextResponse();

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Object.Should().Be("Already running");
    }

    [Fact]
    public async Task DefaultErrorResponse_RegisteredTwice_TheLaterOneWins()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, BadJson, "application/json");
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<ProblemDetails>();
        sut.AddDefaultErrorResponse<BadResponse>();

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Object.Should().BeOfType<BadResponse>();
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task ErrorResponseForAMediaType_IsUsedForThatMediaTypeOnly(
        string kind)
    {
        // Arrange
        using var problem = CreateResponse(HttpStatusCode.NotFound, ProblemJson, ProblemMediaType);
        using var model = CreateResponse(HttpStatusCode.NotFound, BadJson, "application/json");

        // Act
        var problemResult = await BuildAsync(kind, Register(CreateSut(problem)));
        var modelResult = await BuildAsync(kind, Register(CreateSut(model)));

        // Assert
        problemResult.Object.Should().BeOfType<ProblemDetails>().Which.Title.Should().Be("Not found");
        modelResult.Object.Should().BeOfType<BadResponse>().Which.Error.Should().Be("No such item");

        static MessageResponseBuilder Register(MessageResponseBuilder sut)
        {
            sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound);
            sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, ProblemMediaType);
            return sut;
        }
    }

    [Theory]
    [InlineData("application/problem+json; charset=utf-8", "application/problem+json")]
    [InlineData("APPLICATION/PROBLEM+JSON", "application/problem+json")]
    [InlineData("application/problem+json", "Application/Problem+Json")]
    [InlineData("application/problem+json", "application/problem+json; charset=utf-8")]
    [InlineData("application/problem+json", " application/problem+json ")]
    public async Task ErrorResponseForAMediaType_MatchesWithoutParametersAndCasing(
        string responseContentType,
        string registeredMediaType)
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(ProblemJson)),
        };
        response.Content.Headers.TryAddWithoutValidation("Content-Type", responseContentType);
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, registeredMediaType);

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Object.Should().BeOfType<ProblemDetails>();
    }

    [Theory]
    [InlineData(ProblemMediaType, typeof(ProblemDetails))]
    [InlineData("application/json", typeof(BadResponse))]
    [InlineData("application/vnd.other+json", typeof(BadResponse))]
    public async Task Lookup_AMediaTypeWinsOverTheStatusWhichWinsOverTheDefault(
        string mediaType,
        Type expectedType)
    {
        // Arrange - the default would take every one of them, the status registration all but the problem
        using var response = CreateResponse(HttpStatusCode.NotFound, "{}", mediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<SuccessResponse>();
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, ProblemMediaType);

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Object.Should().BeOfType(expectedType);
    }

    [Fact]
    public async Task Lookup_AMediaTypeOfAnotherStatus_FallsBackToTheDefault()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.Conflict, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddDefaultErrorResponse<BadResponse>();
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, ProblemMediaType);

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Object.Should().BeOfType<BadResponse>();
    }

    [Fact]
    public async Task ErrorResponseForAMediaType_RegisteredTwice_TheLaterOneWins()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, BadJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, ProblemMediaType);
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.NotFound, "APPLICATION/PROBLEM+JSON");

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Object.Should().BeOfType<BadResponse>();
    }

    [Fact]
    public async Task ErrorResponseForAMediaType_MakesTheStatusAnError()
    {
        // Arrange - 304 is no success status by itself, 200 is
        using var response = CreateResponse(HttpStatusCode.OK, ProblemJson, ProblemMediaType);
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.OK, ProblemMediaType);

        // Act
        var result = await sut.BuildResponseAsync(x => x, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ContentObject.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task SuccessResponseForAMediaType_IsUsedForThatMediaTypeOnly()
    {
        // Arrange
        using var summary = CreateResponse(HttpStatusCode.OK, "{\"name\":\"a\"}", "application/vnd.summary+json");
        using var full = CreateResponse(HttpStatusCode.OK, "{\"error\":\"b\"}", "application/json");

        // Act
        var summaryResult = await Register(CreateSut(summary)).BuildResponseAsync(x => x, CancellationToken.None);
        var fullResult = await Register(CreateSut(full)).BuildResponseAsync(x => x, CancellationToken.None);

        // Assert
        summaryResult.IsSuccess.Should().BeTrue();
        summaryResult.ContentObject.Should().BeOfType<SuccessResponse>().Which.Name.Should().Be("a");
        fullResult.ContentObject.Should().BeOfType<BadResponse>().Which.Error.Should().Be("b");

        static MessageResponseBuilder Register(MessageResponseBuilder sut)
        {
            sut.AddSuccessResponse<BadResponse>(HttpStatusCode.OK);
            sut.AddSuccessResponse<SuccessResponse>(HttpStatusCode.OK, "application/vnd.summary+json");
            return sut;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("; charset=utf-8")]
    public void ResponseForAMediaType_WithoutAMediaType_Throws(
        string? mediaType)
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act
        var error = () => sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, mediaType!);
        var success = () => sut.AddSuccessResponse<SuccessResponse>(HttpStatusCode.OK, mediaType!);

        // Assert
        error.Should().Throw<ArgumentException>().WithParameterName(nameof(mediaType));
        success.Should().Throw<ArgumentException>().WithParameterName(nameof(mediaType));
    }

    [Fact]
    public void Registrations_ReturnTheBuilder()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act & Assert
        sut.AddDefaultErrorResponse<ProblemDetails>().Should().BeSameAs(sut);
        sut.AddDefaultErrorTextResponse().Should().BeSameAs(sut);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound, ProblemMediaType).Should().BeSameAs(sut);
        sut.AddSuccessResponse<SuccessResponse>(HttpStatusCode.OK, "application/json").Should().BeSameAs(sut);
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string body,
        string mediaType)
        => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        };

    private static async Task<(bool IsSuccess, string? Text, object? Object)> BuildAsync(
        string kind,
        MessageResponseBuilder sut)
    {
        switch (kind)
        {
            case "factory":
                return await sut.BuildResponseAsync(
                    x => (x.IsSuccess, x.Content, x.ContentObject),
                    CancellationToken.None);
            case "typed":
                var typed = await sut.BuildResponseAsync<SuccessResponse, ProblemDetails>(CancellationToken.None);
                return (typed.IsSuccess, typed.Content, typed.ContentObject);
            case "binary":
                var binary = await sut.BuildBinaryResponseAsync(CancellationToken.None);
                return (binary.IsSuccess, binary.ErrorContent, binary.ErrorContentObject);
            case "stream-binary":
                using (var streamBinary = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None))
                {
                    return (streamBinary.IsSuccess, streamBinary.ErrorContent, streamBinary.ErrorContentObject);
                }

            case "streaming":
                using (var streaming = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>())
                {
                    return (streaming.IsSuccess, streaming.ErrorContent, streaming.ErrorContentObject);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown response kind.");
        }
    }

    private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
        => new(response, serializer);
}