namespace Atc.Rest.Client;

/// <summary>
/// Reads a server-sent events stream (<c>text/event-stream</c>) for a caller that has the stream and a deserializer
/// of its own - a generated or hand-written client that does not go through <see cref="IMessageResponseBuilder"/>.
/// </summary>
public static class ServerSentEventReader
{
    /// <summary>
    /// Reads the items of an event stream and keeps the id of the last event read in the response it returns.
    /// </summary>
    /// <remarks>
    /// It is the reader behind <see cref="StreamFraming.ServerSentEvents"/> of <see cref="IMessageResponseBuilder"/>,
    /// with the same rules: which event is an item, what is read past, and when
    /// <see cref="StreamingEndpointResponse{T}.LastEventId"/> changes. The caller has decided that the response is a
    /// stream of items, so the response returned is a success with the status code and the headers of
    /// <paramref name="response"/>. Nothing is read before the items are enumerated.
    /// There are no <c>AtcRestClientOptions</c> here: without <see cref="StreamReadOptions.MaxRecordLength"/>, one
    /// event is limited to <see cref="AtcRestClientOptions.DefaultMaxStreamRecordLength"/> bytes.
    /// </remarks>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="response">The HTTP response the stream is of. The response returned owns it and disposes it.</param>
    /// <param name="stream">The content of the response, read as the items are enumerated.</param>
    /// <param name="deserialize">
    /// Deserializes the data of one event - its JSON - to an item. An exception it throws fails the enumeration with
    /// that exception.
    /// </param>
    /// <param name="options">The options for reading the stream, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">The cancellation token for the enumeration.</param>
    /// <returns>A streaming response whose <see cref="StreamingEndpointResponse{T}.LastEventId"/> follows the enumeration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/>, <paramref name="stream"/> or <paramref name="deserialize"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="StreamReadOptions.MaxRecordLength"/> is not greater than zero.
    /// </exception>
    public static StreamingEndpointResponse<T> Read<T>(
        HttpResponseMessage response,
        Stream stream,
        Func<string, T?> deserialize,
        StreamReadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (deserialize is null)
        {
            throw new ArgumentNullException(nameof(deserialize));
        }

        var maxRecordLength = StreamRecordLimit.Validate(options?.MaxRecordLength ?? AtcRestClientOptions.DefaultMaxStreamRecordLength);
        var eventState = new ServerSentEventState();

        var content = StreamRecordReaders.ReadServerSentEventsAsync(
            stream,
            json => deserialize(Encoding.UTF8.GetString(json)),
            maxRecordLength,
            options?.ItemEventNames,
            options?.OnEvent,
            eventState,
            cancellationToken);

        return new StreamingEndpointResponse<T>(
            isSuccess: true,
            response.StatusCode,
            content,
            errorContent: null,
            httpResponse: response,
            errorContentObject: null,
            MessageResponseBuilder.GetHeaders(response),
            eventState);
    }
}