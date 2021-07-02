using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TSMoreland.Extensions.Http
{
    public sealed class HttpClientRepository : IHttpClientRepository
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<HttpClientRepository> _logger;

        private readonly object _activeHandlers;
        private readonly MethodInfo _activeHandlersTryRemove;
        private readonly IOptionsMonitor<HttpClientFactoryOptions> _optionsMonitor;

        public HttpClientRepository(IHttpClientFactory clientFactory, ILogger<HttpClientRepository> logger)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var factoryType = _clientFactory.GetType();
            var fields = factoryType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            _activeHandlers =
                fields
                    .Where(field => string.Equals(field.Name, "_activeHandlers", StringComparison.OrdinalIgnoreCase))
                    .Select(field => field.GetValue(_clientFactory))
                    .SingleOrDefault()
                ?? throw new ArgumentException("Unable to locate active and/or expired handlers in client factory",
                    nameof(clientFactory));
            _activeHandlersTryRemove = GetFirstRemoveMethodOrThrow(_activeHandlers.GetType());

            _optionsMonitor =
                fields
                    .Where(field => string.Equals(field.Name, "_optionsMonitor", StringComparison.OrdinalIgnoreCase))
                    .Select(field => field.GetValue(_clientFactory))
                    .OfType<IOptionsMonitor<HttpClientFactoryOptions>>()
                    .SingleOrDefault()
                ?? throw new ArgumentException("Unable to locate active and/or expired handlers in client factory",
                    nameof(clientFactory));

            
            MethodInfo GetFirstRemoveMethodOrThrow(Type type) =>
                type.GetMethods().FirstOrDefault(m => m.Name == "TryRemove" && m.GetParameters().Length == 2)
                    ?? throw new ArgumentException("Unable to locate TryRemove method", nameof(clientFactory));
        }

        /// <inheritdoc/>
        public HttpClient CreateClient(string name)
        {
            return _clientFactory.CreateClient(name);
        }

        /// <inheritdoc/>
        public bool TryRemoveClient(string name)
        {
            object? outputParameter = null;
            var parameters = new object?[] {name, outputParameter};
            var result = _activeHandlersTryRemove
                .Invoke(_activeHandlers, parameters) is bool and true;

            return result;
        }
    }
}
