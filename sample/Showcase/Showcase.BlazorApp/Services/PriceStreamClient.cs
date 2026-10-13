namespace Showcase.BlazorApp.Services;

/// <summary>
/// The price API of the showcase, called with Atc.Rest.Client. This class is all the HTTP code of the app.
/// </summary>
public sealed class PriceStreamClient
{
    public const string HttpClientName = "Prices";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IHttpMessageFactory messageFactory;

    public PriceStreamClient(
        IHttpClientFactory httpClientFactory,
        IHttpMessageFactory messageFactory)
    {
        this.httpClientFactory = httpClientFactory;
        this.messageFactory = messageFactory;
    }

    /// <summary>
    /// The price stream, in the framing asked for. It goes on until the result is disposed.
    /// </summary>
    /// <param name="framing">How the server is to frame the items, and so how they are read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, which the caller disposes.</returns>
    public Task<PriceStreamResult> StreamAsync(
        StreamFraming framing,
        CancellationToken cancellationToken)
        => SendAsync(
            messageFactory
                .FromTemplate("/api/prices")
                .WithQueryParameter("framing", framing.ToString()),
            framing,
            options: null,
            cancellationToken);

    /// <summary>
    /// A call that fails with 503, a problem body and a Retry-After header.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, which the caller disposes.</returns>
    public Task<PriceStreamResult> StreamUnavailableAsync(
        CancellationToken cancellationToken)
        => SendAsync(
            messageFactory.FromTemplate("/api/prices/unavailable"),
            StreamFraming.ServerSentEvents,
            options: null,
            cancellationToken);

    /// <summary>
    /// A stream whose second event is 64 KiB, read with a limit of <paramref name="maxRecordLength"/> bytes.
    /// </summary>
    /// <param name="maxRecordLength">The longest event that is read, in bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, which the caller disposes.</returns>
    public Task<PriceStreamResult> StreamHugeAsync(
        int maxRecordLength,
        CancellationToken cancellationToken)
        => SendAsync(
            messageFactory.FromTemplate("/api/prices/huge"),
            StreamFraming.ServerSentEvents,
            new StreamReadOptions { MaxRecordLength = maxRecordLength },
            cancellationToken);

    private async Task<PriceStreamResult> SendAsync(
        IMessageRequestBuilder requestBuilder,
        StreamFraming framing,
        StreamReadOptions? options,
        CancellationToken cancellationToken)
    {
        // Return when the headers are in: the body is a stream with no end to wait for
        requestBuilder.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead);

        using var request = requestBuilder.Build(HttpMethod.Get);

        // In the browser the body is read through fetch; this asks for it as a stream instead of all at once
        request.SetBrowserResponseStreamingEnabled(true);

        var client = httpClientFactory.CreateClient(HttpClientName);
        var response = await client
            .SendAsync(request, requestBuilder.HttpCompletionOption, cancellationToken)
            .ConfigureAwait(false);

        // The result owns the HTTP response from here on
        return await messageFactory
            .FromResponse(response)
            .AddErrorResponse<Problem>(HttpStatusCode.ServiceUnavailable)
            .BuildStreamingEndpointResponseAsync<PriceTick, PriceStreamResult>(
                x => new PriceStreamResult(x),
                framing,
                options,
                cancellationToken)
            .ConfigureAwait(false);
    }
}