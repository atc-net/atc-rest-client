namespace Atc.Rest.Client.Builder;

/// <summary>
/// A message request builder can be used to build a <see cref="HttpRequestMessage"/>.
/// </summary>
public interface IMessageRequestBuilder
{
    /// <summary>
    /// Adds a value to a path parameter in a path template passed to the constructor of the <see cref="IMessageRequestBuilder"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="IMessageRequestBuilder"/> implementation is expected to get passed a path template with
    /// optional path parameters inside, which this method will replace with the <paramref name="value"/>.
    /// </remarks>
    /// <param name="name">Name of the path parameter in the template path.</param>
    /// <param name="value">Value to use as the path parameter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithPathParameter(
        string name,
        object? value);

    /// <summary>
    /// Adds a value to a header parameter in the headers.
    /// </summary>
    /// <param name="name">Name of the header parameter.</param>
    /// <param name="value">Value to use as the header parameter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithHeaderParameter(
        string name,
        object? value);

    /// <summary>
    /// Adds a query parameter to the created request URL.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="value"/> is null, the query parameter is not added.
    /// </remarks>
    /// <param name="name">Name of the query parameter.</param>
    /// <param name="value">Value of the query parameter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithQueryParameter(
        string name,
        string? value);

    /// <summary>
    /// Adds a query parameter with multiple values to the created request URL.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="values"/> is null or empty, the query parameter is not added.
    /// Each value in the collection will be added as a separate query parameter with the same name.
    /// </remarks>
    /// <param name="name">Name of the query parameter.</param>
    /// <param name="values">Collection of values for the query parameter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithQueryParameter(
        string name,
        IEnumerable? values);

    /// <summary>
    /// Adds a query parameter to the created request URL.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="value"/> is null, the query parameter is not added.
    /// Supports special handling for enums (using EnumMemberAttribute), DateTime, and DateTimeOffset.
    /// </remarks>
    /// <param name="name">Name of the query parameter.</param>
    /// <param name="value">Value of the query parameter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithQueryParameter(
        string name,
        object? value);

    /// <summary>
    /// Adds the body of the request.
    /// </summary>
    /// <remarks>
    /// The builder should use a <see cref="IContractSerializer"/> to serialize <paramref name="body"/>.
    /// A file body (<see cref="IFileContent"/>) is sent as multipart form data, and a <see cref="Stream"/> body is
    /// sent as-is with <c>application/octet-stream</c>, as <see cref="WithBinaryBody"/> sends it.
    /// </remarks>
    /// <typeparam name="TBody">The type of object to add as the body of the request.</typeparam>
    /// <param name="body">The body to add to the request.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithBody<TBody>(TBody body);

    /// <summary>
    /// Adds the body of the request, sent with the given media type instead of <c>application/json</c>.
    /// </summary>
    /// <remarks>
    /// Use it for a JSON media type such as <c>application/merge-patch+json</c> or <c>application/vnd.x+json</c>.
    /// The body is serialized with the builder's <see cref="IContractSerializer"/>. A file body
    /// (<see cref="IFileContent"/>) is still sent as multipart form data, and <paramref name="contentType"/> is ignored.
    /// A <see cref="Stream"/> body is sent as-is with <paramref name="contentType"/>, as <see cref="WithBinaryBody"/> sends it.
    /// </remarks>
    /// <typeparam name="TBody">The type of object to add as the body of the request.</typeparam>
    /// <param name="body">The body to add to the request.</param>
    /// <param name="contentType">The media type of the serialized body.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithBody<TBody>(
        TBody body,
        string contentType);

    /// <summary>
    /// Adds a binary stream as the body of the request.
    /// </summary>
    /// <remarks>
    /// Use this method for raw binary uploads (e.g., application/octet-stream).
    /// The stream is sent directly without serialization.
    /// </remarks>
    /// <param name="stream">The stream to send as the request body.</param>
    /// <param name="contentType">Optional content type. Defaults to application/octet-stream.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithBinaryBody(
        Stream stream,
        string? contentType = null);

    /// <summary>
    /// Builds a <see cref="HttpRequestMessage"/> with the added content.
    /// </summary>
    /// <param name="method">The <see cref="HttpMethod"/> to use in the request.</param>
    /// <returns>The created <see cref="HttpRequestMessage"/>.</returns>
    HttpRequestMessage Build(HttpMethod method);

    /// <summary>
    /// Sets the HTTP completion option for the request.
    /// Use <see cref="HttpCompletionOption.ResponseHeadersRead"/> for streaming scenarios.
    /// </summary>
    /// <param name="completionOption">The HTTP completion option.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithHttpCompletionOption(
        HttpCompletionOption completionOption);

    /// <summary>
    /// Gets the HTTP completion option set for this request.
    /// </summary>
    HttpCompletionOption HttpCompletionOption { get; }

    /// <summary>
    /// Adds a file to the multipart form data content.
    /// </summary>
    /// <param name="stream">The file stream to upload.</param>
    /// <param name="name">The form field name.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">Optional content type. Defaults to application/octet-stream.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithFile(
        Stream stream,
        string name,
        string fileName,
        string? contentType = null);

    /// <summary>
    /// Adds multiple files to the multipart form data content.
    /// </summary>
    /// <param name="files">Collection of files with Stream, Name, FileName, and optional ContentType.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithFiles(
        IEnumerable<(Stream Stream, string Name, string FileName, string? ContentType)> files);

    /// <summary>
    /// Adds a form field to the multipart form data content.
    /// Each call adds a part, so a repeated name sends one part per value, in call order.
    /// </summary>
    /// <param name="name">The form field name.</param>
    /// <param name="value">The form field value.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithFormField(
        string name,
        string value);

    /// <summary>
    /// Adds a field to an <c>application/x-www-form-urlencoded</c> request body.
    /// </summary>
    /// <remarks>
    /// A repeated name sends one field per value, in call order (how a list is encoded).
    /// URL-encoded fields cannot be combined with another request body; <see cref="Build"/> then throws an
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <param name="name">The form field name.</param>
    /// <param name="value">The form field value.</param>
    /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
    IMessageRequestBuilder WithUrlEncodedFormField(
        string name,
        string value);
}