namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderServerSentEventInfoTests
{
    private const string ItemA = "data: {\"name\":\"a\"}\n";
    private const string ItemB = "data: {\"name\":\"b\"}\n";
    private const string ItemC = "data: {\"name\":\"c\"}\n";

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    public static TheoryData<StreamFraming> OtherFramings
    {
        get
        {
            var data = new TheoryData<StreamFraming>();
            foreach (var framing in Enum.GetValues<StreamFraming>().Where(x => x != StreamFraming.ServerSentEvents))
            {
                data.Add(framing);
            }

            return data;
        }
    }

    [Fact]
    public async Task LastEventId_IsTheIdOfTheLastEventReadWhenAnItemIsYielded()
    {
        // Act
        var ids = await ReadLastEventIdsAsync(
            "id: 1\n" + ItemA + "\n" +
            "id: 2\n" + ItemB + "\n" +
            ItemC + "\n");

        // Assert
        ids.Should().Equal("1", "2", "2");
    }

    [Fact]
    public async Task LastEventId_BeforeAnEventIsRead_IsNull()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, "id: 1\n" + ItemA + "\n");
        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.ServerSentEvents);

        // Assert
        result.LastEventId.Should().BeNull();
    }

    [Theory]
    [InlineData("event: ping\nid: 9\ndata: {}\n\n")]
    [InlineData("event: ping\nid: 9\n\n")]
    [InlineData("id: 9\n\n")]
    [InlineData("id:9\r\n\r\n")]
    public async Task LastEventId_IsAdvancedByAnEventThatIsNoItem(
        string heartbeat)
    {
        // Act
        var ids = await ReadLastEventIdsAsync("id: 1\n" + ItemA + "\n" + heartbeat + ItemB + "\n");

        // Assert
        ids.Should().Equal("1", "9");
    }

    [Theory]
    [InlineData("id:\n")]
    [InlineData("id: \n")]
    [InlineData("id\n")]
    public async Task LastEventId_AnIdWithoutAValue_ResetsItToEmpty(string id)
    {
        // Act
        var ids = await ReadLastEventIdsAsync("id: 1\n" + ItemA + "\n" + id + ItemB + "\n");

        // Assert
        ids.Should().Equal("1", string.Empty);
    }

    [Fact]
    public async Task LastEventId_AnIdWithANul_IsIgnored()
    {
        // Act
        var ids = await ReadLastEventIdsAsync("id: 1\n" + ItemA + "\n" + "id: 2\u00003\n" + ItemB + "\n");

        // Assert
        ids.Should().Equal("1", "1");
    }

    [Fact]
    public async Task LastEventId_OfAnEventThatTheStreamEndsIn_IsNotTaken()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, "id: 1\n" + ItemA + "\n" + "id: 2\n" + ItemB);
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.ServerSentEvents);

        // Act
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().ContainSingle();
        result.LastEventId.Should().Be("1");
    }

    [Fact]
    public async Task LastEventId_OfAResultMadeByTheFactory_FollowsTheStream()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, "id: 1\n" + ItemA + "\n" + "id: 2\n" + ItemB + "\n");
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedResult>(
            x => new DerivedResult(x),
            StreamFraming.ServerSentEvents);

        // Act
        var ids = new List<string?>();
        await foreach (var item in result.Content!)
        {
            ids.Add(result.LastEventId);
        }

        // Assert
        ids.Should().Equal("1", "2");
    }

    [Theory]
    [MemberData(nameof(OtherFramings))]
    public async Task OtherFramings_HaveNoLastEventIdAndNoEvents(
        StreamFraming framing)
    {
        // Arrange
        var events = new List<ServerSentEventInfo>();
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "{\"name\":\"a\"}", "{\"name\":\"b\"}"));
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(
            framing,
            new StreamReadOptions { OnEvent = events.Add });

        // Act
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().HaveCount(2);
        result.LastEventId.Should().BeNull();
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task OnEvent_IsCalledForEveryItemBeforeItIsYielded()
    {
        // Arrange
        var log = new List<string>();
        const string wire =
            "id: 1\n" + ItemA + "\n" +
            "event: ping\nid: 2\ndata: {}\n\n" +
            ": comment\n\n" +
            "event: order\n" + ItemB + "\n";
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, wire);
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            new StreamReadOptions
            {
                ItemEventNames = ["order"],
                OnEvent = x => log.Add($"event {x.EventName ?? "(unnamed)"} id {x.LastEventId}"),
            });

        // Act
        await foreach (var item in result.Content!)
        {
            log.Add($"item {item!.Name}");
        }

        // Assert
        log.Should().Equal(
            "event (unnamed) id 1",
            "item a",
            "event order id 2",
            "item b");
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("event:\n", null)]
    [InlineData("event: message\n", "message")]
    [InlineData("event: order\n", "order")]
    public async Task OnEvent_HasTheNameOfTheEvent(
        string eventLine,
        string? expected)
    {
        // Act
        var events = await ReadEventsAsync(eventLine + ItemA + "\n");

        // Assert
        events.Should().ContainSingle().Which.EventName.Should().Be(expected);
    }

    [Fact]
    public async Task OnEvent_WithoutAnIdOrARetry_HasNeither()
    {
        // Act
        var events = await ReadEventsAsync(ItemA + "\n");

        // Assert
        events.Should().ContainSingle().Which.Should().Be(default(ServerSentEventInfo));
    }

    [Fact]
    public async Task OnEvent_HasTheRetryTimeTheServerLastAskedFor()
    {
        // Act
        var events = await ReadEventsAsync(
            ItemA + "\n" +
            "retry: 1500\n" + ItemB + "\n" +
            ItemC + "\n" +
            "retry: 20\n\n" + ItemA + "\n");

        // Assert
        events.Select(x => x.Retry).Should().Equal(
            null,
            TimeSpan.FromMilliseconds(1500),
            TimeSpan.FromMilliseconds(1500),
            TimeSpan.FromMilliseconds(20));
    }

    [Theory]
    [InlineData("retry: soon\n")]
    [InlineData("retry: 1.5\n")]
    [InlineData("retry: -1\n")]
    [InlineData("retry: +1\n")]
    [InlineData("retry:  1\n")]
    [InlineData("retry:\n")]
    [InlineData("retry\n")]
    [InlineData("retry: 99999999999999999999999999\n")]
    public async Task OnEvent_ARetryThatIsNoNumberOfMilliseconds_IsIgnored(
        string retry)
    {
        // Act
        var events = await ReadEventsAsync("retry: 1500\n" + ItemA + "\n" + retry + ItemB + "\n");

        // Assert
        events.Select(x => x.Retry).Should().Equal(TimeSpan.FromMilliseconds(1500), TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public async Task OnEvent_ThatThrows_FailsTheEnumerationWithItsException()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, ItemA + "\n" + ItemB + "\n");
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            new StreamReadOptions { OnEvent = _ => throw new InvalidOperationException("The caller's own.") });

        // Act
        var act = async () => await result.Content!.ToListAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("The caller's own.");
    }

    private async Task<List<string?>> ReadLastEventIdsAsync(string wire)
    {
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, wire);
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.ServerSentEvents);

        var ids = new List<string?>();
        await foreach (var item in result.Content!)
        {
            ids.Add(result.LastEventId);
        }

        return ids;
    }

    private async Task<List<ServerSentEventInfo>> ReadEventsAsync(string wire)
    {
        var events = new List<ServerSentEventInfo>();
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, wire);
        var sut = new MessageResponseBuilder(response, serializer);
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            new StreamReadOptions { ItemEventNames = ["order"], OnEvent = events.Add });

        await result.Content!.ToListAsync();

        return events;
    }

    private sealed class DerivedResult : StreamingEndpointResponse<SuccessResponse>
    {
        public DerivedResult(
            StreamingEndpointResponse<SuccessResponse> response)
            : base(response)
        {
        }
    }
}