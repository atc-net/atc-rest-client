namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderStreamFramingTests
{
    private const int Limit = 64;
    private const int ReadBufferSize = 4096;
    private const string ProblemJson = "{\"title\":\"Not found\",\"status\":404,\"detail\":\"No item 7\"}";

    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    public static TheoryData<StreamFraming> Framings => StreamWire.Framings;

    public static TheoryData<StreamFraming, HttpStatusCode> FramingsAndErrorStatusCodes
    {
        get
        {
            var data = new TheoryData<StreamFraming, HttpStatusCode>();
            foreach (var framing in Enum.GetValues<StreamFraming>())
            {
                foreach (var statusCode in Enum.GetValues<HttpStatusCode>().Distinct().Where(x => (int)x >= 400))
                {
                    data.Add(framing, statusCode);
                }
            }

            return data;
        }
    }

    public static TheoryData<StreamFraming, HttpStatusCode, string> FramingsAndBodiesWithoutContent
    {
        get
        {
            var data = new TheoryData<StreamFraming, HttpStatusCode, string>();
            foreach (var framing in Enum.GetValues<StreamFraming>())
            {
                foreach (var statusCode in new[] { HttpStatusCode.OK, HttpStatusCode.NoContent })
                {
                    data.Add(framing, statusCode, string.Empty);
                    data.Add(framing, statusCode, " \r\n \n\t");
                }
            }

            return data;
        }
    }

    public static TheoryData<StreamFraming, int> FramingsAndLargestLimits
    {
        get
        {
            var data = new TheoryData<StreamFraming, int>();
            foreach (var framing in Enum.GetValues<StreamFraming>())
            {
                data.Add(framing, int.MaxValue);
                data.Add(framing, int.MaxValue - 1);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Items_ArriveInOrder_EachBeforeTheNextIsWritten(
        StreamFraming framing)
    {
        // Arrange
        var writeSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeThird = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = LoopbackServer.Start(async (response, cancellationToken) =>
        {
            await response.WriteHeadAsync(HttpStatusCode.OK, StreamWire.ContentType(framing));
            await response.WriteAsync(StreamWire.Open(framing) + StreamWire.Record(framing, "{\"name\":\"first\"}"));
            await writeSecond.Task.WaitAsync(cancellationToken);
            await response.WriteAsync(StreamWire.Record(framing, "{\"name\":\"second\"}"));
            await writeThird.Task.WaitAsync(cancellationToken);
            await response.WriteAsync(StreamWire.Record(framing, "{\"name\":\"third\"}", isLast: true) + StreamWire.Close(framing));
        });

        using var client = new HttpClient();
        var httpResponse = await client.GetAsync(server.Uri, HttpCompletionOption.ResponseHeadersRead);
        var sut = CreateSut(httpResponse);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await using var items = result.Content!.GetAsyncEnumerator();

        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("first");
        writeSecond.Task.IsCompleted.Should().BeFalse("the first item must arrive before the second is written");

        writeSecond.SetResult();
        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("second");
        writeThird.Task.IsCompleted.Should().BeFalse("the second item must arrive before the third is written");

        writeThird.SetResult();
        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("third");
        (await items.NextAsync()).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Items_WithoutAFraming_AreReadAsAJsonArray(
        StreamFraming framing)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(StreamFraming.JsonArray, "1", "2"));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>();

        // Assert
        (await result.Content!.ToListAsync()).Should().Equal(1, 2);
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_OfExactlyTheLimit_IsRead(StreamFraming framing)
    {
        // Arrange
        var record = JsonStringOfLength(Limit - StreamWire.RecordOverhead(framing));
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, record, record, record));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().HaveCount(3).And.OnlyContain(x => x!.Length == record.Length - 2);
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_OneByteLongerThanTheLimit_ThrowsAfterTheItemsBeforeIt(
        StreamFraming framing)
    {
        // Arrange
        var record = JsonStringOfLength(Limit - StreamWire.RecordOverhead(framing));
        var tooLong = JsonStringOfLength(Limit - StreamWire.RecordOverhead(framing) + 1);
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, record, tooLong, record));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>(framing);
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue();
        items.Current.Should().HaveLength(record.Length - 2);

        var act = () => items.NextAsync();
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*64 bytes*MaxStreamRecordLength*");
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_ThatNeverEnds_ThrowsBeforeItIsBuffered(
        StreamFraming framing)
    {
        // Arrange
        var prefix = StreamWire.OpenRecord(framing);
        using var body = new EndlessStream(prefix, 'a');
        using var response = StreamWire.Response(framing, body);
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>(framing);
        var act = () => result.Content!.ToListAsync().AsTask();

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>();
        body.BytesRead.Should().BeLessThanOrEqualTo(
            prefix.Length + Limit + ReadBufferSize,
            "no more than the limit and one read buffer may have been taken from the peer");
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_SplitAcrossTwoReadsAtEveryPosition_IsRead(
        StreamFraming framing)
    {
        // Arrange
        var wire = Encoding.UTF8.GetBytes(StreamWire.Document(
            framing,
            "{\"name\":\"æ€😀 one\"}",
            "{\"name\":\"two æ€😀\"}"));

        for (var position = 1; position < wire.Length; position++)
        {
            using var response = StreamWire.Response(framing, FeedStream.From(wire[..position], wire[position..]));
            var sut = CreateSut(response, maxStreamRecordLength: 1024);

            // Act
            using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
            var items = await result.Content!.ToListAsync();

            // Assert
            items.Select(x => x!.Name).Should().Equal(
                ["æ€😀 one", "two æ€😀"],
                $"the split is at byte {position}");
        }
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_ArrivingByteByByte_IsRead(StreamFraming framing)
    {
        // Arrange
        var wire = Encoding.UTF8.GetBytes(StreamWire.Document(
            framing,
            "{\"name\":\"æ€😀 one\"}",
            "{\"name\":\"two æ€😀\"}"));
        using var response = StreamWire.Response(framing, FeedStream.From([.. wire.Select(x => new[] { x })]));
        var sut = CreateSut(response, maxStreamRecordLength: 1024);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Select(x => x!.Name).Should().Equal("æ€😀 one", "two æ€😀");
    }

    [Theory]
    [MemberData(nameof(FramingsAndErrorStatusCodes))]
    public async Task ErrorStatus_HasTheTextTheTypedErrorAndTheHeaders(
        StreamFraming framing,
        HttpStatusCode statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(ProblemJson, Encoding.UTF8, "application/problem+json"),
        };
        response.Headers.TryAddWithoutValidation("x-rate-limit", "5");
        var sut = CreateSut(response);
        sut.AddErrorResponse<ProblemDetails>(statusCode);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(statusCode);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().Be(ProblemJson);
        result.ErrorContentObject.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("No item 7");
        result.Headers["X-Rate-Limit"].Should().Equal("5");
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Success_HasTheHeaders(StreamFraming framing)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "1"));
        response.Headers.TryAddWithoutValidation("x-rate-limit", "5");
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(framing);

        // Assert
        result.Headers["X-Rate-Limit"].Should().Equal("5");
        result.Headers["content-type"].Should().Equal(StreamWire.ContentType(framing));
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task DisposingTheResponseDuringTheEnumeration_EndsItAndReleasesTheConnection(
        StreamFraming framing)
    {
        // Arrange
        await using var server = LoopbackServer.Start(async (response, cancellationToken) =>
        {
            await response.WriteHeadAsync(HttpStatusCode.OK, StreamWire.ContentType(framing));
            await response.WriteAsync(StreamWire.Open(framing) + StreamWire.Record(framing, "{\"name\":\"first\"}"));
            await response.WaitForDisconnectAsync(cancellationToken);
        });

        using var client = new HttpClient();
        var httpResponse = await client.GetAsync(server.Uri, HttpCompletionOption.ResponseHeadersRead);
        var sut = CreateSut(httpResponse);

        var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        var items = result.Content!.GetAsyncEnumerator();
        (await items.NextAsync()).Should().BeTrue();
        var waitingForTheSecondItem = items.NextAsync();

        // Act
        result.Dispose();

        // Assert
        var ended = await Record.ExceptionAsync(async () => await waitingForTheSecondItem);

        // It ends with the exception of the aborted read, or without one when the read saw the end of the stream first
        (ended is TimeoutException).Should().BeFalse("the enumeration must end when the response is disposed");
        await server.Completion.WaitAsync(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Cancellation_IsObservedBetweenRecords(
        StreamFraming framing)
    {
        // Arrange
        using var body = new FeedStream();
        body.Feed(StreamWire.Open(framing) + StreamWire.Record(framing, "1"));
        using var response = StreamWire.Response(framing, body);
        using var cancellation = new CancellationTokenSource();
        var sut = CreateSut(response);

        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(framing, cancellation.Token);
        await using var items = result.Content!.GetAsyncEnumerator();
        (await items.NextAsync()).Should().BeTrue();

        // Act
        await cancellation.CancelAsync();
        var act = () => items.NextAsync();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_ThatIsJsonNull_YieldsNull(StreamFraming framing)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "null", "{\"name\":\"a\"}"));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().HaveCount(2);
        items[0].Should().BeNull();
        items[1]!.Name.Should().Be("a");
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task Record_ThatIsNoJson_ThrowsFromTheEnumerationAndNotFromTheBuilder(
        StreamFraming framing)
    {
        // Arrange
        // Two reads: a reader may refuse what it has been given before it yields the item ahead of it.
        using var body = FeedStream.From(
            Encoding.UTF8.GetBytes(StreamWire.Open(framing) + StreamWire.Record(framing, "{\"name\":\"a\"}")),
            Encoding.UTF8.GetBytes(StreamWire.Record(framing, "{oops", isLast: true) + StreamWire.Close(framing)));
        using var response = StreamWire.Response(framing, body);
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue();
        items.Current!.Name.Should().Be("a");

        var act = () => items.NextAsync();
        await act.Should().ThrowAsync<JsonException>();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task EmptyBody_YieldsNoItems(StreamFraming framing)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(FramingsAndBodiesWithoutContent))]
    public async Task SuccessWithoutContent_YieldsNoItems(
        StreamFraming framing,
        HttpStatusCode statusCode,
        string body)
    {
        // Arrange
        using var response = StreamWire.Response(framing, FeedStream.FromText(body), statusCode);
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task SuccessWithoutContent_OverAConnectionThatJustCloses_YieldsNoItems(
        StreamFraming framing)
    {
        // Arrange
        await using var server = LoopbackServer.Start(
            (response, _) => response.WriteHeadAsync(HttpStatusCode.OK, StreamWire.ContentType(framing)));

        using var client = new HttpClient();
        var httpResponse = await client.GetAsync(server.Uri, HttpCompletionOption.ResponseHeadersRead);
        var sut = CreateSut(httpResponse);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData(" \r\n [ ] \n")]
    public async Task JsonArray_EmptyArray_YieldsNoItems(string body)
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.JsonArray, body);
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(StreamFraming.JsonArray);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(" \r\n\t [1,2]")]
    [InlineData("﻿[1,2]")]
    [InlineData("﻿ \n[1,2]")]
    public async Task JsonArray_AfterLeadingWhitespaceOrAByteOrderMark_IsRead(
        string body)
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.JsonArray, body);
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(StreamFraming.JsonArray);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().Equal(1, 2);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("  {\"name\":\"a\"}")]
    [InlineData("not json")]
    public async Task JsonArray_ContentThatIsNoArray_ThrowsFromTheEnumeration(
        string body)
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.JsonArray, body);
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(StreamFraming.JsonArray);
        var act = () => result.Content!.ToListAsync().AsTask();

        // Assert
        var exception = await act.Should().ThrowAsync<JsonException>();
        exception.Which.Should().NotBeNull();
    }

    [Fact]
    public async Task JsonArray_ElementPastTheLimitAfterLeadingWhitespace_Throws()
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.JsonArray, " \n " + StreamWire.Document(StreamFraming.JsonArray, JsonStringOfLength(Limit + 1)));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>(StreamFraming.JsonArray);
        var act = () => result.Content!.ToListAsync().AsTask();

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*64 bytes*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n ")]
    public async Task BuildStreamingResponseAsync_WithoutContent_YieldsNoItems(
        string body)
    {
        // Arrange
        using var response = StreamWire.Response(StreamFraming.JsonArray, FeedStream.FromText(body));
        var sut = CreateSut(response);

        // Act
        var items = await sut.BuildStreamingResponseAsync<int>().ToListAsync();

        // Assert
        items.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task WithFactory_ReturnsTheFactoryResultWithTheItems(
        StreamFraming framing)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "{\"name\":\"a\"}", "{\"name\":\"b\"}"));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<SuccessResponse, DerivedStreamingEndpointResponse<SuccessResponse>>(
            x => new DerivedStreamingEndpointResponse<SuccessResponse>(x),
            framing);

        // Assert
        var items = await result.Content!.ToListAsync();
        items.Select(x => x!.Name).Should().Equal("a", "b");
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task WithOptions_TheLimitOfTheCallIsUsed(StreamFraming framing)
    {
        // Arrange
        var record = JsonStringOfLength(Limit);
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, record));
        var sut = CreateSut(response, maxStreamRecordLength: 16);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>(
            framing,
            new StreamReadOptions { MaxRecordLength = 1024 });
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task WithFactoryAndOptions_TheLimitOfTheCallIsUsed(
        StreamFraming framing)
    {
        // Arrange
        var record = JsonStringOfLength(Limit);
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, record));
        var sut = CreateSut(response, maxStreamRecordLength: 1024);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string, DerivedStreamingEndpointResponse<string>>(
            x => new DerivedStreamingEndpointResponse<string>(x),
            framing,
            new StreamReadOptions { MaxRecordLength = 16 });
        var act = () => result.Content!.ToListAsync().AsTask();

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task WithOptionsWithoutALimit_TheDefaultLimitIsUsed(
        StreamFraming framing)
    {
        // Arrange
        var record = JsonStringOfLength(Limit);
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, record));
        var sut = CreateSut(response, maxStreamRecordLength: 16);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>(framing, new StreamReadOptions());
        var act = () => result.Content!.ToListAsync().AsTask();

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [MemberData(nameof(FramingsAndLargestLimits))]
    public async Task WithOptions_TheLargestLimit_ReadsTheItems(
        StreamFraming framing,
        int maxRecordLength)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "1", "2"));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(
            framing,
            new StreamReadOptions { MaxRecordLength = maxRecordLength });
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().Equal(1, 2);
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task TheLargestLimitAsTheDefault_ReadsTheItems(
        StreamFraming framing)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "1", "2"));
        var sut = CreateSut(response, maxStreamRecordLength: int.MaxValue);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(framing);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().Equal(1, 2);
    }

    // The smallest limit a record of one byte fits in. (A multipart stream has delimiter and header lines, which are
    // limited to the same length, so it has no limit this small.)
    [Theory]
    [InlineData(StreamFraming.JsonArray, 1)]
    [InlineData(StreamFraming.JsonLines, 1)]
    [InlineData(StreamFraming.JsonSequence, 1)]
    [InlineData(StreamFraming.ServerSentEvents, 7)]
    public async Task WithOptions_TheSmallestLimit_ReadsARecordThatFitsAndRefusesOneByteMore(
        StreamFraming framing,
        int maxRecordLength)
    {
        // Arrange
        using var response = StreamWire.Response(framing, StreamWire.Document(framing, "1", "2", "34"));
        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(
            framing,
            new StreamReadOptions { MaxRecordLength = maxRecordLength });
        await using var items = result.Content!.GetAsyncEnumerator();

        // Assert
        (await items.NextAsync()).Should().BeTrue();
        items.Current.Should().Be(1);
        (await items.NextAsync()).Should().BeTrue();
        items.Current.Should().Be(2);

        var act = () => items.NextAsync();
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task WithOptions_ALimitThatIsNotPositive_ThrowsAndDisposesTheHttpResponse(
        int maxRecordLength)
    {
        // Arrange
        using var response = new DisposeCountingHttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        };
        var sut = CreateSut(response);

        // Act
        var act = () => sut.BuildStreamingEndpointResponseAsync<int>(
            StreamFraming.JsonLines,
            new StreamReadOptions { MaxRecordLength = maxRecordLength });

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        response.DisposeCount.Should().Be(1);
    }

    [Fact]
    public async Task UnknownFraming_ThrowsAndDisposesTheHttpResponse()
    {
        // Arrange
        using var response = new DisposeCountingHttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        };
        var sut = CreateSut(response);

        // Act
        var act = () => sut.BuildStreamingEndpointResponseAsync<int>((StreamFraming)99);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        response.DisposeCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(Framings))]
    public async Task NullResponse_ReturnsInternalServerError(
        StreamFraming framing)
    {
        // Arrange
        var sut = new MessageResponseBuilder(response: null, serializer);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<int>(framing);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Content.Should().BeNull();
        result.Headers.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildStreamingResponseAsync_AnElementLongerThanTheLimit_Throws()
    {
        // Arrange
        using var response = StreamWire.Response(
            StreamFraming.JsonArray,
            StreamWire.Document(StreamFraming.JsonArray, JsonStringOfLength(Limit + 1)));
        var sut = CreateSut(response);

        // Act
        var act = () => sut.BuildStreamingResponseAsync<string>().ToListAsync().AsTask();

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*element of the JSON array*");
    }

    // A JSON string of the given length in bytes, quotes included.
    private static string JsonStringOfLength(int length)
        => "\"" + new string('a', length - 2) + "\"";

    private MessageResponseBuilder CreateSut(
        HttpResponseMessage response,
        int maxStreamRecordLength = Limit)
        => new(response, serializer, maxStreamRecordLength);
}