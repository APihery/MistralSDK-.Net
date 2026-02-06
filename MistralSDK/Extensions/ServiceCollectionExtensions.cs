using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MistralSDK.Abstractions;
using MistralSDK.Configuration;
using System;

namespace MistralSDK.Extensions
{
    /// <summary>
    /// Extension methods for configuring MistralSDK services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Mistral AI client to the service collection with the specified options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">An action to configure the client options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMistralClient(options =>
        /// {
        ///     options.ApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        ///     options.TimeoutSeconds = 60;
        ///     options.MaxRetries = 3;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMistralClient(
            this IServiceCollection services,
            Action<MistralClientOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Register the options
            services.Configure(configureOptions);

            // Register the HttpClient using IHttpClientFactory
            services.AddHttpClient<IMistralClient, MistralClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MistralClientOptions>>().Value;
                
                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            });

            return services;
        }

        /// <summary>
        /// Adds the Mistral AI client to the service collection with configuration from the specified section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration section containing MistralClientOptions.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // In appsettings.json:
        /// // {
        /// //   "MistralApi": {
        /// //     "ApiKey": "", // Set via environment variable or user secrets
        /// //     "BaseUrl": "https://api.mistral.ai/v1",
        /// //     "TimeoutSeconds": 30
        /// //   }
        /// // }
        /// 
        /// services.AddMistralClient(Configuration.GetSection("MistralApi"));
        /// </code>
        /// </example>
        public static IServiceCollection AddMistralClient(
            this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfigurationSection configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Bind configuration to options
            services.Configure<MistralClientOptions>(configuration);

            // Register the HttpClient using IHttpClientFactory
            services.AddHttpClient<IMistralClient, MistralClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MistralClientOptions>>().Value;
                
                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            });

            return services;
        }

        /// <summary>
        /// Adds the Mistral AI client to the service collection with API key from environment variable.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="environmentVariableName">The name of the environment variable containing the API key. Default is "MISTRAL_API_KEY".</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the environment variable is not set.</exception>
        /// <example>
        /// <code>
        /// // Uses MISTRAL_API_KEY environment variable by default
        /// services.AddMistralClientFromEnvironment();
        /// 
        /// // Or specify a custom environment variable name
        /// services.AddMistralClientFromEnvironment("MY_MISTRAL_KEY");
        /// </code>
        /// </example>
        public static IServiceCollection AddMistralClientFromEnvironment(
            this IServiceCollection services,
            string environmentVariableName = "MISTRAL_API_KEY")
        {
            var apiKey = Environment.GetEnvironmentVariable(environmentVariableName);
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{environmentVariableName}' is not set or is empty. " +
                    "Please set this variable with your Mistral API key. " +
                    "Never commit API keys to source control.");
            }

            return services.AddMistralClient(options =>
            {
                options.ApiKey = apiKey;
            });
        }
    }
}
