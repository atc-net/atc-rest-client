namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class TestableEndpointResponse : EndpointResponse
{
    public TestableEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        string content,
        object? contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
        : base(isSuccess, statusCode, content, contentObject, headers)
    {
    }

    public InvalidOperationException GetInvalidContentAccessException<TExpected>(
        HttpStatusCode expectedStatusCode,
        string propertyName)
        => InvalidContentAccessException<TExpected>(expectedStatusCode, propertyName);
}