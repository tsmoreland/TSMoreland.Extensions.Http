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

namespace TSMoreland.Extensions.Http.Abstractions;

public interface IHttpMessageHandlerBuilder<T>
{
    /// <summary>
    /// Builder Name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// a delegate that will be used to configure the primary <see cref="HttpMessageHandler"/> for a
    /// named <see cref="HttpClient"/> 
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// when attempting to set to <see langword="null"/>
    /// </exception>
    public Func<T, IServiceProvider, HttpMessageHandler> PrimaryHandlerFactory { get; set; }

    /// <summary>
    /// Delegates which will be used to configure a named <see cref="HttpClient"/>
    /// </summary>
    public IList<Func<T, IServiceProvider, DelegatingHandler>> AdditionalHandlers { get; }

    /// <summary>
    /// Builds an instance of <see cref="HttpMessageHandler"/> using the configured 
    /// <see cref="PrimaryHandlerFactory"/> and optional <see cref="AdditionalHandlers"/>
    /// </summary>
    /// <param name="argument">argument used when constructing the handler</param>
    /// <param name="serviceProvider">service provider passed to primary handler factory and additional handlers</param>
    /// <returns>A new instance of <see cref="HttpMessageHandler"/></returns>
    /// <exception cref="ArgumentNullException">
    /// if <paramref name="serviceProvider"/> is <see langword="null"/>
    /// </exception>
    /// <remarks>
    /// If <see cref="PrimaryHandlerFactory"/> is null then an instance of <see cref="HttpClientHandler"/>
    /// will be used with default settings.
    /// </remarks>
    public HttpMessageHandler Build(T argument, IServiceProvider serviceProvider);
}
