namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderStreamingErrorTests
{
    private const string ProblemJson = "{\"title\":\"Not found\",\"status\":404,\"detail\":\"No item 7\"}";
    private const string HtmlErrorPage = "<html><body><h1>502 Bad Gateway</h1></body></html>";

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    public static TheoryData<HttpStatusCode> ErrorStatusCodes
    {
        get
        {
            var data = new TheoryData<HttpStatusCode>();
            foreach (var statusCode in Enum.GetValues<HttpStatusCode>().Distinct().Where(x => (int)x >= 400))
            {
                data.Add(statusCode);
            }

            return data;
        }
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_ErrorWithRegisteredType_DeserializesTheErrorBody()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, "application/problem+json");
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().Be(ProblemJson);
        result.ErrorContentObject.Should().BeEquivalentTo(new ProblemDetails
        {
            Title = "Not found",
            Status = 404,
            Detail = "No item 7",
        });
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_ErrorWithoutRegisteredType_HasNoErrorContentObject()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, "application/problem+json");
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorContent.Should().Be(ProblemJson);
        result.ErrorContentObject.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_ErrorRegisteredWithoutType_HasNoErrorContentObject()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, "application/problem+json");
        var sut = CreateSut(response);
        sut.AddErrorResponse(HttpStatusCode.NotFound);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorContent.Should().Be(ProblemJson);
        result.ErrorContentObject.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_ErrorRegisteredAsText_HasTheTextAsErrorContentObject()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.Conflict, "Already running", "text/plain");
        var sut = CreateSut(response);
        sut.AddErrorTextResponse(HttpStatusCode.Conflict);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.ErrorContent.Should().Be("Already running");
        result.ErrorContentObject.Should().Be("Already running");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \r\n")]
    [InlineData(HtmlErrorPage)]
    [InlineData("{\"title\":")]
    public async Task BuildStreamingEndpointResponseAsync_ErrorBodyThatIsNotTheRegisteredType_DoesNotThrow(
        string body)
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.BadGateway, body, "text/html");
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.BadGateway);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        result.ErrorContent.Should().Be(body);
        result.ErrorContentObject.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(ErrorStatusCodes))]
    public async Task BuildStreamingEndpointResponseAsync_EveryErrorStatusWithRegisteredType_DeserializesTheErrorBody(
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode, ProblemJson, "application/problem+json");
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(statusCode);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(statusCode);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().Be(ProblemJson);
        result.ErrorContentObject.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("No item 7");
    }

    [Theory]
    [MemberData(nameof(ErrorStatusCodes))]
    public async Task BuildStreamingEndpointResponseAsync_EveryErrorStatusWithoutRegisteredType_HasOnlyTheText(
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode, ProblemJson, "application/problem+json");
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(statusCode);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().Be(ProblemJson);
        result.ErrorContentObject.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_Error_HasTheResponseAndContentHeaders()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.TooManyRequests, ProblemJson, "application/problem+json");
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.Headers["retry-after"].Should().Equal("30");
        result.Headers["Content-Type"].Should().ContainSingle().Which.Should().StartWith("application/problem+json");
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_Success_HasHeadersAndStillStreamsTheItems()
    {
        // Arrange
        var writeTheLastItem = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = LoopbackServer.Start(async (response, cancellationToken) =>
        {
            await response.WriteHeadAsync(HttpStatusCode.OK, "application/json", ("X-Rate-Limit", "5"));
            await response.WriteAsync("[{\"name\":\"first\"},");
            await writeTheLastItem.Task.WaitAsync(cancellationToken);
            await response.WriteAsync("{\"name\":\"last\"}]");
        });

        using var client = new HttpClient();
        var httpResponse = await client.GetAsync(server.Uri, HttpCompletionOption.ResponseHeadersRead);
        var sut = CreateSut(httpResponse);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorContentObject.Should().BeNull();
        result.Headers["x-rate-limit"].Should().Equal("5");
        result.Headers["content-type"].Should().Equal("application/json");

        await using var items = result.Content!.GetAsyncEnumerator();
        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("first");
        writeTheLastItem.Task.IsCompleted.Should().BeFalse("the first item must arrive before the last is written");

        writeTheLastItem.SetResult();
        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("last");
        (await items.NextAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_WithFactory_ReturnsTheFactoryResultWithTheErrorCarriedOver()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.NotFound, ProblemJson, "application/problem+json");
        response.Headers.TryAddWithoutValidation("X-Rate-Limit", "5");
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);
        DerivedStreamingEndpointResponse<SuccessResponse>? created = null;

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            x => created = new DerivedStreamingEndpointResponse<SuccessResponse>(x));

        // Assert
        result.Should().BeSameAs(created);
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().Be(ProblemJson);
        result.IsNotFound.Should().BeTrue();
        result.NotFoundContent.Title.Should().Be("Not found");
        result.Headers["x-rate-limit"].Should().Equal("5");
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_WithFactory_CarriesTheItemsOver()
    {
        // Arrange
        using var response = CreateResponse(HttpStatusCode.OK, "[{\"name\":\"a\"},{\"name\":\"b\"}]", "application/json");
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            x => new DerivedStreamingEndpointResponse<SuccessResponse>(x));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ErrorContent.Should().BeNull();
        result.ErrorContentObject.Should().BeNull();
        var items = await result.Content!.ToListAsync();
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_WithFactory_TheResultOwnsTheHttpResponse()
    {
        // Arrange
        using var response = new DisposeCountingHttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(ProblemJson),
        };
        var sut = CreateSut(response);
        StreamingEndpointResponse<SuccessResponse>? source = null;

        // Act
        var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            x =>
            {
                source = x;
                return new DerivedStreamingEndpointResponse<SuccessResponse>(x);
            });

        // Assert
        source!.Dispose();
        response.DisposeCount.Should().Be(0, "the source handed the HTTP response over to the result");

        result.Dispose();
        response.DisposeCount.Should().Be(1);

        result.Dispose();
        source.Dispose();
        response.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_WithFactoryThatThrows_DisposesTheHttpResponse()
    {
        // Arrange
        using var response = new DisposeCountingHttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        };
        var sut = CreateSut(response);

        // Act
        var act = () => sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            _ => throw new InvalidOperationException("no result"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("no result");
        response.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_WithFactoryAndNullResponse_ReturnsTheFactoryResult()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            x => new DerivedStreamingEndpointResponse<SuccessResponse>(x));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().BeNull();
        result.ErrorContentObject.Should().BeNull();
        result.Headers.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_WithNullFactory_Throws()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act
        var act = () => sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            factory: null!);

        // Assert
        var exception = await act.Should().ThrowAsync<ArgumentNullException>();
        exception.WithParameterName("factory");
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string body,
        string mediaType)
        => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        };

    private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
        => new(response, serializer);
}