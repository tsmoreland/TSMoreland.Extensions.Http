// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TSMoreland.Extensions.Http.Internal
{
    /// <summary>
    /// Thread-safety: This class is immutable
    /// </summary>
    /// <remarks>
    /// Copied from
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/ExpiredHandlerTrackingEntry.cs
    /// </remarks>
    internal sealed class ExpiredHandlerTrackingEntry
    {
        private readonly WeakReference _livenessTracker;

        // IMPORTANT: don't cache a reference to `other` or `other.Handler` here.
        // We need to allow it to be GC'ed.
        public ExpiredHandlerTrackingEntry(ActiveHandlerTrackingEntry other)
        {

            Name = other.Name;
            Scope = other.Scope;

            _livenessTracker = new WeakReference(other.Handler);
            InnerHandler = other.Handler.InnerHandler;
        }

        public bool CanDispose => !_livenessTracker.IsAlive;

        public HttpMessageHandler? InnerHandler { get; }

        public string Name { get; }

        public IServiceScope? Scope { get; }
    }
}
