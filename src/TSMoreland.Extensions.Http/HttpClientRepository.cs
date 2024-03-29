﻿//
// Copyright (c) 2022 Terry Moreland
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
// Contains code based on or copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs
// which is licensed under the the MIT License (same license as above).

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TSMoreland.Extensions.Http.Abstractions;
using TSMoreland.Extensions.Http.Internal;
using TSMoreland.GuardAssertions;

namespace TSMoreland.Extensions.Http;

/// <summary>
/// <para>
/// Extended version of <see cref="IHttpClientFactory"/> based heavily on
/// <a href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs">
/// DefaultHttpClientFactory.cs
/// </a>
/// </para>
/// <para>
/// Intention is to provide repository like behaviour for <see cref="IHttpClientFactory"/> by allow
/// <see cref="HttpMessageHandler"/> objects to be built using dynamic settings.  The reasoning for this class is that
/// <see cref="IHttpClientFactory"/> depends on all named clients being registered during service configuration while
/// this class allows them to be added, updated or removed at runtime.
/// </para>
/// </summary>
public sealed class HttpClientRepository<T> : IHttpClientRepository<T>
{
    private static readonly TimerCallback _cleanupCallback = (s) => ((HttpClientRepository<T>?)s)?.CleanupTimer_Tick();
    private readonly IHttpClientFactory _clientFactory;
    private readonly IHttpMessageHandlerFactory _messageHandlerFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HttpClientRepository<T>> _logger;

    // Default time of 10s for cleanup seems reasonable.
    // Quick math:
    // 10 distinct named clients * expiry time >= 1s = approximate cleanup queue of 100 items
    //
    // This seems frequent enough. We also rely on GC occurring to actually trigger disposal.
    private TimeSpan DefaultCleanupInterval { get; } = TimeSpan.FromSeconds(10);

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
    /// internal for tests
    /// </summary>
    internal TimeSpan? _activeHandlerLifetime;

    /// <summary>
    /// Instantiates a new instance of the <see cref="HttpClientRepository{T}"/> class.
    /// </summary>
    /// <param name="clientFactory">fallback <see cref="IHttpClientFactory"/> used if name is not recognized by the repository</param>
    /// <param name="messageHandlerFactory">fallback <see cref="IHttpMessageHandlerFactory"/> used if name is not recognized by the repository</param>
    /// <param name="scopeFactory">scope factory used when creating tracking entries to managed life time of client and it's dependencies</param>
    /// <param name="logger"><see cref="ILogger"/> instance for diagnostic information</param>
    public HttpClientRepository(
        IHttpClientFactory clientFactory,
        IHttpMessageHandlerFactory messageHandlerFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<HttpClientRepository<T>> logger)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _messageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        MessageHandlersByName = new ConcurrentDictionary<string, IHttpMessageHandlerBuilder<T>>();

        // case-sensitive because named options is.
        _activeHandlers = new ConcurrentDictionary<string, Lazy<ActiveHandlerTrackingEntry>>(StringComparer.Ordinal);
        _expiredHandlers = new ConcurrentQueue<ExpiredHandlerTrackingEntry>();
        _expiryCallback = ExpiryTimer_Tick;

