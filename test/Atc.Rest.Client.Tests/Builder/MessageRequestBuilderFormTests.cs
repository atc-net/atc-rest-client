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
}