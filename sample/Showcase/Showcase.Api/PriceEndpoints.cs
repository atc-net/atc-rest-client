namespace Showcase.Api;

/// <summary>
/// The endpoints of the showcase. The streams are written by hand, record by record, so this file also shows what
/// each framing looks like on the wire.
/// </summary>
public static class PriceEndpoints
{
    public const string MultipartBoundary = "price-boundary";
    public const int HugeEventLength = 64 * 1024;

    private const char RecordSeparator = '\u001e';

    private static readonly TimeSpan Pace = TimeSpan.FromMilliseconds(350);
    private static readonly string[] Symbols = ["ATC", "NET", "SSE", "API"];

    /// <summary>
    /// A price about three times a second, for as long as the client reads, in the framing the client asks for
    /// with <c>?framing=</c> (server-sent events when it does not).
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the client stops reading.</returns>
    public static async Task StreamAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!Enum.TryParse<StreamFraming>(context.Request.Query["framing"], ignoreCase: true, out var framing))
        {
            framing = StreamFraming.ServerSentEvents;
        }

        context.Response.ContentType = GetContentType(framing);
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Stream-Framing"] = framing.ToString();

        var prices = Symbols.ToDictionary(x => x, _ => 100m, StringComparer.Ordinal);

        try
        {
            if (framing == StreamFraming.JsonArray)
            {
                await WriteAsync(context, "[").ConfigureAwait(false);
            }

            var sequence = 0L;
            while (!context.RequestAborted.IsCancellationRequested)
            {
                sequence++;
                var tick = NextTick(prices, sequence);
                var json = JsonSerializer.Serialize(tick, ShowcaseJsonContext.Default.PriceTick);

                await WriteAsync(context, FrameRecord(framing, json, sequence)).ConfigureAwait(false);
                await Task.Delay(Pace, context.RequestAborted).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The client stopped reading.
        }
    }

    /// <summary>
    /// A failed call: 503 with a problem body and a Retry-After header.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the response is written.</returns>
    public static Task UnavailableAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = new Problem("Prices are unavailable", StatusCodes.Status503ServiceUnavailable, "The exchange is closed.");

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.RetryAfter = "30";

        return WriteAsync(context, JsonSerializer.Serialize(problem, ShowcaseJsonContext.Default.Problem));
    }

    /// <summary>
    /// One ordinary event, then one of 64 KiB - more than the app accepts for this call.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the response is written.</returns>
    public static async Task HugeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.ContentType = GetContentType(StreamFraming.ServerSentEvents);

        var tick = new PriceTick("ATC", 100.25m, 0.25m, 1);
        var huge = tick with { Symbol = new string('A', HugeEventLength), Sequence = 2 };

        await WriteAsync(context, $"data: {JsonSerializer.Serialize(tick, ShowcaseJsonContext.Default.PriceTick)}\n\n").ConfigureAwait(false);
        await WriteAsync(context, $"data: {JsonSerializer.Serialize(huge, ShowcaseJsonContext.Default.PriceTick)}\n\n").ConfigureAwait(false);
    }

    private static string GetContentType(StreamFraming framing)
        => framing switch
        {
            StreamFraming.JsonLines => "application/jsonl",
            StreamFraming.JsonSequence => "application/json-seq",
            StreamFraming.ServerSentEvents => "text/event-stream",
            StreamFraming.MultipartMixed => $"multipart/mixed; boundary={MultipartBoundary}",
            _ => "application/json",
        };

    /// <summary>
    /// One record as it goes on the wire. Once it is written, the client has all it needs to yield the item.
    /// </summary>
    private static string FrameRecord(
        StreamFraming framing,
        string json,
        long sequence)
        => framing switch
        {
            // One line per value
            StreamFraming.JsonLines => $"{json}\n",

            // RFC 7464: the record separator, the value, a line feed
            StreamFraming.JsonSequence => $"{RecordSeparator}{json}\n",

            // An event is its fields and a blank line. A stream also carries what is no item:
            // a comment as keep-alive, and a named event as heartbeat - the client reads past both.
            StreamFraming.ServerSentEvents when sequence % 5 == 0 => $": keep-alive\n\nevent: ping\ndata: ping\n\nid: {sequence}\ndata: {json}\n\n",
            StreamFraming.ServerSentEvents => $"id: {sequence}\ndata: {json}\n\n",

            // A part is the boundary, its headers, a blank line and the value
            StreamFraming.MultipartMixed => $"--{MultipartBoundary}\r\nContent-Type: application/json\r\n\r\n{json}\r\n",

            // The elements of an array that is never closed while the stream runs
            _ => $"{json},",
        };

    private static PriceTick NextTick(
        Dictionary<string, decimal> prices,
        long sequence)
    {
        var symbol = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];
        var change = (RandomNumberGenerator.GetInt32(201) - 100) / 100m;
        var price = Math.Max(1m, prices[symbol] + change);
        prices[symbol] = price;

        return new PriceTick(symbol, price, change, sequence);
    }

    private static async Task WriteAsync(
        HttpContext context,
        string text)
    {
        await context.Response.WriteAsync(text, context.RequestAborted).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
    }
}