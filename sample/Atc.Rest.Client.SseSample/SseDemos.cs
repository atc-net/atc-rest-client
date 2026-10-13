namespace Atc.Rest.Client.SseSample;

/// <summary>
/// What reading server-sent events with Atc.Rest.Client looks like, one demo per feature.
/// </summary>
public sealed class SseDemos
{
    private const string ClientName = "Prices";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IHttpMessageFactory messageFactory;

    public SseDemos(
        IHttpClientFactory httpClientFactory,
        IHttpMessageFactory messageFactory)
    {
        this.httpClientFactory = httpClientFactory;
        this.messageFactory = messageFactory;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await ReadItemsAsTheyArriveAsync(cancellationToken).ConfigureAwait(false);
        await SkipWhatIsNoItemAsync(cancellationToken).ConfigureAwait(false);
        await ReadTheTypedErrorAndTheHeadersAsync(cancellationToken).ConfigureAwait(false);
        await UseAResultClassAsync(cancellationToken).ConfigureAwait(false);
        await StopEarlyAsync(cancellationToken).ConfigureAwait(false);
        await RefuseARecordThatIsTooLongAsync(cancellationToken).ConfigureAwait(false);
        await ResumeAfterTheLastEventAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void WriteTitle(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title}");
    }

    private static void WriteTick(
        Stopwatch clock,
        PriceTick? tick)
        => Console.WriteLine($"   +{clock.ElapsedMilliseconds,5} ms  #{tick?.Sequence} {tick?.Symbol} {tick?.Price.ToString("0.00", CultureInfo.InvariantCulture)}");

    /// <summary>
    /// The items are yielded one by one, each as soon as its event has arrived - the time stamps are 300 ms apart,
    /// as the server writes them. Nothing is buffered until the stream ends.
    /// </summary>
    private async Task ReadItemsAsTheyArriveAsync(
        CancellationToken cancellationToken)
    {
        WriteTitle("1. Items arrive as the server sends them");

        var httpResponse = await GetAsync("/prices", count: 5, noise: false, cancellationToken).ConfigureAwait(false);

        // The streaming response owns the HTTP response from here on: dispose the one, and the other follows.
        using var response = await messageFactory
            .FromResponse(httpResponse)
            .BuildStreamingEndpointResponseAsync<PriceTick>(StreamFraming.ServerSentEvents, cancellationToken)
            .ConfigureAwait(false);

        var clock = Stopwatch.StartNew();
        await foreach (var tick in response.Content!.ConfigureAwait(false))
        {
            WriteTick(clock, tick);
        }
    }

    /// <summary>
    /// A real event stream carries more than items: comments that keep the connection alive, and named events
    /// such as a heartbeat. Those are read past; only the events that are items are yielded.
    /// </summary>
    private async Task SkipWhatIsNoItemAsync(
        CancellationToken cancellationToken)
    {
        WriteTitle("2. Keep-alive comments and heartbeat events are no items");

        var httpResponse = await GetAsync("/prices", count: 3, noise: true, cancellationToken).ConfigureAwait(false);
        using var response = await messageFactory
            .FromResponse(httpResponse)
            .BuildStreamingEndpointResponseAsync<PriceTick>(StreamFraming.ServerSentEvents, cancellationToken)
            .ConfigureAwait(false);

        var clock = Stopwatch.StartNew();
        var items = 0;
        await foreach (var tick in response.Content!.ConfigureAwait(false))
        {
            WriteTick(clock, tick);
            items++;
        }

        Console.WriteLine($"   The server sent 3 comments, 3 'ping' events and 3 prices; {items} items were yielded.");
    }

    /// <summary>
    /// A failed call is not a stream. The error body is deserialized to the type registered for the status code,
    /// and the headers are there to read.
    /// </summary>
    private async Task ReadTheTypedErrorAndTheHeadersAsync(
        CancellationToken cancellationToken)
    {
        WriteTitle("3. A failed call has a typed error and headers");

        var httpResponse = await GetAsync("/prices/unavailable", cancellationToken).ConfigureAwait(false);
        using var response = await messageFactory
            .FromResponse(httpResponse)
            .AddErrorResponse<Problem>(HttpStatusCode.ServiceUnavailable)
            .BuildStreamingEndpointResponseAsync<PriceTick>(StreamFraming.ServerSentEvents, cancellationToken)
            .ConfigureAwait(false);

        Console.WriteLine($"   IsSuccess:          {response.IsSuccess}");
        Console.WriteLine($"   StatusCode:         {(int)response.StatusCode} {response.StatusCode}");
        if (response.ErrorContentObject is Problem problem)
        {
            Console.WriteLine($"   ErrorContentObject: {problem.Title} - {problem.Detail}");
        }

        // Header names are looked up without regard to casing
        Console.WriteLine($"   retry-after:        {string.Join(",", response.Headers["retry-after"])} s");
    }

    /// <summary>
    /// The factory overload builds a class of your own, which gives the status codes, the error and the headers
    /// of the operation a name.
    /// </summary>
    private async Task UseAResultClassAsync(CancellationToken cancellationToken)
    {
        WriteTitle("4. A result class of your own");

        var httpResponse = await GetAsync("/prices/unavailable", cancellationToken).ConfigureAwait(false);
        using var result = await messageFactory
            .FromResponse(httpResponse)
            .AddErrorResponse<Problem>(HttpStatusCode.ServiceUnavailable)
            .BuildStreamingEndpointResponseAsync<PriceTick, PriceStreamResult>(
                x => new PriceStreamResult(x),
                StreamFraming.ServerSentEvents,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsServiceUnavailable)
        {
            Console.WriteLine($"   {result.ServiceUnavailableContent.Title}; retry after {result.RetryAfter?.TotalSeconds} s");
        }
    }

