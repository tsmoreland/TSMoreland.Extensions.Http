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
using TSMoreland.GuardAssertions;

namespace TSMoreland.Extensions.Http
{
    public class DefaultHttpMessageHandlerBuilder<T> : IHttpMessageHandlerBuilder<T>
    {
        private Func<T, IServiceProvider, HttpMessageHandler> _primaryHandlerFactory;

        public DefaultHttpMessageHandlerBuilder(string name)
        {
            Guard.Against.ArgumentNullOrEmpty(name, nameof(name));

            Name = name;
            AdditionalHandlers = new List<Func<T, IServiceProvider, DelegatingHandler>>();

            _primaryHandlerFactory = DefaultHandler;

            static HttpMessageHandler DefaultHandler(T argument, IServiceProvider serviceProvider)
            {
                return new HttpClientHandler();
            }
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public Func<T, IServiceProvider, HttpMessageHandler> PrimaryHandlerFactory
        {
            get => _primaryHandlerFactory;
            set
            {
                Guard.Against.ArgumentNull(value, nameof(value));
                _primaryHandlerFactory = value;
            }
        }

        /// <inheritdoc/>
        public IList<Func<T, IServiceProvider, DelegatingHandler>> AdditionalHandlers { get; }

        /// <inheritdoc/>
        public HttpMessageHandler Build(T argument, IServiceProvider serviceProvider)
        {
            Guard.Against.ArgumentNull(serviceProvider, nameof(serviceProvider));

            var primary = PrimaryHandlerFactory.Invoke(argument, serviceProvider) ?? new HttpClientHandler();

            var previous = primary;
            HttpMessageHandler? topMost = null;
            for (int i = AdditionalHandlers.Count - 1; i >= 0; i--)
            {
                var delegatingHandler = AdditionalHandlers[i](argument, serviceProvider);
                delegatingHandler.InnerHandler = previous;
                previous = delegatingHandler;
                topMost ??= previous;
            }
            topMost ??= primary;
            return topMost!;
        }
    }
}
