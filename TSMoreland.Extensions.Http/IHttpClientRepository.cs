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

namespace TSMoreland.Extensions.Http
{
    /// <summary>
    /// <para>
    /// Extended version of <see cref="IHttpClientFactory"/> based heavily on
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs
    /// </para>
    /// <para>
    /// Intention is to provide repository like behaviour for <see cref="IHttpClientFactory"/> by allow
    /// <see cref="HttpMessageHandler"/> objects to be built using dynamic settings.  The reasoning for this class is that
    /// <see cref="IHttpClientFactory"/> depends on all named clients being registered during service configuration while
    /// this class allows them to be added, updated or removed at runtime.
    /// </para>
    /// </summary>
    public interface IHttpClientRepository : IHttpClientFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public HttpClient CreateClient(string name, object options);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="builder"></param>
        public void AddOrUpdate(string name, Func<object, HttpMessageHandler> builder);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool TryRemoveClient(string name);
    }
}
