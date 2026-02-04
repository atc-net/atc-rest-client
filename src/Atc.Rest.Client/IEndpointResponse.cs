namespace Atc.Rest.Client;

public interface IEndpointResponse
{
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the status code indicates OK (200).
    /// </summary>
    bool IsOk { get; }

    HttpStatusCode StatusCode { get; }

    string Content { get; }

    object? ContentObject { get; }

    IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
}