    /// <summary>
    /// The stream of the server goes on for a thousand prices. Stop reading, dispose the response, and the
    /// connection is released - the server sees the request aborted and stops writing.
    /// </summary>
    private async Task StopEarlyAsync(CancellationToken cancellationToken)
    {
        WriteTitle("5. Stop early");

        var httpResponse = await GetAsync("/prices", count: 1000, noise: false, cancellationToken).ConfigureAwait(false);
        using var response = await messageFactory
            .FromResponse(httpResponse)
            .BuildStreamingEndpointResponseAsync<PriceTick>(StreamFraming.ServerSentEvents, cancellationToken)
            .ConfigureAwait(false);

        var clock = Stopwatch.StartNew();
        var read = 0;
        await foreach (var tick in response.Content!.ConfigureAwait(false))
        {
            WriteTick(clock, tick);
            read++;
            if (read == 3)
            {
                break;
            }
        }

        Console.WriteLine($"   Stopped after {read} of 1000.");
    }

    /// <summary>
    /// A peer that never ends an event would make a reader buffer until the process is out of memory. One event is
    /// limited in length (16 MiB by default); here the limit of this one call is 1024 bytes. The item before the
    /// event that is too long is still yielded.
    /// </summary>
    private async Task RefuseARecordThatIsTooLongAsync(
        CancellationToken cancellationToken)
    {
        WriteTitle("6. An event past the limit is refused, not buffered");

        var httpResponse = await GetAsync("/prices/huge", cancellationToken).ConfigureAwait(false);
        using var response = await messageFactory
            .FromResponse(httpResponse)
            .BuildStreamingEndpointResponseAsync<PriceTick>(
                StreamFraming.ServerSentEvents,
                new StreamReadOptions { MaxRecordLength = 1024 },
                cancellationToken)
            .ConfigureAwait(false);

        var clock = Stopwatch.StartNew();
        try
        {
            await foreach (var tick in response.Content!.ConfigureAwait(false))
            {
                WriteTick(clock, tick);
            }
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"   InvalidDataException: {ex.Message}");
        }
    }

    /// <summary>
    /// The server cuts the connection after three of six prices. Every event has an id, and the response has the
    /// last one that was read - so the caller connects again with it as Last-Event-ID, and the server continues
    /// after it. The library does not reconnect by itself: when and how often is the caller's to decide. How long
    /// to wait is what the server asked for with retry:, which OnEvent hands out.
    /// </summary>
    private async Task ResumeAfterTheLastEventAsync(
        CancellationToken cancellationToken)
    {
        WriteTitle("7. A stream that is cut off is continued after the last id");

        string? lastEventId = null;
        var retry = TimeSpan.FromSeconds(3);
        var options = new StreamReadOptions { OnEvent = x => retry = x.Retry ?? retry };
        var clock = Stopwatch.StartNew();

        while (true)
        {
            var requestBuilder = messageFactory
                .FromTemplate("/prices")
                .WithQueryParameter("count", 6)
                .WithQueryParameter("noise", false)
                .WithQueryParameter("dropAfter", 3);

            if (!string.IsNullOrEmpty(lastEventId))
            {
                requestBuilder.WithHeaderParameter("Last-Event-ID", lastEventId);
            }

            try
            {
                var httpResponse = await SendAsync(requestBuilder, cancellationToken).ConfigureAwait(false);
                using var response = await messageFactory
                    .FromResponse(httpResponse)
                    .BuildStreamingEndpointResponseAsync<PriceTick>(StreamFraming.ServerSentEvents, options, cancellationToken)
                    .ConfigureAwait(false);

                await foreach (var tick in response.Content!.ConfigureAwait(false))
                {
                    WriteTick(clock, tick);

                    // Where to continue, should the connection be lost after this item
                    lastEventId = response.LastEventId ?? lastEventId;
                }

                break;
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException)
            {
                Console.WriteLine($"   {ex.GetType().Name}: the connection was lost after id {lastEventId}.");
                Console.WriteLine($"   Again in {retry.TotalMilliseconds} ms, with Last-Event-ID: {lastEventId}");

                await Task.Delay(retry, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task<HttpResponseMessage> GetAsync(
        string path,
        CancellationToken cancellationToken)
        => SendAsync(messageFactory.FromTemplate(path), cancellationToken);

    private Task<HttpResponseMessage> GetAsync(
        string path,
        int count,
        bool noise,
        CancellationToken cancellationToken)
        => SendAsync(
            messageFactory
                .FromTemplate(path)
                .WithQueryParameter("count", count)
                .WithQueryParameter("noise", noise),
            cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(
        IMessageRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // Without this, HttpClient reads the whole body before it returns - and a stream has no end to wait for
        requestBuilder.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead);

        using var request = requestBuilder.Build(HttpMethod.Get);
        var client = httpClientFactory.CreateClient(ClientName);

        return await client
            .SendAsync(request, requestBuilder.HttpCompletionOption, cancellationToken)
            .ConfigureAwait(false);
    }
}