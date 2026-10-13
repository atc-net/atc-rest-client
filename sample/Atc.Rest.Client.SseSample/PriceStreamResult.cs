namespace Atc.Rest.Client.SseSample;

/// <summary>
/// A result class for the price stream, as a generated client has one per operation: the status codes, the typed
/// error and the headers of the operation by name, on top of the items.
/// </summary>
public sealed class PriceStreamResult : StreamingEndpointResponse<PriceTick>
{
    public PriceStreamResult(StreamingEndpointResponse<PriceTick> response)
        : base(response)
    {
    }

    public bool IsServiceUnavailable => StatusCode == HttpStatusCode.ServiceUnavailable;

    public Problem ServiceUnavailableContent
        => IsServiceUnavailable && ErrorContentObject is Problem result
            ? result
            : throw InvalidContentAccessException(HttpStatusCode.ServiceUnavailable, nameof(ServiceUnavailableContent));

    public TimeSpan? RetryAfter
        => Headers.TryGetValue("Retry-After", out var values) &&
           int.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
}