namespace Atc.Rest.Client.Options;

/// <summary>
/// Configuration options for the ATC REST client.
/// </summary>
public class AtcRestClientOptions
{
    /// <summary>
    /// The default of <see cref="MaxStreamRecordLength"/>: 16 MiB.
    /// </summary>
    public const int DefaultMaxStreamRecordLength = 16 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the base address of the HTTP client.
    /// </summary>
    public virtual Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests. Defaults to 30 seconds.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the longest record of a streamed response that is read, in bytes: one line (JSON Lines), one
    /// record (JSON text sequence), one event (server-sent events), one part (multipart/mixed) or one element
    /// (JSON array). A longer one fails the enumeration with an <see cref="InvalidDataException"/> instead of
    /// being buffered. Defaults to 16 MiB.
    /// </summary>
    /// <remarks>
    /// The limit is one per service collection, not one per named client: <c>IHttpMessageFactory</c>, which reads
    /// it, is shared by all clients. <c>AddAtcRestClient</c> applies the value of its options when it is not the
    /// default, so when several clients set different values, the last one registered applies to all of them. It can
    /// also be set with <c>services.Configure&lt;AtcRestClientOptions&gt;(o =&gt; o.MaxStreamRecordLength = ...)</c>, and
    /// for one call with <see cref="StreamReadOptions.MaxRecordLength"/>.
    /// </remarks>
    public virtual int MaxStreamRecordLength { get; set; } = DefaultMaxStreamRecordLength;
}