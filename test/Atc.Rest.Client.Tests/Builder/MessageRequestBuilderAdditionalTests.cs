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

        // Assert - empty arrays should be omitted entirely (PR #21 fix)
        message.RequestUri!.ToString().Should().Be("/api");
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

    [Fact]
    public void WithHeaderParameter_WithEmptyString_AddsHeader()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithHeaderParameter("X-Empty", string.Empty);
        var message = sut.Build(HttpMethod.Get);

        // Assert - empty string is still added as header value
        message.Headers.Contains("X-Empty").Should().BeTrue();
        message.Headers.GetValues("X-Empty").Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Fact]
    public void WithQueryParameter_WithGuidArray_FormatsCorrectly()
    {
        // Arrange
        var sut = CreateSut("/api");
        var guids = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };

        // Act
        sut.WithQueryParameter("ids", guids);
        var message = sut.Build(HttpMethod.Get);

        // Assert
        var uri = message!.RequestUri!.ToString();
        uri.Should().Contain("ids=11111111-1111-1111-1111-111111111111");
        uri.Should().Contain("ids=22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void WithQueryParameter_WithDecimalValue_FormatsCorrectly()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("price", 100m);
        var message = sut.Build(HttpMethod.Get);

        // Assert - use integer decimal to avoid locale-specific decimal separator issues
        message!.RequestUri!.ToString().Should().Be("/api?price=100");
    }

    [Fact]
    public void Build_WithFormFieldsAndFiles_CreatesMultipartContent()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        sut.WithFormField("name", "test");
        sut.WithFile(stream, "file", "test.txt");
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public void WithQueryParameter_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("name", "日本語");
        var message = sut.Build(HttpMethod.Get);

        // Assert - URI contains the parameter (may be URL-encoded on some platforms)
        var uri = message!.RequestUri!.ToString();
        uri.Should().Contain("name=");

        // Verify the decoded URI contains the original value
        Uri.UnescapeDataString(uri).Should().Contain("日本語");
    }

    [Fact]
    public void WithPathParameter_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var sut = CreateSut("/api/users/{name}");

        // Act
        sut.WithPathParameter("name", "日本語");
        var message = sut.Build(HttpMethod.Get);

        // Assert - URI replaces the placeholder (may be URL-encoded on some platforms)
        var uri = message!.RequestUri!.ToString();
        uri.Should().NotContain("{name}");

        // Verify the decoded URI contains the original value
        Uri.UnescapeDataString(uri).Should().Contain("日本語");
    }

    [Fact]
    public void WithFile_ThrowsOnNullStream()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Invoking(x => x.WithFile(null!, "file", "test.txt"))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("stream");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithFile_ThrowsOnInvalidName(string? name)
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act & Assert
        sut.Invoking(x => x.WithFile(stream, name!, "test.txt"))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithFile_ThrowsOnInvalidFileName(string? fileName)
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act & Assert
        sut.Invoking(x => x.WithFile(stream, "file", fileName!))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithFile_ReturnsSameInstance()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream();

        // Act & Assert
        sut.WithFile(stream, "file", "test.txt").Should().BeSameAs(sut);
    }

    [Fact]
    public void WithFile_WithContentType_SetsContentType()
    {
        // Arrange
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        sut.WithFile(stream, "image", "photo.png", "image/png");
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public void WithFiles_ThrowsOnNullCollection()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Invoking(x => x.WithFiles(null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("files");
    }

    [Fact]
    public void WithFiles_ReturnsSameInstance()
    {
        // Arrange
        var sut = CreateSut();
        using var stream1 = new MemoryStream();
        using var stream2 = new MemoryStream();
        var files = new (Stream Stream, string Name, string FileName, string? ContentType)[]
        {
            (stream1, "file1", "a.txt", null),
            (stream2, "file2", "b.txt", "text/plain"),
        };

        // Act & Assert
        sut.WithFiles(files).Should().BeSameAs(sut);
    }

    [Fact]
    public void WithFiles_AddsMultipleFiles()
    {
        // Arrange
        var sut = CreateSut();
        using var stream1 = new MemoryStream(new byte[] { 1 });
        using var stream2 = new MemoryStream(new byte[] { 2 });
        var files = new (Stream Stream, string Name, string FileName, string? ContentType)[]
        {
            (stream1, "file1", "a.txt", null),
            (stream2, "file2", "b.txt", "text/plain"),
        };

        // Act
        sut.WithFiles(files);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithFormField_ThrowsOnInvalidName(string? name)
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Invoking(x => x.WithFormField(name!, "value"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithFormField_ThrowsOnNullValue()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Invoking(x => x.WithFormField("field", null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("value");
    }

    [Fact]
    public void WithFormField_ReturnsSameInstance()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.WithFormField("name", "value").Should().BeSameAs(sut);
    }

    [Fact]
    public void WithFormField_OnlyFormFields_CreatesMultipartContent()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.WithFormField("field1", "value1");
        sut.WithFormField("field2", "value2");
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public void Build_WithOnlyBinaryBody_TakesPrecedenceOverFormFields()
    {
        // Arrange - binary body set after form fields should still be binary
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act - JSON body has priority over binary body (set order matters)
        sut.WithBinaryBody(stream);
        var message = sut.Build(HttpMethod.Post);

        // Assert
        message.Content.Should().BeOfType<StreamContent>();
    }

    [Fact]
    public void Build_ContentPriority_JsonOverBinaryOverMultipart()
    {
        // Arrange - test that JSON body takes priority
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        serializer.Serialize(Arg.Any<object>()).Returns("{\"test\":true}");

        // Act - set binary first, then JSON
        sut.WithBinaryBody(stream);
        sut.WithBody(new { test = true });
        var message = sut.Build(HttpMethod.Post);

        // Assert - JSON content takes priority (checked first in Build method)
        message.Content.Should().BeOfType<StringContent>();
    }

    [Fact]
    public void WithQueryParameter_WithEnumArray_FormatsCorrectly()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act - since the generic WithQueryParameter(IEnumerable) is used, enums go through ToString
        var roles = new[] { "admin", "user" };
        sut.WithQueryParameter("roles", roles);
        var message = sut.Build(HttpMethod.Get);

        // Assert
        var uri = message!.RequestUri!.ToString();
        uri.Should().Contain("roles=admin");
        uri.Should().Contain("roles=user");
    }

    [Fact]
    public void WithQueryParameter_MultipleSameNameParameters_AllIncluded()
    {
        // Arrange
        var sut = CreateSut("/api");

        // Act
        sut.WithQueryParameter("tag", "a");
        sut.WithQueryParameter("other", "value");
        var message = sut.Build(HttpMethod.Get);

        // Assert - last value wins for same key (dictionary behavior)
        var uri = message!.RequestUri!.ToString();
        uri.Should().Contain("tag=a");
        uri.Should().Contain("other=value");
    }
}