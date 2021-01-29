using System;
using System.ComponentModel;
using Atc.Rest.Client.Builder;
using Atc.Rest.Client.Options;
using Atc.Rest.Client.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Atc.Rest.Client
{
    public static class ServiceCollectionExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IServiceCollection AddAtcRestClient<TOptions>(
            this IServiceCollection services,
            string clientName,
            TOptions options,
            Action<IHttpClientBuilder>? httpClientBuilder = default)
            where TOptions : AtcRestClientOptions, new()
        {
            services.AddSingleton(options);
            services.AddSingleton<AtcRestClientOptions>(options);

            var clientBuilder = services.AddHttpClient(clientName, (s, c) =>
            {
                var o = s.GetRequiredService<AtcRestClientOptions>();
                c.BaseAddress = o.BaseAddress;
                c.Timeout = o.Timeout;
            });

            httpClientBuilder?.Invoke(clientBuilder);

            // Register utilities
            services.AddSingleton<IHttpMessageFactory, HttpMessageFactory>();
            services.AddSingleton<IContractSerializer, DefaultJsonContractSerializer>();

            return services;
        }
    }
}