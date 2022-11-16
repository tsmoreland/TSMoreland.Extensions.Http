//
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

using System;
using System.Collections.Generic;
using System.Net.Http;
using TSMoreland.Extensions.Http.Abstractions;

namespace TSMoreland.Extensions.Http
{
    /// <summary>
    /// Placeholder <see cref="IHttpMessageHandlerBuilder{T}"/> intended for use with <see cref="IHttpClientRepository{T}.AddIfNotPresent"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class UnusedHttpMessageHandlerBuilder<T> : IHttpMessageHandlerBuilder<T>
    {
        public UnusedHttpMessageHandlerBuilder(string name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public Func<T, IServiceProvider, HttpMessageHandler> PrimaryHandlerFactory { get; set; } =
            (_, _) => new HttpClientHandler();

        /// <inheritdoc/>
        public IList<Func<T, IServiceProvider, DelegatingHandler>> AdditionalHandlers { get; } = new List<Func<T, IServiceProvider, DelegatingHandler>>();

        /// <inheritdoc/>
        public HttpMessageHandler Build(T argument, IServiceProvider serviceProvider)
        {
            throw new NotSupportedException("Unsupported message handler builder, was provided to allow builder pattern to continue but not intended to be used");
        }
    }
}
