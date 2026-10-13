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

    private const int RetryMilliseconds = 2000;

    private static readonly TimeSpan Pace = TimeSpan.FromMilliseconds(350);
    private static readonly string[] Symbols = ["ATC", "NET", "SSE", "API"];

    // One market for every connection, so a client that comes back finds the prices where it left them
    private static readonly Dictionary<string, decimal> Prices = Symbols.ToDictionary(x => x, _ => 100m, StringComparer.Ordinal);
    private static readonly Lock PricesLock = new();

    // Raised by DropAsync; a stream that sees it change cuts its connection
    private static long drops;

    /// <summary>
    /// A price about three times a second, for as long as the client reads, in the framing the client asks for
    /// with <c>?framing=</c> (server-sent events when it does not).
    /// </summary>
    /// <remarks>
    /// Every server-sent event has an id. A client that sends the last one it saw as <c>Last-Event-ID</c> is
    /// continued after it - the numbers of the prices go on without a gap.
    /// </remarks>
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

        // What a browser's EventSource sends by itself when it connects again; here the app does
        _ = long.TryParse(context.Request.Headers["Last-Event-ID"], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence);
        var dropsAtStart = Interlocked.Read(ref drops);

        try
        {
            if (framing == StreamFraming.JsonArray)
            {
                await WriteAsync(context, "[").ConfigureAwait(false);
            }

            if (framing == StreamFraming.ServerSentEvents)
            {
                // How long a client is to wait before it connects again, in milliseconds
                await WriteAsync(context, $"retry: {RetryMilliseconds}\n\n").ConfigureAwait(false);
            }

            while (!context.RequestAborted.IsCancellationRequested)
            {
                if (Interlocked.Read(ref drops) != dropsAtStart)
                {
                    // The connection is lost: no end of the response is written
                    context.Abort();
                    return;
                }

                sequence++;
                var tick = NextTick(sequence);
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
    /// Cuts the connection of every price stream that is open, as a network that fails would.
    /// </summary>
    /// <returns>204 No Content.</returns>
    public static IResult Drop()
    {
        Interlocked.Increment(ref drops);

        return Results.NoContent();
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

    private static PriceTick NextTick(long sequence)
    {
        var symbol = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];
        var change = (RandomNumberGenerator.GetInt32(201) - 100) / 100m;

        lock (PricesLock)
        {
            var price = Math.Max(1m, Prices[symbol] + change);
            Prices[symbol] = price;

            return new PriceTick(symbol, price, change, sequence);
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        string text)
    {
        await context.Response.WriteAsync(text, context.RequestAborted).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
    }
}