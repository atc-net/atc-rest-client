namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderErrorContentLimitTests
{
    private const int Limit = 64;

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    public static TheoryData<string> ResponseKinds => new()
    {
        "factory",
        "typed",
        "binary",
        "stream-binary",
        "streaming",
    };

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task ErrorBodyThatNeverEnds_IsCutAtTheLimitAndNotReadFurther(
        string kind)
    {
        // Arrange
        using var body = new EndlessStream(string.Empty, 'x');
        using var response = CreateResponse(HttpStatusCode.InternalServerError, new StreamContent(body));
        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Text.Should().Be(new string('x', Limit));
        body.BytesRead.Should().Be(Limit + 1, "one byte past the limit tells that the body was cut, and no more is read");
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task ErrorBodyOfExactlyTheLimit_IsReadWhole(string kind)
    {
        // Arrange
        var text = new string('x', Limit);
        using var response = CreateResponse(HttpStatusCode.BadGateway, new StringContent(text));
        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Text.Should().Be(text);
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task ErrorBodyOneByteLongerThanTheLimit_IsCut(string kind)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.BadGateway, new StringContent(new string('x', Limit) + "y"));
        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Text.Should().Be(new string('x', Limit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ErrorBodyCutInsideAMultiByteCharacter_EndsBeforeThatCharacter(
        int bytesOfTheCharacterWithinTheLimit)
    {
        // Arrange - the euro sign is three bytes in UTF-8
        var start = new string('x', Limit - bytesOfTheCharacterWithinTheLimit);
        using var response = CreateResponse(HttpStatusCode.BadGateway, new StringContent(start + "€ after"));
        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Text.Should().Be(start);
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task ErrorBodyWithinTheLimit_IsDeserializedToTheRegisteredType(
        string kind)
    {
        // Arrange
        const string json = "{\"title\":\"Not found\",\"status\":404}";
        using var response = CreateResponse(HttpStatusCode.NotFound, new StringContent(json, Encoding.UTF8, "application/problem+json"));
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Text.Should().Be(json);
        result.Object.Should().BeOfType<ProblemDetails>().Which.Title.Should().Be("Not found");
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task ErrorBodyPastTheLimit_IsNotDeserializedAndDoesNotThrow(
        string kind)
    {
        // Arrange
        var json = "{\"title\":\"" + new string('x', Limit) + "\",\"status\":404}";
        using var response = CreateResponse(HttpStatusCode.NotFound, new StringContent(json, Encoding.UTF8, "application/problem+json"));
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Text.Should().Be(json[..Limit]);
        (result.Object is ProblemDetails).Should().BeFalse("a body that was cut is not deserialized");
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task TheLargestLimit_ReadsTheWholeErrorBody(string kind)
    {
        // Arrange
        var text = new string('x', 100_000);
        using var response = CreateResponse(HttpStatusCode.BadGateway, new StringContent(text));
        var sut = CreateSut(response, maxErrorContentLength: int.MaxValue);

        // Act
        var result = await BuildAsync(kind, sut);

        // Assert
        result.Text.Should().Be(text);
    }

    [Theory]
    [InlineData("factory", 0)]
    [InlineData("typed", -1)]
    [InlineData("binary", 0)]
    [InlineData("stream-binary", -1)]
    [InlineData("streaming", 0)]
    public async Task ALimitThatIsNotPositive_ThrowsAndNamesTheOption(
        string kind,
        int maxErrorContentLength)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.BadGateway, new StringContent("error"));
        var sut = CreateSut(response, maxErrorContentLength);

        // Act
        var act = () => BuildAsync(kind, sut);

        // Assert
        var exception = await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        exception.WithMessage("*MaxErrorContentLength*");
    }

    [Theory]
    [InlineData("text/plain; charset=iso-8859-1", new byte[] { 0x62, 0x6C, 0xE5 })]
    [InlineData("text/plain; charset=\"ISO-8859-1\"", new byte[] { 0x62, 0x6C, 0xE5 })]
    [InlineData("text/plain; charset=utf-8", new byte[] { 0x62, 0x6C, 0xC3, 0xA5 })]
    [InlineData("text/plain", new byte[] { 0x62, 0x6C, 0xC3, 0xA5 })]
    [InlineData("text/plain", new byte[] { 0xEF, 0xBB, 0xBF, 0x62, 0x6C, 0xC3, 0xA5 })]
    [InlineData("text/plain; charset=no-such-charset", new byte[] { 0x62, 0x6C, 0xC3, 0xA5 })]
    [InlineData(null, new byte[] { 0x62, 0x6C, 0xC3, 0xA5 })]
    public async Task ErrorBody_IsDecodedWithTheCharsetOfTheContentType(
        string? contentType,
        byte[] body)
    {
        // Arrange
        var content = new ByteArrayContent(body);
        if (contentType is not null)
        {
            content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        using var response = CreateResponse(HttpStatusCode.BadGateway, content);
        var sut = CreateSut(response);

        // Act
        var result = await BuildAsync("streaming", sut);

        // Assert
        result.Text.Should().Be("blå");
    }

    [Fact]
    public async Task SuccessBody_IsNotLimited()
    {
        // Arrange
        var name = new string('x', Limit * 4);
        using var response = CreateResponse(
            HttpStatusCode.OK,
            new StringContent("{\"name\":\"" + name + "\"}", Encoding.UTF8, "application/json"));
        var sut = CreateSut(response);
        sut.AddSuccessResponse<SuccessResponse>(HttpStatusCode.OK);

        // Act
        var result = await sut.BuildResponseAsync<SuccessResponse>(CancellationToken.None);

        // Assert
        result.SuccessContent!.Name.Should().Be(name);
    }

    [Fact]
    public void MaxErrorContentLength_DefaultsToOneMebibyte()
    {
        // Act
        var options = new AtcRestClientOptions();

        // Assert
        options.MaxErrorContentLength.Should().Be(1024 * 1024);
        AtcRestClientOptions.DefaultMaxErrorContentLength.Should().Be(1024 * 1024);
    }

    [Fact]
    public async Task Configure_SetsTheLimitOfAnErrorBody()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClientCore();
        services.Configure<AtcRestClientOptions>(o => o.MaxErrorContentLength = 16);

        // Act
        var text = await ReadErrorTextAsync(services);

        // Assert
        text.Should().Be(new string('x', 16));
    }

    [Fact]
    public async Task AddAtcRestClient_WithOptions_SetsTheLimitOfAnErrorBody()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClient(
            "limited",
            new AtcRestClientOptions { BaseAddress = new Uri("https://limited.example"), MaxErrorContentLength = 16 });
        services.AddAtcRestClient(
            "other",
            new AtcRestClientOptions { BaseAddress = new Uri("https://other.example") });

        // Act
        var text = await ReadErrorTextAsync(services);

        // Assert
        text.Should().Be(new string('x', 16));
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        HttpContent content)
        => new(statusCode)
        {
            Content = content,
        };

    private static async Task<string?> ReadErrorTextAsync(
        ServiceCollection services)
    {
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageFactory>();
        using var response = CreateResponse(HttpStatusCode.BadGateway, new StringContent(new string('x', 100)));

        var result = await factory
            .FromResponse(response)
            .BuildBinaryResponseAsync(CancellationToken.None);
        return result.ErrorContent;
    }

    private static async Task<(bool IsSuccess, HttpStatusCode StatusCode, string? Text, object? Object)> BuildAsync(
        string kind,
        MessageResponseBuilder sut)
    {
        switch (kind)
        {
            case "factory":
                return await sut.BuildResponseAsync(
                    x => (x.IsSuccess, x.StatusCode, x.Content, x.ContentObject),
                    CancellationToken.None);
            case "typed":
                var typed = await sut.BuildResponseAsync<SuccessResponse, ProblemDetails>(CancellationToken.None);
                return (typed.IsSuccess, typed.StatusCode, typed.Content, typed.ContentObject);
            case "binary":
                var binary = await sut.BuildBinaryResponseAsync(CancellationToken.None);
                return (binary.IsSuccess, binary.StatusCode, binary.ErrorContent, binary.ErrorContentObject);
            case "stream-binary":
                using (var streamBinary = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None))
                {
                    return (streamBinary.IsSuccess, streamBinary.StatusCode, streamBinary.ErrorContent, streamBinary.ErrorContentObject);
                }

            case "streaming":
                using (var streaming = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>())
                {
                    return (streaming.IsSuccess, streaming.StatusCode, streaming.ErrorContent, streaming.ErrorContentObject);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown response kind.");
        }
    }

    private MessageResponseBuilder CreateSut(
        HttpResponseMessage response,
        int maxErrorContentLength = Limit)
        => new(response, serializer, maxErrorContentLength: maxErrorContentLength);
}