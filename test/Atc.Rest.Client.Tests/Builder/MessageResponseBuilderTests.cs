namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageResponseBuilderTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
        => new(response, serializer);

    [Theory, AutoNSubstituteData]
    public async Task IsSuccess_Should_Respect_Configured_ErrorResponse(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.NotFound;

        var result = await sut.AddErrorResponse(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        result
            .IsSuccess
            .Should()
            .BeFalse();
    }

    [Theory, AutoNSubstituteData]
    public async Task IsSuccess_Should_Respect_Configured_SuccessResponse(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.OK;

        var result = await sut.AddSuccessResponse(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        result
            .IsSuccess
            .Should()
            .BeTrue();
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Deserialize_Configured_SuccessResponseCode(
        HttpResponseMessage response,
        Guid expectedResponse,
        CancellationToken cancellationToken)
    {
        serializer.Deserialize<Guid>(Arg.Any<string>()).Returns(expectedResponse);
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.OK;

        var result = await sut.AddSuccessResponse<Guid>(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        result
            .ContentObject
            .Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Deserialize_Configured_ErrorResponseCode(
        HttpResponseMessage response,
        Guid expectedResponse,
        CancellationToken cancellationToken)
    {
        serializer.Deserialize<Guid>(Arg.Any<string>()).Returns(expectedResponse);
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.BadRequest;

        var result = await sut.AddErrorResponse<Guid>(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        result
            .ContentObject
            .Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Return_Response_Headers(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var sut = CreateSut(response);
        var expected = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
        {
            { "responseHeader", new[] { "value" } },
            { "contentHeader", new[] { "value" } },
        };
        response.Headers.Add("responseHeader", "value");
        response.Content.Headers.Add("contentHeader", "value");
        response.StatusCode = HttpStatusCode.OK;

        var result = await sut.AddSuccessResponse(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        result
            .Headers
            .Should()
            .BeEquivalentTo(expected);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_SuccessContent_NotBeNull(
        HttpResponseMessage response,
        SuccessResponse expectedResponse,
        CancellationToken cancellationToken)
    {
        serializer.Deserialize<SuccessResponse>(Arg.Any<string>()).Returns(expectedResponse);
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.OK;

        var result = await sut.AddSuccessResponse<SuccessResponse>(response.StatusCode)
            .BuildResponseAsync<SuccessResponse>(cancellationToken);

        result
            .SuccessContent
            .Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_FailureContent_BeNull(
        HttpResponseMessage response,
        SuccessResponse expectedResponse,
        CancellationToken cancellationToken)
    {
        serializer.Deserialize<SuccessResponse>(Arg.Any<string>()).Returns(expectedResponse);
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.OK;

        var result = await sut.AddSuccessResponse<SuccessResponse>(response.StatusCode)
            .BuildResponseAsync<SuccessResponse, BadResponse>(cancellationToken);

        result
            .ErrorContent
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_FailureContent_NotBeNull(
        HttpResponseMessage response,
        BadResponse expectedResponse,
        CancellationToken cancellationToken)
    {
        serializer.Deserialize<BadResponse>(Arg.Any<string>()).Returns(expectedResponse);
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.BadRequest;

        var result = await sut.AddErrorResponse<BadResponse>(response.StatusCode)
            .BuildResponseAsync<SuccessResponse, BadResponse>(cancellationToken);

        result
            .ErrorContent
            .Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_SuccessContent_BeNull(
        HttpResponseMessage response,
        BadResponse expectedResponse,
        CancellationToken cancellationToken)
    {
        serializer.Deserialize<BadResponse>(Arg.Any<string>()).Returns(expectedResponse);
        var sut = CreateSut(response);
        response.StatusCode = HttpStatusCode.BadRequest;

        var result = await sut.AddErrorResponse<BadResponse>(response.StatusCode)
            .BuildResponseAsync<SuccessResponse, BadResponse>(cancellationToken);

        result
            .SuccessContent
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineAutoNSubstituteData("application/json")]
    [InlineAutoNSubstituteData("application/JSON")]
    [InlineAutoNSubstituteData("Application/Json")]
    [InlineAutoNSubstituteData("text/plain")]
    [InlineAutoNSubstituteData("text/PLAIN")]
    [InlineAutoNSubstituteData("TEXT/plain")]
    public async Task Should_Deserialize_Content_With_Various_ContentType_Cases(
        string contentType,
        Guid expectedResponse,
        CancellationToken cancellationToken)
    {
        // Arrange
        serializer.Deserialize<Guid>(Arg.Any<string>()).Returns(expectedResponse);

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, contentType),
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.AddSuccessResponse<Guid>(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        // Assert
        result
            .ContentObject
            .Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Deserialize_Content_With_Charset_Parameter(
        Guid expectedResponse,
        CancellationToken cancellationToken)
    {
        // Arrange
        serializer.Deserialize<Guid>(Arg.Any<string>()).Returns(expectedResponse);

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };

        response.Content.Headers.ContentType!.CharSet = "utf-8";

        var sut = CreateSut(response);

        // Act
        var result = await sut.AddSuccessResponse<Guid>(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        // Assert
        result
            .ContentObject
            .Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Read_Binary_Content_For_NonText_ContentType(
        CancellationToken cancellationToken)
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(binaryData),
        };

        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var sut = CreateSut(response);

        // Act
        var result = await sut.AddSuccessResponse(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        // Assert
        result
            .ContentObject
            .Should()
            .BeEquivalentTo(binaryData);
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Throw_RestClientDeserializationException_When_Deserialization_Fails(
        CancellationToken cancellationToken)
    {
        // Arrange
        var innerException = new InvalidOperationException("Deserialization failed");
        serializer.Deserialize<SuccessResponse>(Arg.Any<string>()).Throws(innerException);

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json content", System.Text.Encoding.UTF8, "application/json"),
        };

        var sut = CreateSut(response);

        // Act
        var act = () => sut.AddSuccessResponse<SuccessResponse>(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<RestClientDeserializationException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.OK);
        exception.Which.RawContent.Should().Be("invalid json content");
        exception.Which.InnerException.Should().BeSameAs(innerException);
        exception.Which.Message.Should().Contain("200");
        exception.Which.Message.Should().Contain("OK");
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Include_StatusCode_In_DeserializationException(
        CancellationToken cancellationToken)
    {
        // Arrange
        serializer.Deserialize<BadResponse>(Arg.Any<string>()).Throws(new JsonException("Parse error"));

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{malformed}", System.Text.Encoding.UTF8, "application/json"),
        };

        var sut = CreateSut(response);

        // Act
        var act = () => sut.AddErrorResponse<BadResponse>(response.StatusCode)
            .BuildResponseAsync(res => res, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<RestClientDeserializationException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.RawContent.Should().Be("{malformed}");
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Not_Throw_When_No_Serializer_Registered_For_StatusCode(
        CancellationToken cancellationToken)
    {
        // Arrange - no serializer registered for OK status
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("plain text content", System.Text.Encoding.UTF8, "text/plain"),
        };

        var sut = CreateSut(response);

        // Act - don't register any response handler
        var result = await sut.BuildResponseAsync(res => res, cancellationToken);

        // Assert - should return raw content without throwing
        result.Content.Should().Be("plain text content");
        result.ContentObject.Should().Be("plain text content");
    }
}