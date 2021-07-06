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

namespace TSMoreland.Extensions.Http
{
    /// <summary>
    /// Builder function use to create and configure a <see cref="HttpMessageHandler"/> using any
    /// <paramref name="argument"/> to configure 
    /// </summary>
    /// <typeparam name="T">argument type, must match <see cref="IHttpClientRepository{T}"/></typeparam>
    /// <param name="argument">argument used to determine how <see cref="HttpMessageHandler"/> should be created</param>
    /// <returns>
    /// new instance of <see cref="HttpMessageHandler"/> configured using <paramref name="argument"/>
    /// </returns>
    public delegate HttpMessageHandler BuildHttpMessageHandler<in T>(T argument);

    /// <summary>
    /// <para>
    /// Extended version of <see cref="IHttpClientFactory"/> based heavily on
    /// <a href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs">
    /// DefaultHttpClientFactory.cs
    /// </a>
    /// </para>
    /// <para>
    /// Intention is to provide repository like behaviour for <see cref="IHttpClientFactory"/> by allow
    /// <see cref="HttpMessageHandler"/> objects to be built using dynamic settings.  The reasoning for this class is that
    /// <see cref="IHttpClientFactory"/> depends on all named clients being registered during service configuration while
    /// this class allows them to be added, updated or removed at runtime.
    /// </para>
    /// </summary>
    public interface IHttpClientRepository<T> : IHttpClientFactory
    {
        /// <summary>
        /// Creates and configures an <see cref="HttpClient"/> instance using the configuration that corresponds
        /// to the logical name specified by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The logical name of the client to create.</param>
        /// <param name="argument">
        /// argument used to create client if name is recognized wihtin the
        /// <see cref="IHttpClientRepository{T}"/>
        /// </param>
        /// <returns>A new <see cref="HttpClient"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// Each call to <see cref="CreateClient(string, T)"/> is guaranteed to return a new <see cref="HttpClient"/>
        /// instance. It is generally not necessary to dispose of the <see cref="HttpClient"/> as the
        /// <see cref="IHttpClientRepository{T}"/> tracks and disposes resources used by the <see cref="HttpClient"/>.
        /// </para>
        /// <para>
        /// Callers are also free to mutate the returned <see cref="HttpClient"/> instance's public properties
        /// as desired.
        /// </para>
        /// <para>
        /// if <paramref name="name"/> is not recognized by the <see cref="IHttpClientRepository{T}"/>
        /// then creation will be handled by an inner <see cref="IHttpClientFactory"/> using
        /// <see cref="IHttpClientFactory.CreateClient(string)"/>
        /// </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">if <paramref name="name"/> is <see langword="null" /></exception>
        /// <exception cref="System.ArgumentException">if <paramref name="name"/> is empty</exception>
        public HttpClient CreateClient(string name, T argument);

        /// <summary>
        /// Uses the specified functions to add a key/value pair to the
        /// <see cref="IHttpClientRepository{T}" /> if the name does not already exist,
        /// or to update a key/value pair in the <see cref="IHttpClientRepository{T}" />
        /// if the name already exists.</summary>
        /// <param name="name">The name to be added or whose value should be updated</param>
        /// <param name="builder">The function used to generate a value for an absent key</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="name" />, <paramref name="builder" />, is <see langword="null" />.</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
        /// <returns>
        /// The new value for the key. This will be be the result of
        /// <paramref name="builder" />
        /// </returns>
        public BuildHttpMessageHandler<T> AddOrUpdate(string name, BuildHttpMessageHandler<T> builder);

        /// <summary>
        /// Determines whether the <see cref="IHttpClientRepository{T}" /> contains the specified name.
        /// </summary>
        /// <param name="name">The name to locate in the <see cref="IHttpClientRepository{T}" />.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="name" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="name" /> is empty.
        /// </exception>
        /// <returns>
        /// <see langword="true" /> if the <see cref="IHttpClientRepository{T}" /> contains an element with the specified name;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool ContainsName(string name);

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the
        /// <see cref="IHttpClientRepository{T}" />.
        /// </summary>
        /// <param name="name">The name of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="name" /> is  <see langword="null" />.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="name" /> is  empty.
        /// </exception>
        /// <returns>
        /// <see langword="true" /> if the object was removed successfully; otherwise, <see langword="false" />.</returns>
        public bool TryRemoveClient(string name);
    }
}
