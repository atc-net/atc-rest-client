namespace Showcase.BlazorApp.Pages;

[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A page of a showcase shows what went wrong instead of the error bar of the framework.")]
public sealed partial class Home : IDisposable
{
    private const int LogLength = 12;
    private const int HugeLimit = 1024;
    private const int MaxReconnectsInARow = 5;

    private static readonly TimeSpan DefaultRetry = TimeSpan.FromSeconds(3);

    private static readonly StreamFraming[] Framings =
    [
        StreamFraming.ServerSentEvents,
        StreamFraming.JsonLines,
        StreamFraming.JsonSequence,
        StreamFraming.MultipartMixed,
        StreamFraming.JsonArray,
    ];

    private readonly Dictionary<string, Quote> quotes = new(StringComparer.Ordinal);
    private readonly List<LogEntry> log = [];
    private readonly List<string> hugeOutcome = [];

    private CancellationTokenSource? streaming;
    private StreamFraming framing = StreamFraming.ServerSentEvents;
    private bool isStreaming;
    private bool isLive;
    private bool isBusy;
    private string status = "Not started";
    private string? contentType;
    private int itemCount;
    private decimal elapsedSeconds;
    private string? lastEventId;
    private string? eventName;
    private TimeSpan? retry;
    private int reconnects;
    private int reconnectsInARow;
    private bool isResumed;
    private Failure? failure;

    [Inject]
    public required PriceStreamClient Prices { get; set; }

    public void Dispose()
    {
        streaming?.Cancel();
        streaming?.Dispose();
    }

    private static string Describe(StreamFraming framing)
        => framing switch
        {
            StreamFraming.ServerSentEvents => "Server-sent events (text/event-stream)",
            StreamFraming.JsonLines => "JSON Lines (application/jsonl)",
            StreamFraming.JsonSequence => "JSON text sequence (application/json-seq)",
            StreamFraming.MultipartMixed => "Multipart (multipart/mixed)",
            _ => "JSON array (application/json)",
        };

    private static string Format(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private async Task StartAsync()
    {
        streaming?.Dispose();
        streaming = new CancellationTokenSource();
        var cancellationToken = streaming.Token;

        Reset();
        isStreaming = true;
        status = "Connecting";

        var clock = Stopwatch.StartNew();

        try
        {
            while (true)
            {
                try
                {
                    await ReadAsync(clock, cancellationToken);
                    return;
                }
                catch (Exception ex) when (CanResume(ex, cancellationToken))
                {
                    // The library does not reconnect by itself: when, and how often, is for the app to decide.
                    // It waits as long as the server asked for with retry:
                    var wait = retry ?? DefaultRetry;

                    reconnectsInARow++;
                    reconnects++;
                    isResumed = true;
                    isLive = false;
                    status = $"Connection lost after event {lastEventId} - connecting again in {Format((decimal)wait.TotalSeconds)} s";
                    StateHasChanged();

                    await Task.Delay(wait, cancellationToken);
                }
            }
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested || ex is OperationCanceledException)
        {
            status = $"Stopped after {itemCount} items";
        }
        catch (Exception ex)
        {
            // The server went away, a record was no JSON or past the limit, ...
            status = $"Failed: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            isStreaming = false;
            isLive = false;
        }
    }

    private void Reset()
    {
        quotes.Clear();
        log.Clear();
        itemCount = 0;
        elapsedSeconds = 0;
        contentType = null;
        lastEventId = null;
        eventName = null;
        retry = null;
        reconnects = 0;
        reconnectsInARow = 0;
        isResumed = false;
    }

    // One response, read to its end - from the start, or after the last event of the response before it.
    private async Task ReadAsync(
        Stopwatch clock,
        CancellationToken cancellationToken)
    {
        // Disposing the result ends the stream: the browser aborts the request
        using var result = await Prices.StreamAsync(framing, lastEventId, Show, cancellationToken);
        contentType = result.ContentType;

        if (!result.IsSuccess)
        {
            status = $"Failed: {(int)result.StatusCode} {result.StatusCode}";
            return;
        }

        status = isResumed ? $"Live - resumed after event {lastEventId}" : "Live";
        isLive = true;
        StateHasChanged();

        await foreach (var tick in result.Content!.WithCancellation(cancellationToken))
        {
            // Where to continue, should the connection be lost after this item
            lastEventId = result.LastEventId ?? lastEventId;
            reconnectsInARow = 0;

            if (tick is not null)
            {
                Show(tick, clock.ElapsedMilliseconds);
                StateHasChanged();
            }
        }

        status = "The server ended the stream";
    }

    // A stream can be continued when it has ids - of the five framings only server-sent events have - and it was
    // the connection that failed, not a record that was refused.
    private bool CanResume(
        Exception exception,
        CancellationToken cancellationToken)
        => !cancellationToken.IsCancellationRequested &&
           exception is not (OperationCanceledException or InvalidDataException or System.Text.Json.JsonException) &&
           framing == StreamFraming.ServerSentEvents &&
           !string.IsNullOrEmpty(lastEventId) &&
           reconnectsInARow < MaxReconnectsInARow;

    private void Stop()
        => streaming?.Cancel();

    private async Task DropAsync()
    {
        try
        {
            await Prices.DropConnectionsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            status = $"Failed: {ex.GetType().Name}: {ex.Message}";
        }
    }

    // StreamReadOptions.OnEvent: what an event says besides its data, just before its item is yielded
    private void Show(ServerSentEventInfo info)
    {
        eventName = info.EventName ?? "(unnamed)";
        retry = info.Retry;
    }

    private void Show(
        PriceTick tick,
        long elapsedMilliseconds)
    {
        var ticks = quotes.TryGetValue(tick.Symbol, out var previous) ? previous.Ticks + 1 : 1;
        quotes[tick.Symbol] = new Quote(tick.Symbol, tick.Price, tick.Change, ticks);

        log.Insert(0, new LogEntry(elapsedMilliseconds, tick, isResumed));
        isResumed = false;
        if (log.Count > LogLength)
        {
            log.RemoveAt(log.Count - 1);
        }

        itemCount++;
        elapsedSeconds = elapsedMilliseconds / 1000m;
    }

    private async Task CallUnavailableAsync()
    {
        isBusy = true;
        failure = null;

        try
        {
            using var result = await Prices.StreamUnavailableAsync(CancellationToken.None);

            failure = result.IsServiceUnavailable
                ? new Failure(
                    $"{(int)result.StatusCode} {result.StatusCode}",
                    result.ServiceUnavailableContent.Title,
                    result.ServiceUnavailableContent.Detail,
                    $"{result.RetryAfter?.TotalSeconds} s")
                : new Failure($"{(int)result.StatusCode} {result.StatusCode}", "Not the 503 that was expected", result.ErrorContent ?? string.Empty, "–");
        }
        catch (Exception ex)
        {
            failure = new Failure("No response", ex.GetType().Name, ex.Message, "–");
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task ReadHugeAsync()
    {
        isBusy = true;
        hugeOutcome.Clear();

        try
        {
            using var result = await Prices.StreamHugeAsync(HugeLimit, CancellationToken.None);

            await foreach (var tick in result.Content!)
            {
                hugeOutcome.Add($"Item #{tick?.Sequence}: {tick?.Symbol} {Format(tick?.Price ?? 0)}");
            }
        }
        catch (Exception ex)
        {
            // An InvalidDataException that names the limit is what this demo is about
            hugeOutcome.Add($"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            isBusy = false;
        }
    }

    private sealed record Failure(
        string StatusCode,
        string Title,
        string Detail,
        string RetryAfter);
}