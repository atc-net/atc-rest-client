namespace Atc.Rest.Client.Builder;

/// <summary>
/// Represents an HTTP message factory that can create both the <see cref="IMessageRequestBuilder"/>
/// and <see cref="IMessageRequestBuilder"/>, used to provide input to
/// and processes responses from an HTTP exchange.
/// </summary>
public interface IHttpMessageFactory
{
    /// <summary>
    /// Start building a <see cref="HttpRequestMessage"/> with the
    /// returned <see cref="IMessageRequestBuilder"/>, which will use the provided
    /// <paramref name="pathTemplate"/> as the request URI.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pathTemplate"/> is null.</exception>
    /// <param name="pathTemplate">The relative URI to request. Can contain tokens,
    /// that will be replaced with real values passed to the <see cref="IMessageRequestBuilder.WithPathParameter(string, object?)"/>
    /// method.</param>
    /// <returns>A new <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder FromTemplate(
        string pathTemplate);

    IMessageResponseBuilder FromResponse(
        HttpResponseMessage? response);
}