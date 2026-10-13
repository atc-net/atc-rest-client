namespace Atc.Rest.Client.AotSample;

/// <summary>
/// A result class for a streamed operation, as a generated client has one: built from the response the builder made.
/// </summary>
public sealed class OrderStreamResult : StreamingEndpointResponse<Order>
{
    public OrderStreamResult(StreamingEndpointResponse<Order> response)
        : base(response)
    {
    }

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    public Order NotFoundContent
        => IsNotFound && ErrorContentObject is Order result
            ? result
            : throw InvalidContentAccessException(HttpStatusCode.NotFound, nameof(NotFoundContent));
}