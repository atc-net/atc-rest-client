namespace Atc.Rest.Client.Options;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Atc.Rest.Client services (IHttpMessageFactory and IContractSerializer)
    /// without HttpClient configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="contractSerializer">Optional custom contract serializer. If null, uses DefaultJsonContractSerializer.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAtcRestClientCore(
        this IServiceCollection services,
        IContractSerializer? contractSerializer = null)
    {
        if (contractSerializer is null)
        {
            services.TryAddSingleton<IContractSerializer, DefaultJsonContractSerializer>();
        }
        else
        {
            services.TryAddSingleton(contractSerializer);
        }

        services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
        return services;
    }

    /// <summary>
    /// Registers a named HttpClient with the specified options and core Atc.Rest.Client services.
    /// </summary>
    /// <typeparam name="TOptions">The type of options, must inherit from <see cref="AtcRestClientOptions"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the HttpClient to register.</param>
    /// <param name="options">The options containing BaseAddress and Timeout configuration.</param>
    /// <param name="httpClientBuilder">Optional action to further configure the HttpClient.</param>
    /// <param name="contractSerializer">Optional custom contract serializer. If null, uses DefaultJsonContractSerializer.</param>
    /// <returns>The service collection for chaining.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddAtcRestClient<TOptions>(
        this IServiceCollection services,
        string clientName,
        TOptions options,
        Action<IHttpClientBuilder>? httpClientBuilder = null,
        IContractSerializer? contractSerializer = null)
        where TOptions : AtcRestClientOptions, new()
    {
        var clientBuilder = services.AddHttpClient(clientName, (_, c) =>
        {
            c.BaseAddress = options.BaseAddress;
            c.Timeout = options.Timeout;
        });

        httpClientBuilder?.Invoke(clientBuilder);

        return services.AddAtcRestClientCore(contractSerializer);
    }

    /// <summary>
    /// Registers a named HttpClient with the specified base address, timeout, and core Atc.Rest.Client services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the HttpClient to register.</param>
    /// <param name="baseAddress">The base address for the HttpClient.</param>
    /// <param name="timeout">The timeout for the HttpClient.</param>
    /// <param name="httpClientBuilder">Optional action to further configure the HttpClient.</param>
    /// <param name="contractSerializer">Optional custom contract serializer. If null, uses DefaultJsonContractSerializer.</param>
    /// <returns>The service collection for chaining.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddAtcRestClient(
        this IServiceCollection services,
        string clientName,
        Uri baseAddress,
        TimeSpan timeout,
        Action<IHttpClientBuilder>? httpClientBuilder = null,
        IContractSerializer? contractSerializer = null)
    {
        var clientBuilder = services.AddHttpClient(clientName, (_, c) =>
        {
            c.BaseAddress = baseAddress;
            c.Timeout = timeout;
        });

        httpClientBuilder?.Invoke(clientBuilder);

        return services.AddAtcRestClientCore(contractSerializer);
    }
}