﻿//
// Copyright © 2021 Terry Moreland
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TSMoreland.Extensions.Http.Internal;

namespace TSMoreland.Extensions.Http
{
    public sealed class UpdatableHttpClientFactory : IHttpMessageHandlerFactory
    {
        private static readonly TimerCallback _cleanupCallback = (s) => ((UpdatableHttpClientFactory)s).CleanupTimer_Tick();
        private readonly ILogger<UpdatableHttpClientFactory> _logger;
        private readonly IServiceProvider _services;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<HttpClientFactoryOptions> _optionsMonitor;
        private readonly IHttpMessageHandlerBuilderFilter[] _filters;
        private readonly IHttpClientFactory _clientFactory;

        private readonly Func<string, Lazy<ActiveHandlerTrackingEntry>> _entryFactory;
        // Default time of 10s for cleanup seems reasonable.
        // Quick math:
        // 10 distinct named clients * expiry time >= 1s = approximate cleanup queue of 100 items
        //
        // This seems frequent enough. We also rely on GC occurring to actually trigger disposal.
        // ReSharper disable once InconsistentNaming
        private readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromSeconds(10);

        // We use a new timer for each regular cleanup cycle, protected with a lock. Note that this scheme
        // doesn't give us anything to dispose, as the timer is started/stopped as needed.
        //
        // There's no need for the factory itself to be disposable. If you stop using it, eventually everything will
        // get reclaimed.
        private Timer? _cleanupTimer;
        private readonly object _cleanupTimerLock;
        private readonly object _cleanupActiveLock;

        // Collection of 'active' handlers.
        //
        // Using lazy for synchronization to ensure that only one instance of HttpMessageHandler is created
        // for each name.
        //
        // internal for tests
        internal readonly ConcurrentDictionary<string, Lazy<ActiveHandlerTrackingEntry>> _activeHandlers;

        // Collection of 'expired' but not yet disposed handlers.
        //
        // Used when we're rotating handlers so that we can dispose HttpMessageHandler instances once they
        // are eligible for garbage collection.
        //
        // internal for tests
        internal readonly ConcurrentQueue<ExpiredHandlerTrackingEntry> _expiredHandlers;
        private readonly TimerCallback _expiryCallback;

        /// <summary>
        /// Instantiates a new instance of <see cref="UpdatableHttpClientFactory"/> class.
        /// </summary>
        public UpdatableHttpClientFactory(
            IServiceProvider services,
            IServiceScopeFactory scopeFactory,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor,
            IEnumerable<IHttpMessageHandlerBuilderFilter> filters,
            IHttpClientFactory clientFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (filters == null!)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _filters = filters.ToArray();

            _logger = loggerFactory.CreateLogger<UpdatableHttpClientFactory>();

            // case-sensitive because named options is.
            _activeHandlers = new ConcurrentDictionary<string, Lazy<ActiveHandlerTrackingEntry>>(StringComparer.Ordinal);
            _entryFactory = (name) => 
                new Lazy<ActiveHandlerTrackingEntry>(() => 
                    CreateHandlerEntry(name), LazyThreadSafetyMode.ExecutionAndPublication);

            _expiredHandlers = new ConcurrentQueue<ExpiredHandlerTrackingEntry>();
            _expiryCallback = ExpiryTimer_Tick;

            _cleanupTimerLock = new object();
            _cleanupActiveLock = new object();
        }

        /// <inheritdoc cref="IHttpClientFactory.CreateClient"/>
        public HttpClient CreateClient(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            HttpMessageHandler handler = CreateHandler(name);
            var client = new HttpClient(handler, disposeHandler: false);

            HttpClientFactoryOptions options = _optionsMonitor.Get(name);
            for (int i = 0; i < options.HttpClientActions.Count; i++)
            {
                options.HttpClientActions[i](client);
            }

            return client;
        }
        public HttpMessageHandler CreateHandler(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var entry = _activeHandlers.GetOrAdd(name, _entryFactory).Value;

            StartHandlerEntryTimer(entry);

            return entry.Handler;
        }

