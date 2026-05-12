using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core.Logging;

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
                builder.AddRollingFile(new RollingFileLoggerOptions
                {
                    MinimumLevel = LogLevel.Information,
                    RetentionDays = 14
                });
            });

            // Register non-generic ILogger for classes that use service location
            services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("RecoveryCommander"));

            // Register other services as needed
            services.AddSingleton<GlobalExceptionHandler>();

            services.AddHttpClient("RecoveryCommander", client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromMinutes(5);
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                ConnectCallback = async (context, cancellationToken) =>
                {
                    // SSRF Protection: Resolve DNS to IP and check if it's loopback or private.
                    // This protects against DNS rebinding and obfuscated IPs.
                    var entry = await System.Net.Dns.GetHostEntryAsync(context.DnsEndPoint.Host);
                    var ip = entry.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                    
                    if (ip == null || IsPrivateOrLoopbackIp(ip))
                        throw new System.Security.SecurityException($"SSRF attempt detected. Host {context.DnsEndPoint.Host} resolved to an invalid or private IP.");

                    var socket = new System.Net.Sockets.Socket(ip.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                    try
                    {
                        socket.NoDelay = true;
                        await socket.ConnectAsync(new System.Net.IPEndPoint(ip, context.DnsEndPoint.Port), cancellationToken);
                        return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            });
        }

        private static bool IsPrivateOrLoopbackIp(System.Net.IPAddress ip)
        {
            if (System.Net.IPAddress.IsLoopback(ip)) return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes[0] == 10) return true; // 10.0.0.0/8
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
                if (bytes[0] == 192 && bytes[1] == 168) return true; // 192.168.0.0/16
                if (bytes[0] == 169 && bytes[1] == 254) return true; // 169.254.0.0/16 (Link-local)
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;
                // Unique Local Address (fc00::/7)
                var bytes = ip.GetAddressBytes();
                if ((bytes[0] & 0xfe) == 0xfc) return true;
            }

            return false;
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


    }
}
