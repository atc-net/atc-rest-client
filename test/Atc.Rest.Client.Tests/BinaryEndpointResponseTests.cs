namespace Atc.Rest.Client.Tests;

public sealed class BinaryEndpointResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };

        // Act
        var sut = new BinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.OK,
            content,
            "application/octet-stream",
            "test.bin",
            contentLength: 3);

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.IsOk.Should().BeTrue();
        sut.StatusCode.Should().Be(HttpStatusCode.OK);
        sut.Content.Should().BeEquivalentTo(content);
        sut.ContentType.Should().Be("application/octet-stream");
        sut.FileName.Should().Be("test.bin");
        sut.ContentLength.Should().Be(3);
    }

    [Fact]
    public void IsOk_WhenStatusCodeIsNotOK_ReturnsFalse()
    {
        // Arrange & Act
        var sut = new BinaryEndpointResponse(
            isSuccess: true,
            HttpStatusCode.Created,
            content: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Assert
        sut.IsOk.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullValues_SetsPropertiesToNull()
    {
        // Arrange & Act
        var sut = new BinaryEndpointResponse(
            isSuccess: false,
            HttpStatusCode.NotFound,
            content: null,
            contentType: null,
            fileName: null,
            contentLength: null);

        // Assert
        sut.IsSuccess.Should().BeFalse();
        sut.Content.Should().BeNull();
        sut.ContentType.Should().BeNull();
        sut.FileName.Should().BeNull();
        sut.ContentLength.Should().BeNull();
    }
}