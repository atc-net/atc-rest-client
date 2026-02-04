namespace Atc.Rest.Client.Tests;

public sealed class RestClientDeserializationExceptionTests
{
    [Fact]
    public void DefaultConstructor_Should_Create_Exception_With_Default_Values()
    {
        var exception = new RestClientDeserializationException();

        exception.Message.Should().NotBeNullOrEmpty();
        exception.StatusCode.Should().Be(default(HttpStatusCode));
        exception.RawContent.Should().BeEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void MessageConstructor_Should_Set_Message()
    {
        const string message = "Test error message";

        var exception = new RestClientDeserializationException(message);

        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(default(HttpStatusCode));
        exception.RawContent.Should().BeEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_Should_Set_Properties()
    {
        const string message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        var exception = new RestClientDeserializationException(message, innerException);

        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.StatusCode.Should().Be(default(HttpStatusCode));
        exception.RawContent.Should().BeEmpty();
    }

    [Fact]
    public void FullConstructor_Should_Set_All_Properties()
    {
        const string message = "Deserialization failed";
        var innerException = new JsonException("Invalid JSON");
        const HttpStatusCode statusCode = HttpStatusCode.OK;
        const string rawContent = "{invalid json}";

        var exception = new RestClientDeserializationException(
            message,
            innerException,
            statusCode,
            rawContent);

        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.StatusCode.Should().Be(statusCode);
        exception.RawContent.Should().Be(rawContent);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public void Should_Preserve_Various_StatusCodes(HttpStatusCode statusCode)
    {
        var exception = new RestClientDeserializationException(
            "Error",
            new JsonException("Invalid JSON"),
            statusCode,
            "content");

        exception.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void Should_Preserve_Empty_RawContent()
    {
        var exception = new RestClientDeserializationException(
            "Error",
            new JsonException("Invalid JSON"),
            HttpStatusCode.OK,
            string.Empty);

        exception.RawContent.Should().BeEmpty();
    }

    [Fact]
    public void Should_Preserve_Large_RawContent()
    {
        var largeContent = new string('x', 10000);

        var exception = new RestClientDeserializationException(
            "Error",
            new JsonException("Invalid JSON"),
            HttpStatusCode.OK,
            largeContent);

        exception.RawContent.Should().Be(largeContent);
    }

    [Fact]
    public void Should_Be_Catchable_As_Base_Exception()
    {
        var exception = new RestClientDeserializationException("Test");

        Action act = () => throw exception;

        act.Should().Throw<Exception>()
            .Which.Should().BeOfType<RestClientDeserializationException>();
    }
}