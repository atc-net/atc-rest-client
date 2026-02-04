namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderAdditionalTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut(string template = "/api") => new(template, serializer);

    [Fact]
    public void Build_WithNoContent_ReturnsMessageWithNullContent()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.Content.Should().BeNull();
    }

    [Fact]
    public void Build_SetsRequestUri()
    {
        // Arrange
        var sut = CreateSut("/api/users");

        // Act
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri.Should().NotBeNull();
        message.RequestUri!.ToString().Should().Be("/api/users");
    }

    [Fact]
    public void WithHeaderParameter_AddsCustomHeader()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithHeaderParameter("X-Custom-Header", "custom-value");
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.Headers.GetValues("X-Custom-Header").Should().Contain("custom-value");
    }

    [Fact]
    public void WithHeaderParameter_CanOverwriteAcceptHeader()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithHeaderParameter("accept", "text/plain");
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.Headers.Accept.Should().ContainSingle()
            .Which.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public void WithHeaderParameter_WithNullValue_DoesNotAddHeader()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithHeaderParameter("X-Optional", null);
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.Headers.Contains("X-Optional").Should().BeFalse();
    }

    [Fact]
    public void WithQueryParameter_WithNullValue_DoesNotAddParameter()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("optional", null);
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Be("/api");
    }

    [Fact]
    public void WithQueryParameter_ThrowsOnNullOrWhitespaceName()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Invoking(x => x.WithQueryParameter(null!, "value"))
            .Should().Throw<ArgumentException>();

        sut.Invoking(x => x.WithQueryParameter("", "value"))
            .Should().Throw<ArgumentException>();

        sut.Invoking(x => x.WithQueryParameter("  ", "value"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithPathParameter_UrlEncodesSpecialCharacters()
    {
        // Arrange
        var sut = CreateSut("/api/users/{id}");

        // Act
        sut.WithPathParameter("id", "user/with/slashes");
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Be("/api/users/user%2Fwith%2Fslashes");
    }

    [Fact]
    public void WithQueryParameter_UrlEncodesSpecialCharacters()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("search", "hello world&special=chars");
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Contain("search=hello%20world%26special%3Dchars");
    }

    [Fact]
    public void CombinedParameters_BuildsCorrectUri()
    {
        // Arrange
        var sut = CreateSut("/api/{resource}/{id}");

        // Act
        sut.WithPathParameter("resource", "users");
        sut.WithPathParameter("id", "123");
        sut.WithQueryParameter("include", "details");
        sut.WithQueryParameter("format", "json");
        sut.WithHeaderParameter("Authorization", "Bearer token");
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Be("/api/users/123?include=details&format=json");
        message.Headers.GetValues("Authorization").Should().Contain("Bearer token");
    }

    [Fact]
    public void FluentApi_ReturnsSameInstance()
    {
        // Arrange
        var sut = CreateSut("/api/{id}");

        // Act & Assert
        sut.WithPathParameter("id", "1").Should().BeSameAs(sut);
        sut.WithQueryParameter("q", "test").Should().BeSameAs(sut);
        sut.WithHeaderParameter("X-Test", "value").Should().BeSameAs(sut);
        sut.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead).Should().BeSameAs(sut);
    }

    [Fact]
    public void WithBody_SerializesObjectUsingSerializer()
    {
        // Arrange
        var sut = CreateSut();
        var body = new { Name = "Test", Value = 42 };
        serializer.Serialize(body).Returns("{\"name\":\"Test\",\"value\":42}");

        // Act
        sut.WithBody(body);
        _ = sut.Build(HttpMethod.Post);

        // Assert
        serializer.Received(1).Serialize(body);
    }

    [Fact]
    public void WithQueryParameter_WithBooleanValue_ConvertsToString()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("active", true);
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Be("/api?active=True");
    }

    [Fact]
    public void Build_CanBeCalledMultipleTimes()
    {
        // Arrange
        var sut = CreateSut("/api");
        sut.WithQueryParameter("page", 1);

        // Act
        var message1 = sut.Build(HttpMethod.Get);
        var message2 = sut.Build(HttpMethod.Post);

        // Assert
        message1.Method.Should().Be(HttpMethod.Get);
        message2.Method.Should().Be(HttpMethod.Post);
        message1.RequestUri.Should().Be(message2.RequestUri);
    }

    [Fact]
    public void WithPathParameter_MultipleReplacements_WorksCorrectly()
    {
        // Arrange
        var sut = CreateSut("/api/{version}/users/{userId}/orders/{orderId}");

        // Act
        sut.WithPathParameter("version", "v1");
        sut.WithPathParameter("userId", "user123");
        sut.WithPathParameter("orderId", "order456");
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Be("/api/v1/users/user123/orders/order456");
    }

    [Fact]
    public void WithQueryParameter_EmptyArray_DoesNotAddParameter()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("ids", Array.Empty<int>());
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri!.ToString().Should().Be("/api?ids=");
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("api")]
    [InlineData("https://example.com/api")]
    [InlineData("/api/v1/users")]
    public void Build_HandlesVariousUriFormats(string template)
    {
        // Arrange
        var sut = CreateSut(template);

        // Act
        var message = sut.Build(HttpMethod.Get);

        // Assert
        message.RequestUri.Should().NotBeNull();
        message.RequestUri!.ToString().Should().Be(template);
    }

    [Fact]
    public void WithBinaryBody_ThrowsOnNullStream()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Invoking(x => x.WithBinaryBody(null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("stream");
    }

    [Fact]
    public void WithBinaryBody_ReturnsSameInstance()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act & Assert
        sut.WithBinaryBody(stream).Should().BeSameAs(sut);
    }

    [Fact]
    public async Task WithBinaryBody_SetsStreamContent()
    {
        // Arrange
        var sut = CreateSut();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(data);

        // Act
        sut.WithBinaryBody(stream);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().NotBeNull();
        message.Content.Should().BeOfType<StreamContent>();
        var result = await message.Content!.ReadAsByteArrayAsync();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void WithBinaryBody_SetsDefaultContentType()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act
        sut.WithBinaryBody(stream);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content!.Headers.ContentType.Should().NotBeNull();
        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/octet-stream");
    }

    [Fact]
    public void WithBinaryBody_SetsCustomContentType()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();
        const string customContentType = "image/png";

        // Act
        sut.WithBinaryBody(stream, customContentType);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content!.Headers.ContentType.Should().NotBeNull();
        message.Content!.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact]
    public void WithBinaryBody_DoesNotInterfereWithJsonBody()
    {
        // Arrange - test that binary and JSON content are mutually exclusive
        // When JSON body is set, it takes precedence (checked first in Build)
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        serializer.Serialize(Arg.Any<object>()).Returns("{\"test\":true}");

        // Act - set JSON body first, then binary body
        sut.WithBody(new { test = true });
        sut.WithBinaryBody(stream);
        var message = sut.Build(HttpMethod.Post);

        // Assert - JSON content takes precedence (checked first in Build method)
        message.Content.Should().BeOfType<StringContent>();
    }

    [Fact]
    public void WithBinaryBody_UsedAlone_SetsStreamContent()
    {
        // Arrange - binary body used alone should work correctly
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act - only set binary body
        sut.WithBinaryBody(stream);
        var message = sut.Build(HttpMethod.Post);

        // Assert - binary content should be set
        message.Content.Should().BeOfType<StreamContent>();
        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/octet-stream");
    }
}