namespace Showcase.BlazorApp.Services;

/// <summary>
/// The result of the price stream: the items, and the status codes, the typed error and the headers of the
/// operation by name. A generated client has such a class per operation.
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

    public string? ContentType
        => Headers.TryGetValue("Content-Type", out var values)
            ? values.FirstOrDefault()
            : null;
}