        // Internal for tests
        internal ActiveHandlerTrackingEntry CreateHandlerEntry(string name)
        {
            IServiceProvider services = _services;
            var scope = (IServiceScope?)null;

            HttpClientFactoryOptions options = _optionsMonitor.Get(name);
            if (!options.SuppressHandlerScope)
            {
                scope = _scopeFactory.CreateScope();
                services = scope.ServiceProvider;
            }

            try
            {
                HttpMessageHandlerBuilder builder = services.GetRequiredService<HttpMessageHandlerBuilder>();
                builder.Name = name;

                // This is similar to the initialization pattern in:
                // https://github.com/aspnet/Hosting/blob/e892ed8bbdcd25a0dafc1850033398dc57f65fe1/src/Microsoft.AspNetCore.Hosting/Internal/WebHost.cs#L188
                Action<HttpMessageHandlerBuilder> configure = Configure;
                for (int i = _filters.Length - 1; i >= 0; i--)
                {
                    configure = _filters[i].Configure(configure);
                }

                configure(builder);

                // Wrap the handler so we can ensure the inner handler outlives the outer handler.
                var handler = new LifetimeTrackingProxiedHttpMessageHandler(builder.Build());

                // Note that we can't start the timer here. That would introduce a very very subtle race condition
                // with very short expiry times. We need to wait until we've actually handed out the handler once
                // to start the timer.
                //
                // Otherwise it would be possible that we start the timer here, immediately expire it (very short
                // timer) and then dispose it without ever creating a client. That would be bad. It's unlikely
                // this would happen, but we want to be sure.
                return new ActiveHandlerTrackingEntry(name, handler, scope, options.HandlerLifetime);

                void Configure(HttpMessageHandlerBuilder b)
                {
                    for (int i = 0; i < options.HttpMessageHandlerBuilderActions.Count; i++)
                    {
                        options.HttpMessageHandlerBuilderActions[i](b);
                    }
                }
            }
            catch
            {
                // If something fails while creating the handler, dispose the services.
                scope?.Dispose();
                throw;
            }
        }

        // Internal for tests
        internal void ExpiryTimer_Tick(object state)
        {
            var active = (ActiveHandlerTrackingEntry)state;

            // The timer callback should be the only one removing from the active collection. If we can't find
            // our entry in the collection, then this is a bug.
            bool removed = _activeHandlers.TryRemove(active.Name, out Lazy<ActiveHandlerTrackingEntry> found);
            Debug.Assert(removed, "Entry not found. We should always be able to remove the entry");
            Debug.Assert(ReferenceEquals(active, found.Value), "Different entry found. The entry should not have been replaced");

            // At this point the handler is no longer 'active' and will not be handed out to any new clients.
            // However we haven't dropped our strong reference to the handler, so we can't yet determine if
            // there are still any other outstanding references (we know there is at least one).
            //
            // We use a different state object to track expired handlers. This allows any other thread that acquired
            // the 'active' entry to use it without safety problems.
            var expired = new ExpiredHandlerTrackingEntry(active);
            _expiredHandlers.Enqueue(expired);

            Log.HandlerExpired(_logger, active.Name, active.Lifetime);

            StartCleanupTimer();
        }

        // Internal so it can be overridden in tests
        internal void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry)
        {
            entry.StartExpiryTimer(_expiryCallback);
        }

