namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class TestableStreamBinaryEndpointResponse : StreamBinaryEndpointResponse
{
    public TestableStreamBinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        Stream? contentStream,
        string? contentType,
        string? fileName,
        long? contentLength)
        : base(isSuccess, statusCode, contentStream, contentType, fileName, contentLength)
    {
    }

    public InvalidOperationException GetInvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName)
        => InvalidContentAccessException(expectedStatusCode, propertyName);
}