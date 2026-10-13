namespace Atc.Rest.Client.SseSample;

/// <summary>
/// The server of the sample: three endpoints on a loopback port. The events are written by hand, so the sample
/// shows what server-sent events look like on the wire - an event is its fields, one per line, and a blank line.
/// </summary>
public sealed class SampleServer : IAsyncDisposable
{
    public const int HugeEventLength = 64 * 1024;

    private static readonly TimeSpan Pace = TimeSpan.FromMilliseconds(300);

    private readonly WebApplication app;

    private SampleServer(
        WebApplication app,
        Uri baseAddress)
    {
        this.app = app;
        BaseAddress = baseAddress;
    }

    public Uri BaseAddress { get; }

    public static async Task<SampleServer> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, port: 0));

        var application = builder.Build();
        application.MapGet("/prices", WritePricesAsync);
        application.MapGet("/prices/unavailable", WriteUnavailableAsync);
        application.MapGet("/prices/huge", WriteHugeAsync);

        await application.StartAsync().ConfigureAwait(false);

        return new SampleServer(application, new Uri(application.Urls.First()));
    }

    public ValueTask DisposeAsync()
        => app.DisposeAsync();

    /// <summary>
    /// A price every 300 ms. With <c>noise=true</c> the stream also carries what a real one does besides the
    /// items: a comment as keep-alive and a named heartbeat event.
    /// </summary>
    private static async Task WritePricesAsync(HttpContext context)
    {
        var count = int.Parse(context.Request.Query["count"].ToString(), CultureInfo.InvariantCulture);
        var noise = bool.Parse(context.Request.Query["noise"].ToString());

        context.Response.ContentType = "text/event-stream";

        try
        {
            for (var sequence = 1; sequence <= count; sequence++)
            {
                if (noise)
                {
                    await WriteAsync(context, ": keep-alive\n\n").ConfigureAwait(false);
                    await WriteAsync(context, "event: ping\ndata: ping\n\n").ConfigureAwait(false);
                }

                var tick = new PriceTick("ATC", 100m + (sequence * 0.25m), sequence);
                var json = JsonSerializer.Serialize(tick, SampleJsonContext.Default.PriceTick);
                await WriteAsync(context, $"id: {sequence}\ndata: {json}\n\n").ConfigureAwait(false);

                await Task.Delay(Pace, context.RequestAborted).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The client stopped reading.
        }
    }

    private static Task WriteUnavailableAsync(HttpContext context)
    {
        var problem = new Problem("Prices are unavailable", 503, "The exchange is closed.");

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.RetryAfter = "30";

        return WriteAsync(context, JsonSerializer.Serialize(problem, SampleJsonContext.Default.Problem));
    }

    /// <summary>
    /// One ordinary event, then one of 64 KiB - more than the client of the sample accepts.
    /// </summary>
    private static async Task WriteHugeAsync(HttpContext context)
    {
        context.Response.ContentType = "text/event-stream";

        var tick = new PriceTick("ATC", 100.25m, 1);
        var huge = tick with { Symbol = new string('A', HugeEventLength), Sequence = 2 };

        await WriteAsync(context, $"data: {JsonSerializer.Serialize(tick, SampleJsonContext.Default.PriceTick)}\n\n").ConfigureAwait(false);
        await WriteAsync(context, $"data: {JsonSerializer.Serialize(huge, SampleJsonContext.Default.PriceTick)}\n\n").ConfigureAwait(false);
    }

    private static async Task WriteAsync(
        HttpContext context,
        string text)
    {
        await context.Response.WriteAsync(text, context.RequestAborted).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
    }
}