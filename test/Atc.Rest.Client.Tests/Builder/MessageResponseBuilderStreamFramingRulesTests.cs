namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderStreamFramingRulesTests
{
    private const string RecordSeparator = "\u001e";

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    [Theory]
    [InlineData("{\"name\":\"a\"}\n{\"name\":\"b\"}\n")]
    [InlineData("{\"name\":\"a\"}\n{\"name\":\"b\"}")]
    [InlineData("{\"name\":\"a\"}\r\n{\"name\":\"b\"}\r\n")]
    [InlineData("\n{\"name\":\"a\"}\n\n\n{\"name\":\"b\"}\n\n")]
    [InlineData("{\"name\":\"a\"}\n  \t\r\n{\"name\":\"b\"}\n ")]
    [InlineData("﻿{\"name\":\"a\"}\n{\"name\":\"b\"}\n")]
    public async Task JsonLines_ReadsOneRecordPerLine(string wire)
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(StreamFraming.JsonLines, wire);

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Fact]
    public async Task JsonLines_LineThatIsTooLong_NamesTheLimit()
    {
        // Act
        var act = () => ReadAsync<string>(StreamFraming.JsonLines, "\"aaaaaaaaaaaaaaaaaaaa\"\n", maxRecordLength: 16);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*16 bytes*");
    }

    [Theory]
    [InlineData(RecordSeparator + "{\"name\":\"a\"}\n" + RecordSeparator + "{\"name\":\"b\"}\n")]
    [InlineData(RecordSeparator + "{\"name\":\"a\"}\n" + RecordSeparator + "{\"name\":\"b\"}")]
    [InlineData(RecordSeparator + "{\"name\":\"a\"}" + RecordSeparator + "{\"name\":\"b\"}")]
    [InlineData(RecordSeparator + "{\n  \"name\": \"a\"\n}\n" + RecordSeparator + "{\n  \"name\": \"b\"\n}\n")]
    [InlineData(RecordSeparator + "{\"name\":\"a\"}\r\n" + RecordSeparator + "{\"name\":\"b\"}\r\n")]
    [InlineData(RecordSeparator + "{\"name\":\"a\"}\n\n" + RecordSeparator + RecordSeparator + "{\"name\":\"b\"}\n\n")]
    public async Task JsonSequence_ReadsOneRecordPerSeparator(string wire)
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(StreamFraming.JsonSequence, wire);

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Fact]
    public async Task JsonSequence_YieldsARecordAtItsLineFeedAndNotAtTheNextSeparator()
    {
        // Arrange
        using var body = new FeedStream();
        body.Feed(RecordSeparator + "{\"name\":\"a\"}\n");
        using var response = StreamWire.Response(StreamFraming.JsonSequence, body);
        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.JsonSequence);
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue("a server that fails after a record never sends a separator");
        items.Current!.Name.Should().Be("a");
    }

    [Fact]
    public Task JsonSequence_RecordCutOffAtTheEndOfTheStream_Throws()
    {
        // Act
        var act = () => ReadAsync<SuccessResponse>(
            StreamFraming.JsonSequence,
            RecordSeparator + "{\"name\":\"a\"}\n" + RecordSeparator + "{\"name\":\"b");

        // Assert
        return act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task JsonSequence_NumbersAreReadAtTheirLineFeed()
    {
        // Act
        var items = await ReadAsync<int>(
            StreamFraming.JsonSequence,
            RecordSeparator + "1\n" + RecordSeparator + "23\n" + RecordSeparator + "456");

        // Assert
        items.Should().Equal(1, 23, 456);
    }

    [Theory]
    [InlineData("data: {\"name\":\"a\"}\n\ndata: {\"name\":\"b\"}\n\n")]
    [InlineData("data: {\"name\":\"a\"}\r\n\r\ndata: {\"name\":\"b\"}\r\n\r\n")]
    [InlineData("data: {\"name\":\"a\"}\r\rdata: {\"name\":\"b\"}\r\r")]
    [InlineData("data:{\"name\":\"a\"}\n\ndata:{\"name\":\"b\"}\n\n")]
    [InlineData("data: {\ndata:   \"name\": \"a\"\ndata: }\n\ndata: {\"name\":\"b\"}\n\n")]
    [InlineData(": keep-alive\n\nid: 1\nretry: 500\ndata: {\"name\":\"a\"}\n\n: keep-alive\n\ndata: {\"name\":\"b\"}\n\n")]
    [InlineData("data: {\"name\":\"a\"}\n\ndata:\n\nevent: ping\ndata: ping\n\nevent: ping\n\ndata: {\"name\":\"b\"}\n\n")]
    [InlineData("event: message\ndata: {\"name\":\"a\"}\n\nevent: message\ndata: {\"name\":\"b\"}\n\n")]

    // The event name is forgotten at every blank line, also when the event had no data: the event after a heartbeat
    // without data is an unnamed event again. An event field without a value names no event.
    [InlineData("event: ping\n\ndata: {\"name\":\"a\"}\n\nevent: ping\nid: 5\n\ndata: {\"name\":\"b\"}\n\n")]
    [InlineData("event: ping\r\n\r\ndata: {\"name\":\"a\"}\r\n\r\nevent: ping\r\rdata: {\"name\":\"b\"}\r\r")]
    [InlineData("event\n\ndata: {\"name\":\"a\"}\n\nevent:\ndata: {\"name\":\"b\"}\n\n")]
    public async Task ServerSentEvents_ReadsTheDataOfTheEvents(string wire)
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(StreamFraming.ServerSentEvents, wire);

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Theory]
    [InlineData("data: {\"name\":\"a\"}\r\n\r\ndata: {\"name\":\"b\"}\r\n\r\n")]
    [InlineData("data: {\"name\":\"a\"}\r\rdata: {\"name\":\"b\"}\r\r")]
    [InlineData("﻿data: {\"name\":\"a\"}\n\ndata: {\"name\":\"b\"}\n\n")]
    public async Task ServerSentEvents_ArrivingByteByByte_ReadsEveryLineEnding(
        string wire)
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(wire);
        using var response = CreateResponse(
            FeedStream.From([.. bytes.Select(x => new[] { x })]),
            StreamWire.ContentType(StreamFraming.ServerSentEvents));
        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.ServerSentEvents);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    // A line that ends with a carriage return is complete: the item is not held back until the next byte shows
    // whether a line feed follows.
    [Fact]
    public async Task ServerSentEvents_EventEndedByCarriageReturns_IsYieldedAtOnce()
    {
        // Arrange
        using var body = new FeedStream();
        body.Feed("data: {\"name\":\"a\"}\r\r");
        using var response = CreateResponse(body, StreamWire.ContentType(StreamFraming.ServerSentEvents));
        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.ServerSentEvents);
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("a");
    }

    [Fact]
    public async Task ServerSentEvents_NamedEventWithValidJson_IsNoItem()
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            "event: ping\ndata: {\"t\":1}\n\ndata: {\"name\":\"a\"}\n\nevent: done\ndata: {\"name\":\"no\"}\n\n");

        // Assert
        items.Should().ContainSingle().Which!.Name.Should().Be("a");
    }

    [Fact]
    public async Task ServerSentEvents_WithItemEventNames_ReadsTheNamedAndTheUnnamedEvents()
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            "event: order\ndata: {\"name\":\"a\"}\n\nevent: ping\ndata: {\"name\":\"no\"}\n\ndata: {\"name\":\"b\"}\n\nevent: invoice\ndata: {\"name\":\"c\"}\n\n",
            itemEventNames: ["order", "invoice"]);

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task ServerSentEvents_ItemEventNames_AreCaseSensitive()
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            "event: Order\ndata: {\"name\":\"no\"}\n\nevent: order\ndata: {\"name\":\"a\"}\n\n",
            itemEventNames: ["order"]);

        // Assert
        items.Should().ContainSingle().Which!.Name.Should().Be("a");
    }

    [Fact]
    public async Task ServerSentEvents_ListedEventThatIsNoJson_Throws()
    {
        // Act
        var act = () => ReadAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            "event: order\ndata: not json\n\n",
            itemEventNames: ["order"]);

        // Assert
        var exception = await act.Should().ThrowAsync<JsonException>();
        exception.Which.Should().NotBeNull();
    }

    [Fact]
    public async Task ServerSentEvents_UnlistedEventThatIsNoJson_IsSkipped()
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            "event: ping\ndata: not json\n\ndata: {\"name\":\"a\"}\n\n",
            itemEventNames: ["order"]);

        // Assert
        items.Should().ContainSingle().Which!.Name.Should().Be("a");
    }

    [Fact]
    public async Task ServerSentEvents_NamedEventPastTheLimit_Throws()
    {
        // Act
        var act = () => ReadAsync<SuccessResponse>(
            StreamFraming.ServerSentEvents,
            "event: ping\ndata: a heartbeat that is far too long\n\n",
            maxRecordLength: 16);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*16 bytes*");
    }

    [Fact]
    public Task ServerSentEvents_UnnamedEventThatIsNoJson_Throws()
    {
        // Act
        var act = () => ReadAsync<SuccessResponse>(StreamFraming.ServerSentEvents, "data: not json\n\n");

        // Assert
        return act.Should().ThrowAsync<JsonException>();
    }

    [Theory]
    [InlineData("data: [1,\ndata: 2,\ndata: 3,\ndata: 4,\ndata: 5]\n\n")]
    [InlineData("data: [1,\r\ndata: 2,\r\ndata: 3,\r\ndata: 4,\r\ndata: 5,\r\n")]
    [InlineData(": a comment that never ends aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public Task ServerSentEvents_EventOfManyLinesPastTheLimit_Throws(
        string wire)
    {
        // Act
        var act = () => ReadAsync<int[]>(StreamFraming.ServerSentEvents, wire, maxRecordLength: 16);

        // Assert
        return act.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [InlineData("data: 1234567\n\ndata: 1234567\n\ndata: 1234567\n\ndata: 1234567\n\n")]
    [InlineData("data: 1234567\r\n\r\ndata: 1234567\r\n\r\ndata: 1234567\r\n\r\ndata: 1234567\r\n\r\n")]
    [InlineData("data: 1234567\r\rdata: 1234567\r\rdata: 1234567\r\rdata: 1234567\r\r")]
    [InlineData(": keep-alive\n\nid:1\ndata:1234567\n\n: keep-alive\n\ndata: 1234567\n\ndata: 1234567\n\ndata: 1234567\n\n")]
    public async Task ServerSentEvents_EventsWithinTheLimit_AreAllReadFromALongerStream(
        string wire)
    {
        // Act
        var items = await ReadAsync<int>(StreamFraming.ServerSentEvents, wire, maxRecordLength: 16);

        // Assert
        items.Should().Equal(1234567, 1234567, 1234567, 1234567);
    }

    [Theory]
    [InlineData("multipart/mixed; boundary=b")]
    [InlineData("multipart/mixed; boundary=\"b\"")]
    [InlineData("multipart/mixed; charset=utf-8; BOUNDARY=\"b\"")]
    public async Task MultipartMixed_ReadsTheBoundaryFromTheContentType(
        string contentType)
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(
            StreamFraming.MultipartMixed,
            "--b\r\nContent-Type: application/json\r\n\r\n{\"name\":\"a\"}\r\n--b\r\n\r\n{\"name\":\"b\"}\r\n--b--\r\n",
            contentType: contentType);

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Theory]
    [InlineData("--b\r\nContent-Type: application/json\r\n\r\n{\"name\":\"a\"}\r\n--b\r\nContent-Type: application/json\r\n\r\n{\"name\":\"b\"}\r\n--b--\r\n")]
    [InlineData("--b\nContent-Type: application/json\n\n{\"name\":\"a\"}\n--b\nContent-Type: application/json\n\n{\"name\":\"b\"}\n--b--")]
    [InlineData("This is the preamble.\r\n{\"name\":\"no\"}\r\n--b\r\nContent-Type: application/json\r\nContent-ID: <1>\r\n\r\n{\"name\":\"a\"}\r\n--b\r\n\r\n{\"name\":\"b\"}\r\n--b--\r\nThis is the epilogue.\r\n{\"name\":\"no\"}\r\n")]
    [InlineData("--b\r\n\r\n{\n  \"name\": \"a\"\n}\r\n--b\r\n\r\n{\r\n  \"name\": \"b\"\r\n}\r\n--b--\r\n")]
    [InlineData("--b  \r\n\r\n{\"name\":\"a\"}\r\n--b\t\r\n\r\n{\"name\":\"b\"}\r\n--b-- \r\n")]
    [InlineData("--bx\r\n\r\n{\"name\":\"no\"}\r\n--b\r\n\r\n{\"name\":\"a\"}\r\n--b\r\n\r\n{\"name\":\"b\"}\r\n--b--\r\n")]
    [InlineData("--b\r\n\r\n{\"name\":\"a\"}\r\n--b\r\n\r\n{\"name\":\"b\"}\r\n")]
    [InlineData("﻿--b\r\n\r\n{\"name\":\"a\"}\r\n--b\r\n\r\n{\"name\":\"b\"}\r\n--b--\r\n")]
    public async Task MultipartMixed_ReadsOneRecordPerPart(string wire)
    {
        // Act
        var items = await ReadAsync<SuccessResponse>(StreamFraming.MultipartMixed, wire, contentType: "multipart/mixed; boundary=b");

        // Assert
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Theory]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/mixed; boundary=")]
    [InlineData("multipart/mixed; boundary=\"\"")]
    [InlineData("application/json")]
    [InlineData(null)]
    public async Task MultipartMixed_WithoutABoundary_ThrowsFromTheBuilderAndDisposesTheHttpResponse(
        string? contentType)
    {
        // Arrange
        using var response = new DisposeCountingHttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(FeedStream.FromText("--b--")),
        };
        if (contentType is not null)
        {
            response.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        var act = () => sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.MultipartMixed);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*boundary*");
        response.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task MultipartMixed_YieldsAPartWhenItsBodyIsCompleteAndNotAtTheNextBoundary()
    {
        // Arrange
        using var body = new FeedStream();
        body.Feed("--b\r\nContent-Type: application/json\r\n\r\n{\"name\":\"a\"}\r\n");
        using var response = CreateResponse(body, "multipart/mixed; boundary=b");
        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.MultipartMixed);
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue("a server that fails after a part never sends a boundary");
        items.Current!.Name.Should().Be("a");
    }

    [Fact]
    public async Task MultipartMixed_EndsAtTheClosingBoundary()
    {
        // Arrange
        using var body = new FeedStream();
        body.Feed("--b\r\n\r\n{\"name\":\"a\"}\r\n--b--\r\n");
        using var response = CreateResponse(body, "multipart/mixed; boundary=b");
        var sut = new MessageResponseBuilder(response, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(StreamFraming.MultipartMixed);
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue();
        (await items.NextAsync()).Should().BeFalse("the stream is not read past the closing boundary");
    }

    [Theory]
    [InlineData("--b\r\nContent-Type: application/json; a-parameter-that-never-ends-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("--b\r\n\r\n[1,\r\n2,\r\n3,\r\n4,\r\n5,\r\n6,\r\n7,\r\n8,\r\n9,\r\n10]\r\n--b--\r\n")]
    [InlineData("a preamble that never ends aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public Task MultipartMixed_PartOrLinePastTheLimit_Throws(string wire)
    {
        // Act
        var act = () => ReadAsync<int[]>(
            StreamFraming.MultipartMixed,
            wire,
            maxRecordLength: 16,
            contentType: "multipart/mixed; boundary=b");

        // Assert
        return act.Should().ThrowAsync<InvalidDataException>();
    }

    private static HttpResponseMessage CreateResponse(
        Stream body,
        string contentType)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(body),
        };
        response.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        return response;
    }

    private async Task<List<T?>> ReadAsync<T>(
        StreamFraming framing,
        string wire,
        int maxRecordLength = 1024,
        string? contentType = null,
        string[]? itemEventNames = null)
    {
        using var response = CreateResponse(FeedStream.FromText(wire), contentType ?? StreamWire.ContentType(framing));
        var sut = new MessageResponseBuilder(response, serializer, maxRecordLength);

        using var result = await sut.BuildStreamingEndpointResponseAsync<T>(
            framing,
            new StreamReadOptions { ItemEventNames = itemEventNames });
        return await result.Content!.ToListAsync();
    }
}