//
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
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using TSMoreland.GuardAssertions;

namespace TSMoreland.Extensions.Http
{
    public class DefaultHttpMessageHandlerBuilder<T> : IHttpMessageHandlerBuilder<T>
    {
        public DefaultHttpMessageHandlerBuilder(string name, IServiceCollection services, 
            Func<T, IServiceCollection, HttpMessageHandler>? primaryHandlerFactory)
        {
            Guard.Against.ArgumentNullOrEmpty(name, nameof(name));

            Name = name;
            Services = services ?? throw new ArgumentNullException(nameof(services));
            AdditionalHandlers = new List<Func<T, IServiceCollection, DelegatingHandler>>();

            PrimaryHandlerFactory = primaryHandlerFactory ?? DefaultHandler;

            static HttpMessageHandler DefaultHandler(T argument, IServiceCollection services)
            {
                return new HttpClientHandler();
            }
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public IServiceCollection Services { get; }

        /// <inheritdoc/>
        public Func<T, IServiceCollection, HttpMessageHandler> PrimaryHandlerFactory { get; }

        /// <inheritdoc/>
        public List<Func<T, IServiceCollection, DelegatingHandler>> AdditionalHandlers { get; }

        /// <inheritdoc/>
        public HttpMessageHandler Build(T argument)
        {
            var primary = PrimaryHandlerFactory.Invoke(argument, Services) ?? new HttpClientHandler();

            var previous = primary;
            HttpMessageHandler? topMost = null;
            for (int i = AdditionalHandlers.Count - 1; i >= 0; i--)
            {
                var delegatingHandler = AdditionalHandlers[i](argument, Services);
                delegatingHandler.InnerHandler = previous;
                previous = delegatingHandler;
                topMost ??= previous;
            }
            topMost ??= primary;
            return topMost!;
        }
    }
}