        _cleanupTimerLock = new object();
        _cleanupActiveLock = new object();
    }

    /// <summary>
    /// Internal for testing
    /// </summary>
    /// <remarks>
    /// intended only to allow overriding default clean up interval for tests
    /// </remarks>
    internal HttpClientRepository(
        IHttpClientFactory clientFactory,
        IHttpMessageHandlerFactory messageHandlerFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<HttpClientRepository<T>> logger,
        TimeSpan? defaultCleanupInterval)
        : this(clientFactory, messageHandlerFactory, scopeFactory, logger)
    {
        if (defaultCleanupInterval != null)
        {
            DefaultCleanupInterval = defaultCleanupInterval.Value;
        }
    }


    /// <inheritdoc/>
    public HttpClient CreateClient(string name)
    {
        return _clientFactory.CreateClient(name);
    }

    /// <inheritdoc/>
    public HttpClient CreateClient(string name, T argument)
    {
        Guard.Against.ArgumentNullOrEmpty(name, nameof(name));

        if (!MessageHandlersByName.ContainsKey(name))
        {
            _logger.LogWarning($"{name} not found in message handler builders, using HttpClientFactory to construct client");
            return _clientFactory.CreateClient(name);
        }

        return new HttpClient(CreateHandler(name, argument), disposeHandler: false);
    }



    /// <inheritdoc/>
    public bool ContainsName(string name)
    {
        Guard.Against.ArgumentNullOrEmpty(name, nameof(name));
        return MessageHandlersByName.ContainsKey(name);
    }

    /// <inheritdoc/>
    public IHttpMessageHandlerBuilder<T> AddIfNotPresent(string name)
    {
        Guard.Against.ArgumentNullOrEmpty(name, nameof(name));

        var builder = new DefaultHttpMessageHandlerBuilder<T>(name);

        _logger.LogDebug($"Adding or updating {name} message handler builder");
        // add, most recent call wins the race if multiple calls

        bool isPresent = false;
        MessageHandlersByName.AddOrUpdate(name, builder,
            (_, value) =>
            {
                isPresent = true;
                return value;
            });

        return isPresent
            ? new UnusedHttpMessageHandlerBuilder<T>(name)
            : builder;
    }

    /// <inheritdoc/>
    public IHttpMessageHandlerBuilder<T> AddOrReplace(string name)
    {
        Guard.Against.ArgumentNullOrEmpty(name, nameof(name));

        var builder = new DefaultHttpMessageHandlerBuilder<T>(name);
        _logger.LogDebug($"Adding or updating {name} message handler builder");
        // add, most recent call wins the race if multiple calls
        return MessageHandlersByName.AddOrUpdate(name, builder, (_, _) => builder);
    }

    /// <inheritdoc/>
    public bool TryRemoveClient(string name)
    {
        Guard.Against.ArgumentNullOrEmpty(name, nameof(name));

        if (!MessageHandlersByName.TryRemove(name, out _))
        {
            return false;
        }

        // remove from active handlers, if was in use it'll expire and perform clean up then
        // if it wasn't active this this would be needed
        _ = _activeHandlers.TryRemove(name, out _);
        return true;
    }

    private HttpMessageHandler CreateHandler(string name, T argument)
    {
        Guard.Against.ArgumentNull(name, nameof(name));

        if (!MessageHandlersByName.ContainsKey(name))
        {
            _logger.LogWarning($"{name} not found in message handler builders, using HttpMessageHandlerFactory to construct client");
            return _messageHandlerFactory.CreateHandler(name);
        }

        var handler = BuildActiveHandlerTrackingEntry(name, argument);
        var entry = _activeHandlers.GetOrAdd(name, handler).Value;
        StartHandlerEntryTimer(entry);
        return entry.Handler;

        Lazy<ActiveHandlerTrackingEntry> BuildActiveHandlerTrackingEntry(string localName, T localOptions)
        {
            return new(() => CreateHandlerEntry(localName, localOptions), LazyThreadSafetyMode.ExecutionAndPublication);
        }

    }


    /// <remarks>
    /// <para>
    /// Based on DefaultHttpClientFactory, modified to use <see cref="MessageHandlersByName"/>
    /// <a href="https://github.com/dotnet/runtime/blob/41e6601c5f9458fa8c5dea25ee78cbf121aa322f/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L136">
    /// DefaultHttpClientFactory.cs line 136
    /// </a>
    /// 
    /// </para>
    /// <para>
    /// updated to build messsage handler via builder method matching <paramref name="name"/>
    /// if no such entries is found then fallback to using <see cref="IHttpMessageHandlerFactory"/>
    /// </para>
    /// <para>
    /// internal for testing
    /// </para>
    /// </remarks>
    internal ActiveHandlerTrackingEntry CreateHandlerEntry(string name, T argument)
    {
        IServiceScope? scope = null;
        try
        {
            scope = _scopeFactory.CreateScope();

            if (!MessageHandlersByName.TryGetValue(name, out var builder))
            {
                // constructing with an argument but don't have a builder that can use it, if we get here it's due to race condition
                _logger.LogWarning("Constructing HttpClient from repository using IHttpMessageHandlerFactory fallback");
                var handler = new LifetimeTrackingProxiedHttpMessageHandler(_messageHandlerFactory.CreateHandler(name));
                return new ActiveHandlerTrackingEntry(name, handler, scope, _activeHandlerLifetime ?? TimeSpan.FromMinutes(2));
            }
            else
            {
                // Wrap the handler so we can ensure the inner handler outlives the outer handler.
                var handler = new LifetimeTrackingProxiedHttpMessageHandler(builder.Build(argument, scope.ServiceProvider));

                // Note that we can't start the timer here. That would introduce a very very subtle race condition
                // with very short expiry times. We need to wait until we've actually handed out the handler once
                // to start the timer.
                //
                // Otherwise it would be possible that we start the timer here, immediately expire it (very short
                // timer) and then dispose it without ever creating a client. That would be bad. It's unlikely
                // this would happen, but we want to be sure.
                // lifetime hard coded to 2 minutes, eventually this may come from the object passed in
                return new ActiveHandlerTrackingEntry(name, handler, scope, _activeHandlerLifetime ?? TimeSpan.FromMinutes(2));
            }
        }
        catch
        {
            // If something fails while creating the handler, dispose the services.
            scope?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// internal for testing
    /// </summary>
    internal ConcurrentDictionary<string, IHttpMessageHandlerBuilder<T>> MessageHandlersByName { get; }

    /// <remarks>
    /// <para>
    /// Based on DefaultHttpClientFactory, modified to use <see cref="MessageHandlersByName"/>
    /// <a href="https://github.com/dotnet/runtime/blob/41e6601c5f9458fa8c5dea25ee78cbf121aa322f/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L207">
    /// DefaultHttpClientFactory.cs line 207
    /// </a>
    /// </para>
    /// <para>
    /// internal for testing
    /// </para>
    /// </remarks>
    internal void ExpiryTimer_Tick(object? state)
    {
        if (state is not ActiveHandlerTrackingEntry active)
        {
            return;
        }

        // The timer callback should be the only one removing from the active collection. If we can't find
        // our entry in the collection, then this is a bug.
        bool removed = _activeHandlers.TryRemove(active.Name, out Lazy<ActiveHandlerTrackingEntry>? found);
        if (!removed)
        {
            _logger.LogWarning("Active Handler not found during expiry, this may be due to its forceful removal due to change in settings");
        }
        else if (found is not null && !ReferenceEquals(active, found.Value))
        {
            _logger.LogWarning("Entry found but value did not match, value will be re-added - if the same engine is updated too rapidly " +
                               "this may result in a race condition leaving us with out of date handler value");
            _activeHandlers.AddOrUpdate(found.Value.Name, found, (_, _) => found);
        }

        // At this point the handler is no longer 'active' and will not be handed out to any new clients.
        // However we haven't dropped our strong reference to the handler, so we can't yet determine if
        // there are still any other outstanding references (we know there is at least one).
        //
        // We use a different state object to track expired handlers. This allows any other thread that acquired
        // the 'active' entry to use it without safety problems.
        var expired = new ExpiredHandlerTrackingEntry(active);
        _expiredHandlers.Enqueue(expired);

        StartCleanupTimer();
    }

    /// <remarks>
    /// <para>
    /// Based on DefaultHttpClientFactory, modified to use <see cref="MessageHandlersByName"/>
    /// <a href="https://github.com/dotnet/runtime/blob/41e6601c5f9458fa8c5dea25ee78cbf121aa322f/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L232">
    /// DefaultHttpClientFactory.cs line 232
    /// </a>
    /// </para>
    /// <para>
    /// Internal so it can be overridden in tests
    /// </para>
    /// </remarks>
    internal void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry)
    {
        entry.StartExpiryTimer(_expiryCallback);
    }

    /// <remarks>
    /// <para>
    /// Based on DefaultHttpClientFactory, modified to use <see cref="MessageHandlersByName"/>
    /// <a href="https://github.com/dotnet/runtime/blob/41e6601c5f9458fa8c5dea25ee78cbf121aa322f/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L238">
    /// DefaultHttpClientFactory.cs line 238
    /// </a>
    /// </para>
    /// <para>
    /// internal for testing
    /// </para>
    /// </remarks>
    internal void StartCleanupTimer()
    {
        lock (_cleanupTimerLock)
        {
            _cleanupTimer ??= NonCapturingTimer.Create(_cleanupCallback, this, DefaultCleanupInterval, Timeout.InfiniteTimeSpan);
        }
    }

    /// <remarks>
    /// <para>
    /// Based on DefaultHttpClientFactory, modified to use <see cref="MessageHandlersByName"/>
    /// <a href="https://github.com/dotnet/runtime/blob/41e6601c5f9458fa8c5dea25ee78cbf121aa322f/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L250">
    /// DefaultHttpClientFactory.cs line 250
    /// </a>
    /// </para>
    /// <para>
    /// internal for testing
    /// </para>
    /// </remarks>
    internal void StopCleanupTimer()
    {
        lock (_cleanupTimerLock)
        {
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;
        }
    }

    /// <remarks>
    /// <para>
    /// Based on DefaultHttpClientFactory, modified to use <see cref="MessageHandlersByName"/>
    /// <a href="https://github.com/dotnet/runtime/blob/41e6601c5f9458fa8c5dea25ee78cbf121aa322f/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L260">
    /// DefaultHttpClientFactory.cs line 260
    /// </a>
    /// </para>
    /// <para>
    /// internal for testing
    /// </para>
    /// </remarks>
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
                        entry.InnerHandler?.Dispose();
                        entry.Scope?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{entry.Name} failed to dispoe correctly");
                    }
                }
                else
                {
                    // If the entry is still live, put it back in the queue so we can process it
                    // during the next cleanup cycle.
                    _expiredHandlers.Enqueue(entry);
                }
            }

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
}
