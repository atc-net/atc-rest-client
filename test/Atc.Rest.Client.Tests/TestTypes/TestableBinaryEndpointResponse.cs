namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class TestableBinaryEndpointResponse : BinaryEndpointResponse
{
    public TestableBinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        byte[]? content,
        string? contentType,
        string? fileName,
        long? contentLength)
        : base(isSuccess, statusCode, content, contentType, fileName, contentLength)
    {
    }

    public InvalidOperationException GetInvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName)
        => InvalidContentAccessException(expectedStatusCode, propertyName);
}