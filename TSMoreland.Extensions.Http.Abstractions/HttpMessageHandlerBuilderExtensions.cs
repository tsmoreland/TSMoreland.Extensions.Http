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
using System.Net.Http;
using TSMoreland.GuardAssertions;

namespace TSMoreland.Extensions.Http.Abstractions
{
    public static class HttpMessageHandlerBuilderExtensions
    {
        public static IHttpMessageHandlerBuilder<T> ConfigurePrimaryHandler<T>(
            this IHttpMessageHandlerBuilder<T> builder,
            Func<T, IServiceProvider, HttpMessageHandler> configureHandler)
        {
            Guard.Against.ArgumentNull(builder, nameof(builder));
            Guard.Against.ArgumentNull(configureHandler, nameof(configureHandler));

            builder.PrimaryHandlerFactory = configureHandler;
            return builder;
        }


        /// <summary>
        /// Adds a delegate that will be used to create an additional message handler for a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="configureHandler">A delegate that is used to create a <see cref="DelegatingHandler"/>.</param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// The <see paramref="configureHandler"/> delegate should return a new instance of the message handler each time it
        /// is invoked.
        /// </para>
        /// <para>
        /// The <see cref="IServiceProvider"/> argument provided to <paramref name="configureHandler"/> will be
        /// a reference to a scoped service provider that shares the lifetime of the handler being constructed.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddHttpMessageHandler<T>(this IHttpMessageHandlerBuilder<T> builder, Func<T, IServiceProvider, DelegatingHandler> configureHandler)
        {
            Guard.Against.ArgumentNull(builder, nameof(builder));
            Guard.Against.ArgumentNull(configureHandler, nameof(configureHandler));

            builder.AdditionalHandlers.Add(configureHandler);
            return builder;
        }

    }
}
