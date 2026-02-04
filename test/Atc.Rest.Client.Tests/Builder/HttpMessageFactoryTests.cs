namespace Atc.Rest.Client.Tests.Builder;

public sealed class HttpMessageFactoryTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private HttpMessageFactory CreateSut() => new(serializer);

    [Theory, AutoNSubstituteData]
    public void Should_Provide_MessageResponseBuilder_FromResponse(
        HttpResponseMessage response)
        => CreateSut().FromResponse(response)
            .Should()
            .NotBeNull();

    [Fact]
    public void Should_Provide_MessageResponseBuilder_From_Null_Response()
        => CreateSut().FromResponse(null)
            .Should()
            .NotBeNull();

    [Theory, AutoNSubstituteData]
    public void Should_Provide_MessageRequestBuilder_FromTemplate(
        string template)
        => CreateSut().FromTemplate(template)
            .Should()
            .NotBeNull();

    [Fact]
    public void FromTemplate_Returns_IMessageRequestBuilder()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.FromTemplate("/api/test");

        // Assert
        result.Should().BeAssignableTo<IMessageRequestBuilder>();
    }

    [Fact]
    public void FromResponse_Returns_IMessageResponseBuilder()
    {
        // Arrange
        var sut = CreateSut();
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var result = sut.FromResponse(response);

        // Assert
        result.Should().BeAssignableTo<IMessageResponseBuilder>();
    }

    [Fact]
    public void FromTemplate_UsesProvidedSerializer()
    {
        // Arrange
        var testBody = new { Name = "Test" };
        serializer.Serialize(testBody).Returns("{\"name\":\"Test\"}");
        var sut = CreateSut();

        // Act
        var builder = sut.FromTemplate("/api");
        builder.WithBody(testBody);
        _ = builder.Build(HttpMethod.Post);

        // Assert
        serializer.Received(1).Serialize(testBody);
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/users/{id}")]
    [InlineData("https://example.com/api")]
    public void FromTemplate_AcceptsVariousTemplateFormats(string template)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.FromTemplate(template);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void FromResponse_AcceptsVariousStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        var sut = CreateSut();
        using var response = new HttpResponseMessage(statusCode);

        // Act
        var result = sut.FromResponse(response);

        // Assert
        result.Should().NotBeNull();
    }
}