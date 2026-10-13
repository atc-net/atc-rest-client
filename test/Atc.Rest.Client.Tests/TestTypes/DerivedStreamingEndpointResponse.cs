namespace Atc.Rest.Client.Tests.TestTypes;

// A result class as a generator emits it: built from the response the builder made.
internal sealed class DerivedStreamingEndpointResponse<T> : StreamingEndpointResponse<T>
{
    public DerivedStreamingEndpointResponse(
        StreamingEndpointResponse<T> response)
        : base(response)
    {
    }

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    public ProblemDetails NotFoundContent
        => IsNotFound && ErrorContentObject is ProblemDetails result
            ? result
            : throw InvalidContentAccessException(HttpStatusCode.NotFound, nameof(NotFoundContent));
}