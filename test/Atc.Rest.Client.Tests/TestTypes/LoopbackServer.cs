namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// A HTTP/1.1 server on a loopback socket that answers one request. The test writes the response itself, write by
/// write, so it decides what is on the wire when the client reads.
/// </summary>
internal sealed class LoopbackServer : IAsyncDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource stopping = new();
    private readonly Task serving;

    private LoopbackServer(
        Func<LoopbackResponse, CancellationToken, Task> respond)
    {
        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        Uri = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        serving = ServeAsync(respond);
    }

    public Uri Uri { get; }

    /// <summary>
    /// Completes when the response has been written and the connection is closed.
    /// </summary>
    public Task Completion => serving;

    public static LoopbackServer Start(
        Func<LoopbackResponse, CancellationToken, Task> respond)
        => new(respond);

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        listener.Dispose();

        try
        {
            await serving;
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException)
        {
            // The test ended before the response did, or the client closed the connection first.
        }

        stopping.Dispose();
    }

    private static async Task ReadRequestHeadAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var matched = 0;
        while (matched < 4)
        {
            if (await stream.ReadAsync(buffer, cancellationToken) == 0)
            {
                return;
            }

            var expected = matched % 2 == 0 ? (byte)'\r' : (byte)'\n';
            matched = buffer[0] == expected
                ? matched + 1
                : buffer[0] == (byte)'\r' ? 1 : 0;
        }
    }

    private async Task ServeAsync(
        Func<LoopbackResponse, CancellationToken, Task> respond)
    {
        using var client = await listener.AcceptTcpClientAsync(stopping.Token);
        client.NoDelay = true;

        var stream = client.GetStream();
        await ReadRequestHeadAsync(stream, stopping.Token);
        await respond(new LoopbackResponse(stream), stopping.Token);
    }
}