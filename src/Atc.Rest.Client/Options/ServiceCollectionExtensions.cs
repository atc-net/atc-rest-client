namespace Atc.Rest.Client.Options;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Atc.Rest.Client services (IHttpMessageFactory and IContractSerializer)
    /// without HttpClient configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAtcRestClient(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IContractSerializer, DefaultJsonContractSerializer>();
        services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
        return services;
    }

    /// <summary>
    /// Registers the core Atc.Rest.Client services with a custom contract serializer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="contractSerializer">The custom contract serializer to use.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contractSerializer"/> is null.</exception>
    public static IServiceCollection AddAtcRestClient(
        this IServiceCollection services,
        IContractSerializer contractSerializer)
    {
        if (contractSerializer is null)
        {
            throw new ArgumentNullException(nameof(contractSerializer));
        }

        services.TryAddSingleton(contractSerializer);
        services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
        return services;
    }

    /// <summary>
    /// Registers the core Atc.Rest.Client services with configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for AtcRestClientOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddAtcRestClient(
        this IServiceCollection services,
        Action<AtcRestClientOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new AtcRestClientOptions();
        configure(options);

        services.TryAddSingleton<IContractSerializer, DefaultJsonContractSerializer>();
        services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
        return services;
    }

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

        // Register utilities
        services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
        if (contractSerializer is null)
        {
            services.TryAddSingleton<IContractSerializer, DefaultJsonContractSerializer>();
        }
        else
        {
            services.TryAddSingleton(contractSerializer);
        }

        return services;
    }

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

        // Register utilities
        services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
        if (contractSerializer is null)
        {
            services.TryAddSingleton<IContractSerializer, DefaultJsonContractSerializer>();
        }
        else
        {
            services.TryAddSingleton(contractSerializer);
        }

        return services;
    }
}