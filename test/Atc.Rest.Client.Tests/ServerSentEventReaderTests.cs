namespace Atc.Rest.Client.Tests;

public sealed class ServerSentEventReaderTests
{
    private const string ItemA = "data: {\"name\":\"a\"}\n";
    private const string ItemB = "data: {\"name\":\"b\"}\n";
    private const string ItemC = "data: {\"name\":\"c\"}\n";

    // Ids 1, none, 3
    private const string Wire =
        "id: 1\n" + ItemA + "\n" +
        ItemB + "\n" +
        "id: 3\n" + ItemC + "\n";

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    [Theory]
    [InlineData("builder")]
    [InlineData("reader")]
    public async Task LastEventId_FollowsTheEnumeration(string way)
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, Wire);
        using var result = await BuildAsync(way, response);

        // Act
        var ids = new List<string?>();
        await foreach (var item in result.Content!)
        {
            ids.Add(result.LastEventId);
        }

        // Assert
        ids.Should().Equal("1", "1", "3");
        result.LastEventId.Should().Be("3");
    }

    [Theory]
    [InlineData("builder")]
    [InlineData("reader")]
    public async Task LastEventId_BeforeAnEventIsRead_IsNull(string way)
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, Wire);

        // Act
        using var result = await BuildAsync(way, response);

        // Assert
        result.LastEventId.Should().BeNull();
    }

    [Fact]
    public async Task LastEventId_OfAResultThatTakesTheResponseOver_FollowsTheStream()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, Wire);
        using var result = new DerivedStreamingEndpointResponse<SuccessResponse>(await BuildAsync("reader", response));

        // Act
        var ids = new List<string?>();
        await foreach (var item in result.Content!)
        {
            ids.Add(result.LastEventId);
        }

        // Assert
        ids.Should().Equal("1", "1", "3");
        result.LastEventId.Should().Be("3");
    }

    [Fact]
    public async Task LastEventId_OfAResponseBuiltWithAPublicConstructor_IsNull()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, Wire);
        using var built = await BuildAsync("reader", response);
        using var sut = new StreamingEndpointResponse<SuccessResponse>(
            isSuccess: true,
            HttpStatusCode.OK,
            built.Content,
            errorContent: null,
            httpResponse: null);

        // Act
        var items = await sut.Content!.ToListAsync();

        // Assert
        items.Should().HaveCount(3);
        sut.LastEventId.Should().BeNull();
    }

    [Fact]
    public async Task Read_YieldsTheItemsTheDeserializerMakesOfTheDataOfAnEvent()
    {
        // Arrange
        var data = new List<string>();
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, "data: {\"name\":\n" + "data: \"æ\"}\n\n" + ItemB + "\n");
        var stream = await response.Content.ReadAsStreamAsync();
        using var sut = ServerSentEventReader.Read(
            response,
            stream,
            json =>
            {
                data.Add(json);
                return serializer.Deserialize<SuccessResponse>(json);
            });

        // Act
        var items = await sut.Content!.ToListAsync();

        // Assert
        data.Should().Equal("{\"name\":\n\"æ\"}", "{\"name\":\"b\"}");
        items.Select(x => x!.Name).Should().Equal("æ", "b");
    }

    [Fact]
    public async Task Read_HasTheStatusAndTheHeadersOfTheResponse()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, ItemA + "\n");
        response.StatusCode = HttpStatusCode.Accepted;
        response.Headers.TryAddWithoutValidation("X-Request-Id", "42");
        var stream = await response.Content.ReadAsStreamAsync();

        // Act
        using var sut = ServerSentEventReader.Read(response, stream, Deserialize);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.Accepted);
        sut.ErrorContent.Should().BeNull();
        sut.Headers["x-request-id"].Should().Equal("42");
        sut.Headers["content-type"].Should().Equal("text/event-stream");
    }

    [Fact]
    public async Task Read_TheResponseReturnedOwnsTheHttpResponse()
    {
        // Arrange
        using var response = new DisposeCountingHttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(FeedStream.FromText(ItemA + "\n")),
        };
        var stream = await response.Content.ReadAsStreamAsync();
        var sut = ServerSentEventReader.Read(response, stream, Deserialize);

        // Act
        sut.Dispose();

        // Assert
        response.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task Read_WithOptions_ReadsTheNamedItemEventsAndCallsOnEvent()
    {
        // Arrange
        const string wire =
            "event: ping\nid: 1\ndata: {}\n\n" +
            "event: order\nretry: 1500\n" + ItemA + "\n";
        var events = new List<ServerSentEventInfo>();
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, wire);
        var stream = await response.Content.ReadAsStreamAsync();
        using var sut = ServerSentEventReader.Read(
            response,
            stream,
            Deserialize,
            new StreamReadOptions { ItemEventNames = ["order"], OnEvent = events.Add });

        // Act
        var items = await sut.Content!.ToListAsync();

        // Assert
        items.Should().ContainSingle().Which!.Name.Should().Be("a");
        events.Should().ContainSingle().Which.Should().Be(new ServerSentEventInfo
        {
            LastEventId = "1",
            EventName = "order",
            Retry = TimeSpan.FromMilliseconds(1500),
        });
    }

    [Fact]
    public async Task Read_AnEventLongerThanMaxRecordLength_FailsTheEnumeration()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, ItemA + "\n");
        var stream = await response.Content.ReadAsStreamAsync();
        using var sut = ServerSentEventReader.Read(
            response,
            stream,
            Deserialize,
            new StreamReadOptions { MaxRecordLength = 8 });

        // Act
        var act = async () => await sut.Content!.ToListAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Read_WithAMaxRecordLengthThatIsNotPositive_Throws()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, ItemA + "\n");
        var stream = await response.Content.ReadAsStreamAsync();

        // Act
        var act = () => ServerSentEventReader.Read(response, stream, Deserialize, new StreamReadOptions { MaxRecordLength = 0 });

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Read_CancelledWhileEnumerating_Throws()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, ItemA + "\n" + ItemB + "\n");
        var stream = await response.Content.ReadAsStreamAsync();
        using var sut = ServerSentEventReader.Read(response, stream, Deserialize, options: null, cancellation.Token);

        // Act
        var act = async () =>
        {
            await foreach (var item in sut.Content!)
            {
                await cancellation.CancelAsync();
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Read_WithoutAResponse_Throws()
    {
        // Act
        var act = () => ServerSentEventReader.Read(response: null!, Stream.Null, Deserialize);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("response");
    }

    [Fact]
    public void Read_WithoutAStream_Throws()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var act = () => ServerSentEventReader.Read(response, stream: null!, Deserialize);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void Read_WithoutADeserializer_Throws()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var act = () => ServerSentEventReader.Read<SuccessResponse>(response, Stream.Null, deserialize: null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("deserialize");
    }

    private SuccessResponse? Deserialize(string json)
        => serializer.Deserialize<SuccessResponse>(json);

    // The same stream through the builder and through the reader that is handed out
    private async Task<StreamingEndpointResponse<SuccessResponse>> BuildAsync(
        string way,
        HttpResponseMessage response)
    {
        if (way == "builder")
        {
            return await new MessageResponseBuilder(response, serializer)
                .BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.ServerSentEvents);
        }

        var stream = await response.Content.ReadAsStreamAsync();
        return ServerSentEventReader.Read(response, stream, Deserialize);
    }
}