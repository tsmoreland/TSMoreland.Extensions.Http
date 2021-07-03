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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
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
        private Mock<IServiceScopeFactory> _serviceScopeFactory = null!;
        private Mock<IServiceScope> _serviceScope = null!;
        private Mock<IHttpClientFactory> _clientFactory = null!;

        private HttpClientRepository _repository = null!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();

            serviceCollection
                .AddHttpClient("bravo")
                .AddHttpMessageHandler(() => new LoggingHttpMessageHandler(_logger.Object));

            serviceCollection
                .AddHttpClient("charlie")
                .AddHttpMessageHandler(() => new LoggingHttpMessageHandler(_logger.Object));

            _serviceProvider = serviceCollection.BuildServiceProvider();

        }

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<HttpClientRepository>>();
            _clientFactory = new Mock<IHttpClientFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScopeFactory
                .Setup(f => f.CreateScope())
                .Returns(_serviceScope.Object);

            _repository = new HttpClientRepository(_serviceProvider.GetService<IHttpClientFactory>()!, _serviceScopeFactory.Object, _logger.Object);
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenHttpClientFactoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository(null!, _serviceScopeFactory.Object, _logger.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("clientFactory"));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenOptionsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository(_clientFactory.Object, null!, _logger.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("scopeFactory"));

        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {

            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository(_clientFactory.Object, _serviceScopeFactory.Object, null!));
            Assert.That(ex!.ParamName, Is.EqualTo("logger"));

        }

        [Test]
        public void Constructor_DoesNotThrow_WhenHttpClientFactoryContainsExpectedInternals()
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>()!;
            Assert.DoesNotThrow(() => _ = new HttpClientRepository(httpClientFactory, _serviceScopeFactory.Object, _logger.Object));
        }

        [Test]
        public void TryRemove_ReturnsFalse_WhenNameNotFound()
        {
            Assert.That(_repository.TryRemoveClient("alpha"), Is.EqualTo(false));
        }

        [Test]
        public void CreateClient_DoesNotThrow_WhenNameNotFound()
        {
            Assert.DoesNotThrow(() => _ = _repository.CreateClient("bravo"));
        }
        [Test]
        public void CreateClientWithOptions_DoesNotThrow_WhenNameNotFound()
        {
            Assert.DoesNotThrow(() => _ = _repository.CreateClient("bravo", new object()));
        }
    }
}