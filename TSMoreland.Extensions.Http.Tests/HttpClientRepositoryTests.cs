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
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace TSMoreland.Extensions.Http.Tests
{
    [TestFixture]
    public class HttpClientRepositoryTests
    {
        private IServiceProvider _serviceProvider = null!;
        private Mock<ILogger<HttpClientRepository>> _logger = null!;
        private Mock<IHttpClientFactory> _clientFactory = null!;

        private HttpClientRepository _repository = null!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            _serviceProvider = serviceCollection.BuildServiceProvider();

        }

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<HttpClientRepository>>();
            _clientFactory = new Mock<IHttpClientFactory>();

            _repository = new HttpClientRepository(_serviceProvider.GetService<IHttpClientFactory>()!, _logger.Object);
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenHttpClientFactoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository(null!, _logger.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("clientFactory"));

        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {

            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository(_clientFactory.Object, null!));
            Assert.That(ex!.ParamName, Is.EqualTo("logger"));

        }

        [Test]
        public void Constructor_ThrowsArgumentException_WhenHttpClientFactoryDoesNotContainExpectedInternals()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _ = new HttpClientRepository(_clientFactory.Object, _logger.Object));
            Assert.That(ex.ParamName, Is.EqualTo("clientFactory"));
        }

        [Test]
        public void Constructor_DoesNotThrow_WhenHttpClientFactoryContainsExpectedInternals()
        {
            ConcurrentDictionary<string, object> x = new();

            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>()!;
            Assert.DoesNotThrow(() => _ = new HttpClientRepository(httpClientFactory, _logger.Object));
        }

        [Test]
        public void TryRemove_ReturnsFalse_WhenNameNotFound()
        {
            Assert.That(_repository.TryRemoveClient("alpha"), Is.EqualTo(false));
        }
    }
}