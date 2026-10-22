namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderStreamedBodyTests
{
    private readonly IContractSerializer serializer = new DefaultJsonContractSerializer();

    private MessageRequestBuilder CreateSut()
        => new("/api", serializer);

    [Fact]
    public async Task WithStreamedBody_WritesOneRecordPerLine()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithStreamedBody(Records(new TestModel("a", 1), new TestModel("b", 2), new TestModel("c", 3)));
        using var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/jsonl");
        message.Content.Headers.ContentLength.Should().BeNull();
        var body = await ReadAsync(message.Content);
        body.Should().EndWith("\n");
        body.TrimEnd('\n').Split('\n')
            .Select(line => JsonSerializer.Deserialize<TestModel>(line, JsonSerializerOptions.Web))
            .Should()
            .Equal(new TestModel("a", 1), new TestModel("b", 2), new TestModel("c", 3));
    }

    [Theory]
    [InlineData("application/jsonl")]
    [InlineData("application/x-ndjson")]
    [InlineData("application/x-jsonlines")]
    [InlineData("application/jsonl; charset=utf-8")]
    public async Task WithStreamedBody_JsonLinesMediaTypes_AreOneCompactRecordPerLine(
        string contentType)
    {
        // Arrange - the default serializer indents; each record must still be one line
        var compact = new DefaultJsonContractSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var sut = new MessageRequestBuilder("/api", compact);

        // Act
        sut.WithStreamedBody(Records(new TestModel("a", 1), new TestModel("b", 2)), contentType);
        using var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content!.Headers.ContentType!.ToString().Should().Be(contentType);
        var body = await ReadAsync(message.Content);
        body.Should().Be("{\"name\":\"a\",\"value\":1}\n{\"name\":\"b\",\"value\":2}\n");
    }

    [Fact]
    public async Task WithStreamedBody_IndentingSerializer_KeepsEachRecordOnOneLine()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithStreamedBody(Records(new TestModel("line\nbreak", 1), new TestModel("b", 2)));
        using var message = sut.Build(HttpMethod.Post);

        // Assert
        var lines = (await ReadAsync(message.Content!)).Split('\n');
        lines.Should().HaveCount(3);
        lines[2].Should().BeEmpty();
        JsonSerializer.Deserialize<TestModel>(lines[0], JsonSerializerOptions.Web).Should().Be(new TestModel("line\nbreak", 1));
        JsonSerializer.Deserialize<TestModel>(lines[1], JsonSerializerOptions.Web).Should().Be(new TestModel("b", 2));
    }

    [Theory]
    [InlineData("application/json-seq")]
    [InlineData("application/geo+json-seq")]
    public async Task WithStreamedBody_JsonSequence_StartsEachRecordWithTheRecordSeparator(
        string contentType)
    {
        // Arrange
        var compact = new DefaultJsonContractSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var sut = new MessageRequestBuilder("/api", compact);

        // Act
        sut.WithStreamedBody(Records(new TestModel("a", 1), new TestModel("b", 2)), contentType);
        using var message = sut.Build(HttpMethod.Post);

        // Assert
        var body = await ReadAsync(message.Content!);
        body.Should().Be("\u001e{\"name\":\"a\",\"value\":1}\n\u001e{\"name\":\"b\",\"value\":2}\n");
    }

    [Fact]
    public async Task WithStreamedBody_NullRecord_IsTheJsonNullLine()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithStreamedBody(Records<string?>("a", null, "c"));
        using var message = sut.Build(HttpMethod.Post);

        // Assert
        var body = await ReadAsync(message.Content!);
        body.Should().Be("\"a\"\nnull\n\"c\"\n");
    }

    [Fact]
    public async Task WithStreamedBody_NoRecords_IsAnEmptyBody()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithStreamedBody(Records<TestModel>());
        using var message = sut.Build(HttpMethod.Post);

        // Assert
        var body = await ReadAsync(message.Content!);
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task WithStreamedBody_SourceThatWaits_FlushesEachRecordBeforeTheNextIsRequested()
    {
        // Arrange
        var compact = new DefaultJsonContractSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var sut = new MessageRequestBuilder("/api", compact);
        var stream = new FlushRecordingStream();

        // Act
        sut.WithStreamedBody(WaitingRecords(stream, new TestModel("a", 1), new TestModel("b", 2)));
        using var message = sut.Build(HttpMethod.Post);
        await message.Content!.CopyToAsync(stream, TestContext.Current.CancellationToken);

        // Assert - when the source asks for the next record, the previous one has been flushed
        var first = "{\"name\":\"a\",\"value\":1}\n".Length;
        var second = first + "{\"name\":\"b\",\"value\":2}\n".Length;
        stream.FlushedLengthWhenRequested.Should().Equal(0, first, second);
        stream.Flushed.Should().Be(second);
    }

    [Fact]
    public async Task WithStreamedBody_SentTwice_TheSecondSendThrows()
    {
        // Arrange
        var sut = CreateSut();
        sut.WithStreamedBody(Records(new TestModel("a", 1)));
        using var message = sut.Build(HttpMethod.Post);
        using var first = new MemoryStream();
        await message.Content!.CopyToAsync(first, TestContext.Current.CancellationToken);
        using var second = new MemoryStream();

        // Act
        var act = () => message.Content.CopyToAsync(second, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void WithStreamedBody_NullRecords_Throws()
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithStreamedBody<TestModel>(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/event-stream")]
    [InlineData("text/plain")]
    public void WithStreamedBody_NotAStreamOfRecords_Throws(string contentType)
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithStreamedBody(Records(new TestModel("a", 1)), contentType))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithStreamedBody_ContentType_Throws_If_Null_Or_WhiteSpace(
        string? contentType)
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithStreamedBody(Records(new TestModel("a", 1)), contentType!))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void WithStreamedBody_WithUrlEncodedForm_Throws()
    {
        var sut = CreateSut();
        sut.WithStreamedBody(Records(new TestModel("a", 1)));
        sut.WithUrlEncodedFormField("name", "value");

        sut.Invoking(x => x.Build(HttpMethod.Post))
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithStreamedBody_ReturnsTheBuilder()
    {
        var sut = CreateSut();

        sut.WithStreamedBody(Records(new TestModel("a", 1))).Should().BeSameAs(sut);
    }

    private static async Task<string> ReadAsync(HttpContent content)
    {
        using var stream = new MemoryStream();
        await content.CopyToAsync(stream, TestContext.Current.CancellationToken);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

#pragma warning disable CS1998 // An iterator without await is a source that never waits
    private static async IAsyncEnumerable<T> Records<T>(params T[] records)
    {
        foreach (var record in records)
        {
            yield return record;
        }
    }
#pragma warning restore CS1998

    private static async IAsyncEnumerable<T> WaitingRecords<T>(
        FlushRecordingStream stream,
        params T[] records)
    {
        foreach (var record in records)
        {
            await Task.Yield();
            stream.FlushedLengthWhenRequested.Add(stream.Flushed);
            yield return record;
        }

        await Task.Yield();
        stream.FlushedLengthWhenRequested.Add(stream.Flushed);
    }

    private sealed class FlushRecordingStream : MemoryStream
    {
        public List<long> FlushedLengthWhenRequested { get; } = [];

        public long Flushed { get; private set; }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            Flushed = Length;
            return base.FlushAsync(cancellationToken);
        }
    }
}