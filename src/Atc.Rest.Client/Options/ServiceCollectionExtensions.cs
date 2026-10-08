namespace Atc.Rest.Client.Options;

[SuppressMessage("", "CA1034:Do not nest type", Justification = "OK - CLang14 - extension")]
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the core Atc.Rest.Client services (IHttpMessageFactory and IContractSerializer)
        /// without HttpClient configuration.
        /// </summary>
        /// <param name="contractSerializer">Optional custom contract serializer. If null, uses DefaultJsonContractSerializer.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddAtcRestClientCore(
            IContractSerializer? contractSerializer = null)
        {
            if (contractSerializer is null)
            {
                services.AddOptions();
                services.TryAddSingleton(CreateDefaultContractSerializer);
            }
            else
            {
                services.TryAddSingleton(contractSerializer);
            }

            services.TryAddSingleton<IHttpMessageFactory, HttpMessageFactory>();
            return services;
        }

        /// <summary>
        /// Adjusts the JSON serializer options of the default <see cref="IContractSerializer"/> and registers the core
        /// Atc.Rest.Client services (IHttpMessageFactory and IContractSerializer) if they are not registered yet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every call adds a configure step. When the default serializer is first resolved, its options are created by
        /// <see cref="DefaultJsonContractSerializer.CreateDefaultOptions"/> and then adjusted by every configure step, in
        /// registration order. Several libraries can each contribute their converters and
        /// <see cref="JsonSerializerContext"/> without overriding each other.
        /// </para>
        /// <para>
        /// For trimmed and Native AOT apps, add the app's <see cref="JsonSerializerContext"/>:
        /// <c>services.ConfigureAtcRestClientJsonOptions(o => o.TypeInfoResolverChain.Insert(0, MyJsonContext.Default));</c>
        /// </para>
        /// <para>
        /// The first registration of <see cref="IContractSerializer"/> wins: the configure steps are ignored when a
        /// serializer instance is registered with <see cref="AddAtcRestClientCore(IServiceCollection, IContractSerializer?)"/>
        /// before. They are also ignored when a <see cref="JsonSerializerOptions"/> instance is registered in the service
        /// collection, because the default serializer then uses that instance.
        /// </para>
        /// </remarks>
        /// <param name="configureJsonSerializerOptions">Configures the JSON serializer options of the default serializer.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection ConfigureAtcRestClientJsonOptions(
            Action<JsonSerializerOptions> configureJsonSerializerOptions)
        {
            if (configureJsonSerializerOptions is null)
            {
                throw new ArgumentNullException(nameof(configureJsonSerializerOptions));
            }

            services.Configure<AtcRestClientJsonOptions>(o => configureJsonSerializerOptions(o.JsonSerializerOptions));

            return services.AddAtcRestClientCore();
        }

        /// <summary>
        /// Registers a named HttpClient with the specified options and core Atc.Rest.Client services.
        /// </summary>
        /// <typeparam name="TOptions">The type of options, must inherit from <see cref="AtcRestClientOptions"/>.</typeparam>
        /// <param name="clientName">The name of the HttpClient to register.</param>
        /// <param name="options">The options containing BaseAddress and Timeout configuration.</param>
        /// <param name="httpClientBuilder">Optional action to further configure the HttpClient.</param>
        /// <param name="contractSerializer">Optional custom contract serializer. If null, uses DefaultJsonContractSerializer.</param>
        /// <returns>The service collection for chaining.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IServiceCollection AddAtcRestClient<TOptions>(
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
        /// <param name="clientName">The name of the HttpClient to register.</param>
        /// <param name="baseAddress">The base address for the HttpClient.</param>
        /// <param name="timeout">The timeout for the HttpClient.</param>
        /// <param name="httpClientBuilder">Optional action to further configure the HttpClient.</param>
        /// <param name="contractSerializer">Optional custom contract serializer. If null, uses DefaultJsonContractSerializer.</param>
        /// <returns>The service collection for chaining.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IServiceCollection AddAtcRestClient(
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

    // A JsonSerializerOptions registered in the service collection is used as before, when the serializer was
    // registered by type and the options were injected into its constructor.
    private static IContractSerializer CreateDefaultContractSerializer(
        IServiceProvider serviceProvider)
        => new DefaultJsonContractSerializer(
            serviceProvider.GetService<JsonSerializerOptions>()
            ?? serviceProvider.GetRequiredService<IOptions<AtcRestClientJsonOptions>>().Value.JsonSerializerOptions);
}