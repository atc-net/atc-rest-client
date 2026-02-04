namespace Atc.Rest.Client.Options;

/// <summary>
/// Configuration options for the ATC REST client.
/// </summary>
public class AtcRestClientOptions
{
    /// <summary>
    /// Gets or sets the base address of the HTTP client.
    /// </summary>
    public virtual Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests. Defaults to 30 seconds.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}