        // Internal so it can be overridden in tests
        internal void StartCleanupTimer()
        {
            lock (_cleanupTimerLock)
            {
                if (_cleanupTimer == null)
                {
                    _cleanupTimer = NonCapturingTimer.Create(_cleanupCallback, this, DefaultCleanupInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        // Internal so it can be overridden in tests
        internal void StopCleanupTimer()
        {
            lock (_cleanupTimerLock)
            {
                _cleanupTimer?.Dispose();
                _cleanupTimer = null;
            }
        }

        // Internal for tests
        internal void CleanupTimer_Tick()
        {
            // Stop any pending timers, we'll restart the timer if there's anything left to process after cleanup.
            //
            // With the scheme we're using it's possible we could end up with some redundant cleanup operations.
            // This is expected and fine.
            //
            // An alternative would be to take a lock during the whole cleanup process. This isn't ideal because it
            // would result in threads executing ExpiryTimer_Tick as they would need to block on cleanup to figure out
            // whether we need to start the timer.
            StopCleanupTimer();

            if (!Monitor.TryEnter(_cleanupActiveLock))
            {
                // We don't want to run a concurrent cleanup cycle. This can happen if the cleanup cycle takes
                // a long time for some reason. Since we're running user code inside Dispose, it's definitely
                // possible.
                //
                // If we end up in that position, just make sure the timer gets started again. It should be cheap
                // to run a 'no-op' cleanup.
                StartCleanupTimer();
                return;
            }

            try
            {
                int initialCount = _expiredHandlers.Count;
                Log.CleanupCycleStart(_logger, initialCount);

                var stopwatch = ValueStopwatch.StartNew();

                int disposedCount = 0;
                for (int i = 0; i < initialCount; i++)
                {
                    // Since we're the only one removing from _expired, TryDequeue must always succeed.
                    _expiredHandlers.TryDequeue(out var entry);
                    Debug.Assert(entry != null, "Entry was null, we should always get an entry back from TryDequeue");

                    if (entry == null)
                    {
                        throw new InvalidOperationException("Entry was null, we should always get an entry back from TryDequeue");
                    }

                    if (entry!.CanDispose)
                    {
                        try
                        {
                            entry.InnerHandler.Dispose();
                            entry.Scope?.Dispose();
                            disposedCount++;
                        }
                        catch (Exception ex)
                        {
                            Log.CleanupItemFailed(_logger, entry.Name, ex);
                        }
                    }
                    else
                    {
                        // If the entry is still live, put it back in the queue so we can process it
                        // during the next cleanup cycle.
                        _expiredHandlers.Enqueue(entry);
                    }
                }

                Log.CleanupCycleEnd(_logger, stopwatch.GetElapsedTime(), disposedCount, _expiredHandlers.Count);
            }
            finally
            {
                Monitor.Exit(_cleanupActiveLock);
            }

            // We didn't totally empty the cleanup queue, try again later.
            if (!_expiredHandlers.IsEmpty)
            {
                StartCleanupTimer();
            }
        }

        private static class Log
        {
            private static class EventIds
            {
                public static readonly EventId CleanupCycleStartEvent = new (100, nameof(CleanupCycleStart));
                public static readonly EventId CleanupCycleEndEvent = new (101, nameof(CleanupCycleEnd));
                public static readonly EventId CleanupItemFailedEvent = new (102, nameof(CleanupItemFailed));
                public static readonly EventId HandlerExpiredEvent = new (103, nameof(HandlerExpired));
            }

            private static readonly Action<ILogger, int, Exception?> _cleanupCycleStart = LoggerMessage.Define<int>(
                LogLevel.Debug,
                EventIds.CleanupCycleStartEvent,
                "Starting HttpMessageHandler cleanup cycle with {InitialCount} items");

            private static readonly Action<ILogger, double, int, int, Exception?> _cleanupCycleEnd = LoggerMessage.Define<double, int, int>(
                LogLevel.Debug,
                EventIds.CleanupCycleEndEvent,
                "Ending HttpMessageHandler cleanup cycle after {ElapsedMilliseconds}ms - processed: {DisposedCount} items - remaining: {RemainingItems} items");

            private static readonly Action<ILogger, string, Exception?> _cleanupItemFailed = LoggerMessage.Define<string>(
                LogLevel.Error,
                EventIds.CleanupItemFailedEvent,
                "HttpMessageHandler.Dispose() threw an unhandled exception for client: '{ClientName}'");

            private static readonly Action<ILogger, double, string, Exception?> _handlerExpired = LoggerMessage.Define<double, string>(
                LogLevel.Debug,
                EventIds.HandlerExpiredEvent,
                "HttpMessageHandler expired after {HandlerLifetime}ms for client '{ClientName}'");


            public static void CleanupCycleStart(ILogger logger, int initialCount)
            {
                _cleanupCycleStart(logger, initialCount, null);
            }

            public static void CleanupCycleEnd(ILogger logger, TimeSpan duration, int disposedCount, int finalCount)
            {
                _cleanupCycleEnd(logger, duration.TotalMilliseconds, disposedCount, finalCount, null);
            }

            public static void CleanupItemFailed(ILogger logger, string clientName, Exception exception)
            {
                _cleanupItemFailed(logger, clientName, exception);
            }

            public static void HandlerExpired(ILogger logger, string clientName, TimeSpan lifetime)
            {
                _handlerExpired(logger, lifetime.TotalMilliseconds, clientName, null);
            }
        }

    }
}
