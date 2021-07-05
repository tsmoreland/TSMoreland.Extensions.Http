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
using System.Linq;
using System.Net.Http;

namespace TSMoreland.Extensions.Http
{
    public class DefaultHttpMessageHandlerBuilder<T> : IHttpMessageHandlerBuilder<T>
    {
        private readonly List<Func<T, DelegatingHandler>> _additionalHandlers;

        public DefaultHttpMessageHandlerBuilder()
        {
            _additionalHandlers = new List<Func<T, DelegatingHandler>>();
        }

        /// <inheritdoc/>
        public Func<T, HttpMessageHandler>? PrimaryHandler { get; internal set; }

        /// <inheritdoc/>
        public IEnumerable<Func<T, DelegatingHandler>> AdditionalHandlers => 
            _additionalHandlers.AsEnumerable();

        /// <inheritdoc/>
        public HttpMessageHandler Build(T argument)
        {
            var primary = PrimaryHandler?.Invoke(argument) ?? new HttpClientHandler();

            var previous = primary;
            HttpMessageHandler? topMost = null;
            for (int i = _additionalHandlers.Count - 1; i >= 0; i--)
            {
                var delegatingHandler = _additionalHandlers[i](argument);
                delegatingHandler.InnerHandler = previous;
                previous = delegatingHandler;
                topMost ??= previous;
            }
            topMost ??= primary;
            return topMost!;
        }
    }
}
