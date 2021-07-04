//
// Copyright � 2021 Terry Moreland
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
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TSMoreland.Extensions.Http.Internal;

namespace TSMoreland.Extensions.Http.Tests
{
    [TestFixture]
    public class HttpClientRepositoryTests
    {
        private Mock<ILogger<HttpClientRepository<object>>> _logger = null!;
        private Mock<IServiceScopeFactory> _serviceScopeFactory = null!;
        private Mock<IServiceScope> _serviceScope = null!;
        private Mock<IHttpClientFactory> _clientFactory = null!;
        private Mock<IHttpMessageHandlerFactory> _messageHandlerFactory = null!;
        private HttpClient _createdClient = null!;
        private Mock<HttpMessageHandler> _messageHandler = null!;

        private HttpClientRepository<object> _repository = null!;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger<HttpClientRepository<object>>>();

            _messageHandler = new Mock<HttpMessageHandler>();
            _createdClient = new HttpClient(_messageHandler.Object);
            _clientFactory = new Mock<IHttpClientFactory>();
            _clientFactory
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(_createdClient);

            _messageHandlerFactory = new Mock<IHttpMessageHandlerFactory>();
            _messageHandlerFactory
                .Setup(factory => factory.CreateHandler(It.IsAny<string>()))
                .Returns(_messageHandler.Object);

            _serviceScope = new Mock<IServiceScope>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScopeFactory
                .Setup(f => f.CreateScope())
                .Returns(_serviceScope.Object);

            _repository = new HttpClientRepository<object>(
                _clientFactory.Object,
                _messageHandlerFactory.Object,
                _serviceScopeFactory.Object, 
                _logger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _createdClient.Dispose(); // disposes the handler as well
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenHttpClientFactoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository<object>(null!, _messageHandlerFactory.Object, _serviceScopeFactory.Object, _logger.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("clientFactory"));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenHttpMessageHandlerFactoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository<object>(_clientFactory.Object, null!, _serviceScopeFactory.Object, _logger.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("messageHandlerFactory"));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenOptionsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository<object>(_clientFactory.Object, _messageHandlerFactory.Object, null!, _logger.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("scopeFactory"));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new HttpClientRepository<object>(_clientFactory.Object, _messageHandlerFactory.Object, _serviceScopeFactory.Object, null!));
            Assert.That(ex!.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public void Constructor_DoesNotThrow_WhenHttpClientFactoryContainsExpectedInternals()
        {
            Assert.DoesNotThrow(() => _ = new HttpClientRepository<object>(_clientFactory.Object, _messageHandlerFactory.Object, _serviceScopeFactory.Object, _logger.Object));
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
        public void CreateClientWithArgument_DoesNotThrow_WhenNameNotFound()
        {
            Assert.DoesNotThrow(() => _ = _repository.CreateClient("zulu", new object()));
        }

        [Test]
        public void AddOrUpdate_ThrowsArgumentNullException_WhenNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = _repository.AddOrUpdate(null!, @object => new MockHttpMessageHandler<object>(@object)));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddOrUpdate_ThrowsArgumentException_WhenNameIsEmpty()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _repository.AddOrUpdate(null!, _ => _messageHandler.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddOrUpdate_AddsHandler_WhenNameNotFound()
        {
            _repository.AddOrUpdate("alpha", _ => _messageHandler.Object);
            Assert.That(_repository.MessageHandlersByName.ContainsKey("alpha"), Is.True);
        }

        [Test]
        public void AddOrUpdate_UpdatesHandler_WhenNameFound()
        {
            _repository.AddOrUpdate("alpha", _ => _messageHandler.Object);
            var expectedCount = _repository.MessageHandlersByName.Count;
            _repository.MessageHandlersByName.TryGetValue("alpha", out var firstHandler);

            _repository.AddOrUpdate("alpha", @object => new MockHttpMessageHandler<object>(@object));
            _repository.MessageHandlersByName.TryGetValue("alpha", out var secondHandler);

            Assert.That(_repository.MessageHandlersByName.Count, Is.EqualTo(expectedCount));
            Assert.That(ReferenceEquals(firstHandler, secondHandler), Is.False);

        }

        [Test]
        public void AddOrUpdate_ThrowsArgumentNullException_WhenBuilderIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = _repository.AddOrUpdate("alpha", null!));
            Assert.That(ex!.ParamName, Is.EqualTo("builder"));
        }

        [Test]
        public void TryRemoveClient_ThrowsArgumentNullException_WhenNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _repository.TryRemoveClient(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void TryRemoveClient_ThrowsArgumentException_WhenNameIsEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(() => _repository.TryRemoveClient(""));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void TryRemoveClient_ReturnsFalse_WhenNameNotFound()
        {
            Assert.That(_repository.TryRemoveClient("alpha"), Is.False);
        }

        [Test]
        public void TryRemoveClient_ReturnsTrue_WhenNameFound()
        {
            _repository.AddOrUpdate("alpha", _ => _messageHandler.Object);
            Assert.That(_repository.TryRemoveClient("alpha"), Is.True);
        }

        /*
        [Test]
        public void TryRemoveClient_RemvoesActiveHandlerButNotExpiry_WhenNamedClientCreated()
        {
            _repository.AddOrUpdate("alpha", _ => _messageHandler.Object);
            var client = _repository.CreateClient("alpha", new object());
            _repository.TryRemoveClient("alpha");

            Assert.That(_repository._activeHandlers.Count, Is.Zero);
            Assert.That(_repository._expiredHandlers.Count, Is.Not.Zero);


            GC.KeepAlive(client);
        }
        */

        [Test]
        public void CreateClient_WithArgument_BuildsHandlerUsingArgument_WhenNamedClientBuilderReturnsHandler()
        {
            const string argument = "beta";
            var expectedInnerHandler = new MockHttpMessageHandler<object>(argument);
            Mock<BuildHttpMessageHandler<object>> builder = new();
            builder
                .Setup(b => b.Invoke(argument))
                .Returns(expectedInnerHandler);
            _repository.AddOrUpdate("alpha", builder.Object);

            var client = _repository.CreateClient("alpha", argument);
            var handler = GetInternalHandlerFromClientOrNull(client);

            builder.Verify(b => b.Invoke(argument), Times.Once);
            Assert.That(ReferenceEquals(handler, expectedInnerHandler), Is.True);
        }

        [Test]
        public void CreateClient_WithArgument_ThrowsArgumentNullException_WhenNamedClientBuilderReturnsNull()
        {
            const string argument = "beta";
            Mock<BuildHttpMessageHandler<object>> builder = new();
            builder
                .Setup(b => b.Invoke(argument))
                .Returns((MockHttpMessageHandler<object>) null!);
            _repository.AddOrUpdate("alpha", builder.Object);
            Assert.Throws<ArgumentNullException>(() => _ = _repository.CreateClient("alpha", argument));
        }

        private static HttpMessageHandler? GetInternalHandlerFromClientOrNull(HttpClient client)
        {
            var handler = typeof(HttpMessageInvoker)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(f => string.Equals("_handler", f.Name, StringComparison.Ordinal))
                .Select(f => f.GetValue(client))
                .OfType<HttpMessageHandler?>()
                .FirstOrDefault();

            return handler is LifetimeTrackingProxiedHttpMessageHandler proxiedHandler
                ? proxiedHandler.InnerHandler
                : handler;
        }
    }
}