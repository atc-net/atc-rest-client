namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderBodyContentTypeTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut()
        => new("/api", serializer);

    [Fact]
    public async Task WithBody_ContentType_SendsSerializedBodyWithThatMediaType()
    {
        // Arrange
        var model = new TestModel("Test", 42);
        serializer.Serialize(model).Returns("""{"name":"Test"}""");
        var sut = CreateSut();

        // Act
        sut.WithBody(model, "application/merge-patch+json");
        var message = sut.Build(HttpMethod.Patch);

        // Assert
        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/merge-patch+json");
        var body = await message.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("""{"name":"Test"}""");
    }

    [Fact]
    public void WithBody_WithoutContentType_StillSendsApplicationJson()
    {
        serializer.Serialize(Arg.Any<object>()).Returns("{}");
        var sut = CreateSut();

        sut.WithBody(new TestModel("Test", 42));
        var message = sut.Build(HttpMethod.Post);

        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithBody_ContentType_Throws_If_Null_Or_WhiteSpace(
        string? contentType)
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithBody(new TestModel("Test", 42), contentType!))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void WithBody_ContentType_Throws_If_Malformed()
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithBody(new TestModel("Test", 42), "not a media type"))
            .Should()
            .Throw<FormatException>();
    }

    [Fact]
    public void WithBody_ContentType_WithFileContent_IsStillMultipart()
    {
        var sut = CreateSut();
        var file = new TestFileContent("report.pdf", "application/pdf", [1, 2, 3]);

        sut.WithBody(file, "application/vnd.x+json");
        var message = sut.Build(HttpMethod.Post);

        message.Content.Should().BeOfType<MultipartFormDataContent>();
        serializer.DidNotReceive().Serialize(Arg.Any<object>());
    }

    [Fact]
    public void WithBody_ContentType_ReturnsSameInstance()
    {
        serializer.Serialize(Arg.Any<object>()).Returns("{}");
        var sut = CreateSut();

        sut.WithBody(new TestModel("Test", 42), "application/vnd.x+json").Should().BeSameAs(sut);
    }
}