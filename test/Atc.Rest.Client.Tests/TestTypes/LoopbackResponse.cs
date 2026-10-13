namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// The response of a <see cref="LoopbackServer"/>: every write is flushed to the socket before it returns.
/// The body ends when the connection is closed.
/// </summary>
internal sealed class LoopbackResponse
{
    private readonly NetworkStream stream;
    private long bytesWritten;

    public LoopbackResponse(NetworkStream stream)
    {
        this.stream = stream;
    }

    /// <summary>
    /// Gets the number of body bytes written to the socket so far.
    /// </summary>
    public long BodyBytesWritten => Interlocked.Read(ref bytesWritten);

    public Task WriteHeadAsync(
        HttpStatusCode statusCode,
        string contentType,
        params (string Name, string Value)[] headers)
    {
        var head = new StringBuilder();
        head.Append(CultureInfo.InvariantCulture, $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n");
        if (contentType is not null)
        {
            head.Append(CultureInfo.InvariantCulture, $"Content-Type: {contentType}\r\n");
        }

        foreach (var (name, value) in headers)
        {
            head.Append(CultureInfo.InvariantCulture, $"{name}: {value}\r\n");
        }

        head.Append("Connection: close\r\n\r\n");

        return WriteToSocketAsync(Encoding.ASCII.GetBytes(head.ToString()));
    }

    // Completes when the client has closed the connection (or sent more, which a test client does not).
    public async Task WaitForDisconnectAsync(
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        try
        {
            while (await stream.ReadAsync(buffer, cancellationToken) > 0)
            {
                // Nothing is expected; read on until the connection ends.
            }
        }
        catch (IOException)
        {
            // A reset connection is a closed connection.
        }
    }

    public Task WriteAsync(string text)
        => WriteAsync(Encoding.UTF8.GetBytes(text));

    public async Task WriteAsync(byte[] bytes)
    {
        await WriteToSocketAsync(bytes);
        Interlocked.Add(ref bytesWritten, bytes.Length);
    }

    private async Task WriteToSocketAsync(byte[] bytes)
    {
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }
}