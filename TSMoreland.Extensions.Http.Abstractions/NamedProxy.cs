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
using System.Net;
using System.Net.Http;

namespace TSMoreland.Extensions.Http.Abstractions
{
    public sealed class NamedProxy : IEqualityComparer<NamedProxy>
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="NamedProxy"/> class.
        /// </summary>
        /// <param name="identifier">unique identifier used to compare instances of <see cref="NamedProxy"/></param>
        /// <param name="proxy">proxy details to be used when creating <see cref="HttpMessageHandler"/></param>
        public NamedProxy(string identifier, IWebProxy? proxy)
        {
            if (identifier is not {Length: > 0})
            {
                throw new ArgumentException("identifier cannot be null or empty.", nameof(identifier));
            }

            Identifier = identifier;
            Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }

        /// <summary>
        /// Unique identifier used to compare instance of <see cref="NamedProxy"/>
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Proxy Settings for use when creating <see cref="HttpMessageHandler"/>
        /// </summary>
        public IWebProxy Proxy { get; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            return obj is NamedProxy that && Equals(this, that);
        }

        /// <inheritdoc cref="IEqualityComparer{NamedProxy}.Equals(NamedProxy, NamedProxy)"/>
        public bool Equals(NamedProxy x, NamedProxy y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null!))
            {
                return false;
            }

            if (ReferenceEquals(y, null!))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Identifier == y.Identifier;
        }

        /// <inheritdoc cref="IEqualityComparer{NamedProxy}.GetHashCode(NamedProxy)"/>
        public int GetHashCode(NamedProxy? obj)
        {
            return obj?.Identifier.GetHashCode() ?? 0;
        }
    }
}
