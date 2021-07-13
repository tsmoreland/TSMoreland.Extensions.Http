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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TSMoreland.Extensions.Http.Abstractions;
using TSMoreland.GuardAssertions;

namespace TSMoreland.Extensions.Http.DependencyInjection
{
    public static class HttpClientRepositoryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IHttpClientRepository{T}"/> and related services to the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/></param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        /// <exception cref="ArgumentNullException">if <paramref name="services"/> is <see langword="null"/></exception>
        public static IServiceCollection AddHttpClientRepository<T>(this IServiceCollection services)
        {
            Guard.Against.ArgumentNull(services, nameof(services));

            services.AddHttpClient();

            services.TryAddSingleton<IHttpClientRepository<T>, HttpClientRepository<T>>();

            return services;
        }
    }
}
