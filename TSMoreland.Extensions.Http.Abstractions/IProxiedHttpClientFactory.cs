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

using System.Net.Http;

namespace TSMoreland.Extensions.Http.Abstractions
{
    public interface IProxiedHttpClientFactory
    {
        /// <summary>
        /// Creates and configures an <see cref="HttpClient"/> instance using the configuration that corresponds
        /// to the <see cref="NamedProxy"/> specified by <paramref name="proxy"/>.
        /// </summary>
        /// <param name="proxy">
        /// The logical name of the client to create as well as proxy details used when configuring the message handler.
        /// </param>
        /// <returns>A new <see cref="HttpClient"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// Each call to <see cref="CreateClient(NamedProxy)"/> is guaranteed to return a new <see cref="HttpClient"/>
        /// instance. It is generally not necessary to dispose of the <see cref="HttpClient"/> as the
        /// <see cref="IProxiedHttpClientFactory"/> tracks and disposes resources used by the <see cref="HttpClient"/>.
        /// </para>
        /// <para>
        /// Callers are also free to mutate the returned <see cref="HttpClient"/> instance's public properties
        /// as desired.
        /// </para>
        /// <para>
        /// most of the above details is copied from <see cref="System.Net.Http.IHttpClientFactory.CreateClient(string)"/>
        /// </para>
        /// </remarks>
        HttpClient CreateClient(NamedProxy proxy);
    }
}
