using System;
using System.Net.Http;
using Atc.Rest.Client.Serialization;

namespace Atc.Rest.Client.Builder
{
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
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
        /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
        IMessageRequestBuilder WithPathParameter(string name, string value);

        /// <summary>
        /// Adds a value to a path parameter in a path template passed to the constructor of the <see cref="IMessageRequestBuilder"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="IMessageRequestBuilder"/> implementation is expected to get passed a path template with
        /// optional path parameters inside, which this method will replace with the <paramref name="value"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        /// <param name="name">Name of the path parameter in the template path.</param>
        /// <param name="value">Value to use as the path parameter. It will be converted to a string
        /// before being added as a query string, using the <c>ToString()</c> method.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
        /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
        IMessageRequestBuilder WithPathParameter<T>(string name, T value)
            where T : struct;

        /// <summary>
        /// Adds a query parameter to the created request URL.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="value"/> is null, the query parameter is not added.
        /// </remarks>
        /// <param name="name">Name of the query parameter.</param>
        /// <param name="value">Value of the query parameter.</param>
        /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
        IMessageRequestBuilder WithQueryParameter(string name, string? value);

        /// <summary>
        /// Adds a query parameter to the created request URL.
        /// </summary>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        /// <param name="name">Name of the query parameter.</param>
        /// <param name="value">Value of the query parameter. It will be converted to a string
        /// before being added as a query string, using the <c>ToString()</c> method.</param>
        /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
        IMessageRequestBuilder WithQueryParameter<T>(string name, T value)
            where T : struct;

        /// <summary>
        /// Adds the body of the request.
        /// </summary>
        /// <remarks>
        /// The builder should use a <see cref="IContractSerializer"/> to serialize <paramref name="body"/>.
        /// </remarks>
        /// <typeparam name="TBody">The type of object to add as the body of the request.</typeparam>
        /// <param name="body">The body to add to the request.</param>
        /// <returns>The <see cref="IMessageRequestBuilder"/>.</returns>
        IMessageRequestBuilder WithBody<TBody>(TBody body);

        /// <summary>
        /// Builds a <see cref="HttpRequestMessage"/> with the added content.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/> to use in the request.</param>
        /// <returns>The created <see cref="HttpRequestMessage"/>.</returns>
        HttpRequestMessage Build(HttpMethod method);
    }
}