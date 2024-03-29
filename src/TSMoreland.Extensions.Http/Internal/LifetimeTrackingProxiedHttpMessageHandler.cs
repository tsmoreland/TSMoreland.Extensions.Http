﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TSMoreland.Extensions.Http.Internal
{
    /// <summary>
    /// share a reference to an instance of this class, and when it goes out of scope the inner handler 
    /// is eligible to be disposed.
    /// </summary>
    /// <remarks>
    /// Copied from
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/LifetimeTrackingHttpMessageHandler.cs
    /// </remarks>
    internal sealed class LifetimeTrackingProxiedHttpMessageHandler : DelegatingHandler
    {
        public LifetimeTrackingProxiedHttpMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override void Dispose(bool disposing)
        {
            // The lifetime of this is tracked separately by ActiveHandlerTrackingEntry
        }
    }
}
