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

namespace TSMoreland.Extensions.Http
{
    public interface IHttpMessageHandlerBuilder<T>
    {
        /// <summary>
        /// a delegate that will be used to configure the primary <see cref="HttpMessageHandler"/> for a
        /// named <see cref="HttpClient"/> 
        /// </summary>
        public Func<T, HttpMessageHandler>? PrimaryHandler { get; }

        /// <summary>
        /// Delegates which willb e used to configure a named <see cref="HttpClient"/>
        /// </summary>
        public IEnumerable<Func<T, DelegatingHandler>> AdditionalHandlers { get; }

        /// <summary>
        /// Builds an instance of <see cref="HttpMessageHandler"/> using the configured 
        /// <see cref="PrimaryHandler"/> and optional <see cref="AdditionalHandlers"/>
        /// </summary>
        /// <param name="argument">argument used when constructing the handler</param>
        /// <returns>A new instance of <see cref="HttpMessageHandler"/></returns>
        /// <remarks>
        /// If <see cref="PrimaryHandler"/> is null then an instance of <see cref="HttpClientHandler"/>
        /// will be used with default settings.
        /// </remarks>
        public HttpMessageHandler Build(T argument);
    }
}
