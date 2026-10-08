namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderFormTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut()
        => new("/api", serializer);

    [Fact]
    public async Task WithFormField_RepeatedName_AddsOnePartPerValue_InOrder()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithFormField("itemName", "Quarterly");
        sut.WithFormField("items", "finance");
        sut.WithFormField("items", "q3");
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var multipart = message.Content.Should().BeOfType<MultipartFormDataContent>().Subject;
        var parts = new List<(string? Name, string Value)>();
        foreach (var part in multipart)
        {
            var value = await part.ReadAsStringAsync(TestContext.Current.CancellationToken);
            parts.Add((part.Headers.ContentDisposition?.Name?.Trim('"'), value));
        }

        parts.Should().Equal(("itemName", "Quarterly"), ("items", "finance"), ("items", "q3"));
    }

    [Fact]
    public async Task WithUrlEncodedFormField_SendsUrlEncodedBody()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithUrlEncodedFormField("grant_type", "password");
        sut.WithUrlEncodedFormField("username", "a b&c");
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<FormUrlEncodedContent>();
        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
        var body = await message.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("grant_type=password&username=a+b%26c");
    }

    [Fact]
    public async Task WithUrlEncodedFormField_RepeatedName_AddsOneFieldPerValue_InOrder()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithUrlEncodedFormField("scope", "read");
        sut.WithUrlEncodedFormField("scope", "write");
        var message = sut.Build(HttpMethod.Post);

        // Assert
        var body = await message.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("scope=read&scope=write");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithUrlEncodedFormField_Throws_If_Name_Is_Null_Or_WhiteSpace(
        string? name)
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithUrlEncodedFormField(name!, "value"))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void WithUrlEncodedFormField_Throws_If_Value_Is_Null()
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithUrlEncodedFormField("name", null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithUrlEncodedFormField_ReturnsSameInstance()
    {
        var sut = CreateSut();

        sut.WithUrlEncodedFormField("name", "value").Should().BeSameAs(sut);
    }

    [Fact]
    public void WithUrlEncodedFormField_CombinedWithMultipartFormField_ThrowsOnBuild()
    {
        var sut = CreateSut();
        sut.WithUrlEncodedFormField("a", "1");
        sut.WithFormField("b", "2");

        sut.Invoking(x => x.Build(HttpMethod.Post))
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithUrlEncodedFormField_CombinedWithJsonBody_ThrowsOnBuild()
    {
        serializer.Serialize(Arg.Any<object>()).Returns("{}");
        var sut = CreateSut();
        sut.WithUrlEncodedFormField("a", "1");
        sut.WithBody(new TestModel("Test", 42));

        sut.Invoking(x => x.Build(HttpMethod.Post))
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithUrlEncodedFormField_CombinedWithBinaryBody_ThrowsOnBuild()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var sut = CreateSut();
        sut.WithUrlEncodedFormField("a", "1");
        sut.WithBinaryBody(stream);

        sut.Invoking(x => x.Build(HttpMethod.Post))
            .Should()
            .Throw<InvalidOperationException>();
    }
}