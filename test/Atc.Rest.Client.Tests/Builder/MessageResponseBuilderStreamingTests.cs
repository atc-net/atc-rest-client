namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderStreamingTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
        => new(response, serializer);

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_NullResponse_ReturnsInternalServerError()
    {
        // Arrange
        var sut = CreateSut(response: null);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_FailedResponse_ReturnsErrorContent()
    {
        // Arrange
        const string errorContent = "Bad Request: Invalid data";

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorContent),
        };

        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Content.Should().BeNull();
        result.ErrorContent.Should().Be(errorContent);
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_SuccessResponse_ReturnsContentStream()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[\"item1\", \"item2\"]"),
        };

        var expectedItems = new[] { "item1", "item2" };
        serializer
            .DeserializeAsyncEnumerable<string>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(expectedItems.ToAsyncEnumerable());

        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Should().NotBeNull();
        result.ErrorContent.Should().BeNull();
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_SuccessResponse_CanEnumerateContent()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[\"item1\", \"item2\", \"item3\"]"),
        };

        var expectedItems = new[] { "item1", "item2", "item3" };
        serializer
            .DeserializeAsyncEnumerable<string>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(expectedItems.ToAsyncEnumerable());

        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>();
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task BuildStreamingEndpointResponseAsync_DisposingResponse_DisposesHttpResponse()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]"),
        };

        serializer
            .DeserializeAsyncEnumerable<string>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<string?>());

        var sut = CreateSut(response);

        // Act
        var result = await sut.BuildStreamingEndpointResponseAsync<string>();
        result.Dispose();

        // Assert - accessing disposed response should throw
        var act = () => response.Content.ReadAsStringAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Created, true)]
    [InlineData(HttpStatusCode.Accepted, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task BuildStreamingEndpointResponseAsync_RespectsHttpStatusCodeSuccess(
        HttpStatusCode statusCode,
        bool expectedSuccess)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("content"),
        };

        if (expectedSuccess)
        {
            serializer
                .DeserializeAsyncEnumerable<string>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string?>());
        }

        var sut = CreateSut(response);

        // Act
        using var result = await sut.BuildStreamingEndpointResponseAsync<string>();

        // Assert
        result.IsSuccess.Should().Be(expectedSuccess);
        result.StatusCode.Should().Be(statusCode);
    }
}