using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Dependency injection container for services
    /// </summary>
    public static class ServiceContainer
    {
        private static IServiceProvider? _serviceProvider;
        private static IServiceCollection? _services;

        /// <summary>
        /// Initialize the service container
        /// </summary>
        public static void Initialize(Action<IServiceCollection>? configureServices = null)
        {
            _services = new ServiceCollection();
            ConfigureServices(_services);
            configureServices?.Invoke(_services);
            _serviceProvider = _services.BuildServiceProvider();
        }

        /// <summary>
        /// Get service of type T
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceContainer not initialized");
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Get optional service of type T
        /// </summary>
        public static T? GetOptionalService<T>() where T : class
        {
            if (_serviceProvider == null)
                return null;
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Configure services
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Register other services as needed
            services.AddSingleton<GlobalExceptionHandler>();

            services.AddHttpClient("RecoveryCommander", client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromMinutes(5);
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            });
        }

        /// <summary>
        /// Get the shared HttpClient instance via IHttpClientFactory
        /// </summary>
        public static HttpClient GetHttpClient()
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceContainer not initialized");
                
            var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient("RecoveryCommander");
        }

        /// <summary>
        /// Dispose the service container
        /// </summary>
        public static void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                _serviceProvider = null;
            }
        }

        /// <summary>
        /// Register modules
        /// </summary>
        public static void RegisterModules(IEnumerable<IRecoveryModule> modules)
        {
            if (_services == null)
                throw new InvalidOperationException("ServiceContainer not initialized");

            foreach (var module in modules)
            {
                _services.AddSingleton(module);
            }

            // Rebuild service provider
            _serviceProvider = _services.BuildServiceProvider();
        }
    }
}
