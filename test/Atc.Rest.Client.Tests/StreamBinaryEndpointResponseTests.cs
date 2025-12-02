namespace Atc.Rest.Client.Tests;

public sealed class StreamBinaryEndpointResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            "application/octet-stream",
            "test.bin",
            contentLength: 3);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.IsOk.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.OK);
        sut.ContentStream.Should().BeSameAs(stream);
        sut.ContentType.Should().Be("application/octet-stream");
        sut.FileName.Should().Be("test.bin");
        sut.ContentLength.Should().Be(3);
    }

    [Fact]
    public void Dispose_DisposesContentStream()
    {
        // Arrange
        var stream = new MemoryStream([1, 2, 3]);
        var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        sut.Dispose();

        // Assert
        var act = () => stream.ReadByte();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        using var sut = new StreamBinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            stream,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Act
        var act = () => sut.Dispose();

        // Assert - first dispose already happens via using, this is the second
        act.Should().NotThrow();
    }
}