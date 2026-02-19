namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class TestableStreamingEndpointResponse<T> : StreamingEndpointResponse<T>
{
    public TestableStreamingEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        IAsyncEnumerable<T?>? content,
        string? errorContent,
        HttpResponseMessage? httpResponse)
        : base(isSuccess, statusCode, content, errorContent, httpResponse)
    {
    }

    public InvalidOperationException GetInvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName)
        => InvalidContentAccessException(expectedStatusCode, propertyName);
}