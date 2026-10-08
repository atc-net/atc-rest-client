#if !NET
namespace Atc.Rest.Client;

/// <summary>
/// The CancellationToken overloads of the HttpContent read methods exist in .NET 5+ but not in netstandard2.0.
/// Here the token is only checked before the read starts.
/// </summary>
internal static class HttpContentExtensions
{
    public static Task<string> ReadAsStringAsync(
        this HttpContent content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return content.ReadAsStringAsync();
    }

    public static Task<byte[]> ReadAsByteArrayAsync(
        this HttpContent content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return content.ReadAsByteArrayAsync();
    }

    public static Task<Stream> ReadAsStreamAsync(
        this HttpContent content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return content.ReadAsStreamAsync();
    }
}
#endif