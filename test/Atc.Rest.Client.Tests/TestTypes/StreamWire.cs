namespace Atc.Rest.Client.Tests.TestTypes;

// What a server writes for a stream of JSON records in each framing.
internal static class StreamWire
{
    public const string Boundary = "part-boundary";

    public static TheoryData<StreamFraming> Framings
    {
        get
        {
            var data = new TheoryData<StreamFraming>();
            foreach (var framing in Enum.GetValues<StreamFraming>())
            {
                data.Add(framing);
            }

            return data;
        }
    }

    public static string ContentType(StreamFraming framing)
        => framing switch
        {
            StreamFraming.JsonArray => "application/json",
            StreamFraming.JsonLines => "application/jsonl",
            StreamFraming.JsonSequence => "application/json-seq",
            StreamFraming.ServerSentEvents => "text/event-stream",
            StreamFraming.MultipartMixed => $"multipart/mixed; boundary={Boundary}",
            _ => throw new ArgumentOutOfRangeException(nameof(framing), framing, message: null),
        };

    // What comes before the first record.
    public static string Open(StreamFraming framing)
        => framing == StreamFraming.JsonArray ? "[" : string.Empty;

    // One record; once it is written, the reader has all it needs to yield the item.
    public static string Record(
        StreamFraming framing,
        string json,
        bool isLast = false)
        => framing switch
        {
            StreamFraming.JsonArray => isLast ? json : json + ",",
            StreamFraming.JsonLines => json + "\n",
            StreamFraming.JsonSequence => "\u001e" + json + "\n",
            StreamFraming.ServerSentEvents => "data: " + json + "\n\n",
            StreamFraming.MultipartMixed => $"--{Boundary}\r\nContent-Type: application/json\r\n\r\n{json}\r\n",
            _ => throw new ArgumentOutOfRangeException(nameof(framing), framing, message: null),
        };

    // What comes after the last record.
    public static string Close(StreamFraming framing)
        => framing switch
        {
            StreamFraming.JsonArray => "]",
            StreamFraming.MultipartMixed => $"--{Boundary}--\r\n",
            _ => string.Empty,
        };

    // The start of a record that the peer never ends.
    public static string OpenRecord(StreamFraming framing)
        => framing switch
        {
            StreamFraming.JsonArray => "[\"",
            StreamFraming.JsonLines => "\"",
            StreamFraming.JsonSequence => "\u001e\"",
            StreamFraming.ServerSentEvents => "data: \"",
            StreamFraming.MultipartMixed => $"--{Boundary}\r\n\r\n\"",
            _ => throw new ArgumentOutOfRangeException(nameof(framing), framing, message: null),
        };

    // The bytes of a record that count towards the limit besides its JSON.
    public static int RecordOverhead(StreamFraming framing)
        => framing == StreamFraming.ServerSentEvents ? "data: ".Length : 0;

    public static string Document(
        StreamFraming framing,
        params string[] records)
    {
        var wire = new StringBuilder(Open(framing));
        for (var i = 0; i < records.Length; i++)
        {
            wire.Append(Record(framing, records[i], isLast: i == records.Length - 1));
        }

        return wire.Append(Close(framing)).ToString();
    }

    public static HttpResponseMessage Response(
        StreamFraming framing,
        Stream body,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StreamContent(body),
        };
        response.Content.Headers.TryAddWithoutValidation("Content-Type", ContentType(framing));
        return response;
    }

    public static HttpResponseMessage Response(
        StreamFraming framing,
        string body)
        => Response(framing, FeedStream.FromText(body));
}