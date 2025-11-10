namespace Atc.Rest.Client.Options;

public static class ServiceCollectionExtensions
{
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