namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderHeadersTests
{
    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    public static TheoryData<string> ResponseKinds => new()
    {
        "factory",
        "typed-success",
        "typed-success-error",
        "binary",
        "stream-binary",
        "streaming",
        "streaming-factory",
    };

    public static TheoryData<string, HttpStatusCode> ResponseKindsAndStatusCodes
    {
        get
        {
            var data = new TheoryData<string, HttpStatusCode>();
            foreach (var kind in ResponseKinds)
            {
                data.Add(kind, HttpStatusCode.OK);
                data.Add(kind, HttpStatusCode.BadRequest);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ResponseKindsAndStatusCodes))]
    public async Task Headers_ResponseHeader_IsFoundInAnyCasing(
        string kind,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode);
        response.Headers.ETag = new EntityTagHeaderValue("\"v1\"");

        // Act
        var headers = await BuildHeadersAsync(kind, response);

        // Assert
        foreach (var name in new[] { "ETag", "etag", "ETAG" })
        {
            headers.TryGetValue(name, out var values).Should().BeTrue($"'{name}' is the same header name");
            values.Should().Equal("\"v1\"");
            headers.ContainsKey(name).Should().BeTrue();
            headers[name].Should().Equal("\"v1\"");
        }
    }

    [Theory]
    [MemberData(nameof(ResponseKindsAndStatusCodes))]
    public async Task Headers_HeaderAddedInLowerCase_IsFoundInItsCanonicalCasing(
        string kind,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode);
        response.Headers.TryAddWithoutValidation("x-rate-limit", "5");

        // Act
        var headers = await BuildHeadersAsync(kind, response);

        // Assert
        headers.TryGetValue("X-Rate-Limit", out var values).Should().BeTrue();
        values.Should().Equal("5");
    }

    [Theory]
    [MemberData(nameof(ResponseKindsAndStatusCodes))]
    public async Task Headers_ContentHeaders_AreFoundInAnyCasing(
        string kind,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = "report.json",
        };

        // Act
        var headers = await BuildHeadersAsync(kind, response);

        // Assert
        foreach (var name in new[] { "Content-Type", "content-type", "CONTENT-TYPE" })
        {
            headers.TryGetValue(name, out var values).Should().BeTrue($"'{name}' is the same header name");
            values.Should().ContainSingle().Which.Should().StartWith("application/json");
        }

        foreach (var name in new[] { "Content-Disposition", "content-disposition" })
        {
            headers.ContainsKey(name).Should().BeTrue($"'{name}' is the same header name");
        }
    }

    [Theory]
    [MemberData(nameof(ResponseKindsAndStatusCodes))]
    public async Task Headers_HeaderWithSeveralValues_KeepsAllOfThemInOrder(
        string kind,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = CreateResponse(statusCode);
        response.Headers.TryAddWithoutValidation("X-Trace", ["first", "second", "third"]);

        // Act
        var headers = await BuildHeadersAsync(kind, response);

        // Assert
        headers["x-trace"].Should().Equal("first", "second", "third");
    }

    [Theory]
    [MemberData(nameof(ResponseKinds))]
    public async Task Headers_NullResponse_IsEmptyAndLooksUpWithoutRegardToCasing(
        string kind)
    {
        // Act
        var headers = await BuildHeadersAsync(kind, response: null);

        // Assert
        headers.Should().BeEmpty();
        GetKeyComparer(headers).Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Headers_BinaryResponseCreatedWithoutHeaders_LooksUpWithoutRegardToCasing()
    {
        // Act
        var sut = new BinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            content: [],
            contentType: null,
            fileName: null,
            contentLength: null);

        // Assert
        GetKeyComparer(sut.Headers).Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Headers_StreamBinaryResponseCreatedWithoutHeaders_LooksUpWithoutRegardToCasing()
    {
        // Act
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            contentStream: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Assert
        GetKeyComparer(sut.Headers).Should().BeSameAs(StringComparer.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode)
        => new(statusCode)
        {
            Content = new StringContent("{\"name\":\"a\"}", System.Text.Encoding.UTF8, "application/json"),
        };

    private static IEqualityComparer<string> GetKeyComparer(
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
        => headers.Should().BeOfType<Dictionary<string, IEnumerable<string>>>().Subject.Comparer;

    private async Task<IReadOnlyDictionary<string, IEnumerable<string>>> BuildHeadersAsync(
        string kind,
        HttpResponseMessage? response)
    {
        var sut = new MessageResponseBuilder(response, serializer);
        sut.AddSuccessResponse<SuccessResponse>(HttpStatusCode.OK);
        sut.AddErrorResponse<BadResponse>(HttpStatusCode.BadRequest);

        switch (kind)
        {
            case "factory":
                return await sut.BuildResponseAsync(r => r.Headers, CancellationToken.None);
            case "typed-success":
                return (await sut.BuildResponseAsync<SuccessResponse>(CancellationToken.None)).Headers;
            case "typed-success-error":
                return (await sut.BuildResponseAsync<SuccessResponse, BadResponse>(CancellationToken.None)).Headers;
            case "binary":
                return (await sut.BuildBinaryResponseAsync(CancellationToken.None)).Headers;
            case "stream-binary":
                using (var streamBinary = await sut.BuildStreamBinaryResponseAsync(CancellationToken.None))
                {
                    return streamBinary.Headers;
                }

            case "streaming":
                using (var streaming = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>())
                {
                    return streaming.Headers;
                }

            case "streaming-factory":
                using (var streaming = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
                    x => new DerivedStreamingEndpointResponse<SuccessResponse>(x)))
                {
                    return streaming.Headers;
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown response kind.");
        }
    }
}