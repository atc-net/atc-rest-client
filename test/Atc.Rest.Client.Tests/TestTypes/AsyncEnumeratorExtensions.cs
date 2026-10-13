namespace Atc.Rest.Client.Tests.TestTypes;

internal static class AsyncEnumeratorExtensions
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    // Moves to the next item, and fails instead of hanging the test run when none arrives.
    public static Task<bool> NextAsync<T>(this IAsyncEnumerator<T> enumerator)
        => enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout);
}