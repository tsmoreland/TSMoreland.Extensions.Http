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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using TSMoreland.Extensions.Http.Abstractions;
using TSMoreland.Extensions.Http.Internal;

namespace TSMoreland.Extensions.Http.Tests
{
    [TestFixture]
    public class HttpClientRepositoryTests
    {
        private Mock<ILogger<HttpClientRepository<object>>> _logger = null!;
        private Mock<IServiceScopeFactory> _serviceScopeFactory = null!;
        private Mock<IServiceScope> _serviceScope = null!;
        private Mock<IServiceProvider> _serviceProvider = null!;
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

            _serviceProvider = new Mock<IServiceProvider>();
            _serviceScope = new Mock<IServiceScope>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScopeFactory
                .Setup(f => f.CreateScope())
                .Returns(_serviceScope.Object);
            _serviceScope
                .SetupGet(s => s.ServiceProvider)
                .Returns(_serviceProvider.Object);

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
        public void AddOrUpdate_ThrowsArgumentNullException_WhenNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = _repository.AddIfNotPresent(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddOrUpdate_ThrowsArgumentException_WhenNameIsEmpty()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _repository.AddIfNotPresent(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AddOrUpdate_AddsHandler_WhenNameNotFound()
        {
            _repository.AddIfNotPresent("alpha");
            Assert.That(_repository.MessageHandlersByName.ContainsKey("alpha"), Is.True);
        }

        [Test]
        public void AddOrUpdate_UpdatesHandler_WhenNameFound()
        {
            _repository.AddIfNotPresent("alpha");
            var expectedCount = _repository.MessageHandlersByName.Count;
            _repository.MessageHandlersByName.TryGetValue("alpha", out var firstHandler);

            _repository.AddOrReplace("alpha");
            _repository.MessageHandlersByName.TryGetValue("alpha", out var secondHandler);

            Assert.That(_repository.MessageHandlersByName.Count, Is.EqualTo(expectedCount));
            Assert.That(ReferenceEquals(firstHandler, secondHandler), Is.False);

        }

        [Test]
        public void ContainsName_ThrowsArgumentNullException_WhenNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = _repository.ContainsName(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void ContainsName_ThrowsArgumentException_WhenNameIsEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(() => _ = _repository.ContainsName(""));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void ContainsName_ReturnsFalse_WhenNameNotFound()
        {
            Assert.That(_repository.ContainsName("alpha"), Is.False);
        }

        [Test]
        public void ContainsName_ReturnsTrue_WhenNameFound()
        {
            _repository.AddIfNotPresent("alpha");
            Assert.That(_repository.ContainsName("alpha"), Is.True);
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
            _repository.AddIfNotPresent("alpha");
            Assert.That(_repository.TryRemoveClient("alpha"), Is.True);
        }

        [Test]
        public void TryRemoveClient_RemovesActiveHandler_WhenNamedClientCreated()
        {
            _repository.AddIfNotPresent("alpha");
            _repository._activeHandlerLifetime = TimeSpan.FromSeconds(1);
            var client = _repository.CreateClient("alpha", new object());
            _repository.TryRemoveClient("alpha");

            Assert.That(_repository._activeHandlers.Count, Is.Zero);
            GC.KeepAlive(client); // don't want GC disposing too early and impacting on the test
        }

        [Test]
        public void TryRemoveClient_ClientAddedToExpiryQueue_WhenNamedClientCreated()
        {
            _repository.AddIfNotPresent("alpha");
            _repository._activeHandlerLifetime = TimeSpan.FromSeconds(1);
            var client = _repository.CreateClient("alpha", new object());
            _repository.TryRemoveClient("alpha");

            Thread.Sleep(TimeSpan.FromSeconds(1.1));
            Assert.That(_repository._expiredHandlers.Count, Is.Not.Zero);

            GC.KeepAlive(client); // don't want GC disposing too early and impacting on the test
        }

        [Test]
        public void CreateClient_DoesNotThrow_WhenNameNotFound()
        {
            Assert.DoesNotThrow(() => _ = _repository.CreateClient("bravo"));
        }

        [Test]
        public void CreateClient_ReturnsClientBuildFromInnerFactory()
        {
            _ = _repository.CreateClient("alpha");
            _clientFactory.Verify(f => f.CreateClient("alpha"), Times.Once);
        }

        [Test]
        public void CreateClientWithArgument_ReturnsClientBuildFromInnerFactory_WhenNameNotFound()
        {
            _ = _repository.CreateClient("alpha", new object());
            _clientFactory.Verify(f => f.CreateClient("alpha"), Times.Once);
        }

        [Test]
        public void CreateClientWithArgument_DoesNotThrow_WhenNameNotFound()
        {
            Assert.DoesNotThrow(() => _ = _repository.CreateClient("zulu", new object()));
        }

        [Test]
        public void CreateClientWithArgument_ThrowsArgumentNullException_WhenNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _repository.CreateClient(null!, new object()));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }
        [Test]
        public void CreateClientWithArgument_ThrowsArgumentException_WhenNameIsEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(() => _repository.CreateClient("", new object()));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }


        [Test]
        public void CreateClient_WithArgument_BuildsHandlerUsingArgument_WhenNamedClientBuilderReturnsHandler()
        {
            const string argument = "beta";
            var expectedInnerHandler = Mock.Of<HttpMessageHandler>();
            Mock<Func<object, IServiceProvider, HttpMessageHandler>> configuerer = new();
            configuerer
                .Setup(b => b.Invoke(argument, _serviceProvider.Object))
                .Returns(expectedInnerHandler);
            _repository.AddIfNotPresent("alpha")
                .ConfigurePrimaryHandler(configuerer.Object);

            var client = _repository.CreateClient("alpha", argument);
            var handler = GetInternalHandlerFromClientOrNull(client);

            configuerer.Verify(b => b.Invoke(argument, _serviceProvider.Object), Times.Once);
            Assert.That(ReferenceEquals(handler, expectedInnerHandler), Is.True);
        }

        [Test]
        public async Task CreateClient_BuildUsingMessageHandlerFactory_WhenBuilderRemovedMidCreation()
        {
            TimeSpan maxWaitTime = TimeSpan.FromSeconds(5);
            List<Task> tasks = new();
            ConcurrentBag<HttpClient> clients = new();

            DateTime startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime) < maxWaitTime)
            {
                clients.Clear();
                tasks.Clear();
                for (int i = 0; i < Environment.ProcessorCount; i += 3)
                {
                    tasks.AddRange(new[] {Task.Run(Add), Task.Run(Create), Task.Run(Remove)});
                }
                await Task.WhenAll(tasks.ToArray());

                try
                {
                    _messageHandlerFactory.Verify(f => f.CreateHandler("alpha"), Times.AtLeastOnce);
                    break;
                }
                catch (MockException)
                {
                    // ... haven't hit the race condition yet ...
                }
            }

            try
            {
                _messageHandlerFactory.Verify(f => f.CreateHandler("alpha"), Times.AtLeastOnce);
            }
            catch (MockException)
            {
                Assert.Inconclusive("This test relies on a race condition which we cannot guarentee to hit.  As a result we're never really sure if we fail or simply didn't hit the condition.");
            }

            void Add() => _repository.AddIfNotPresent("alpha");
            void Remove() => _repository.TryRemoveClient("alpha");
            void Create() => clients.Add(_repository.CreateClient("alpha", new object()));
        }

        [Test]
        public async Task CreateClient_HandlerIsDisposed_WhenExpired()
        {
            TimeSpan maxWaitTime = TimeSpan.FromSeconds(5);
            var repository = new HttpClientRepository<object>(
                _clientFactory.Object,
                _messageHandlerFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                TimeSpan.FromMilliseconds(100)) 
            {
                _activeHandlerLifetime = TimeSpan.FromMilliseconds(100)
            };
            repository.AddIfNotPresent("alpha");

            _ = repository.CreateClient("alpha", new object());

            DateTime startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime) < maxWaitTime)
            {
                GC.Collect();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                GC.Collect();
                if (repository._expiredHandlers.Count == 0)
                {
                    break;
                }
            }

            Assert.That(repository._expiredHandlers.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task CreateClient_ExpiredClientReAdds_WhenRemovedHandlerNotExpectedValue()
        {
            TimeSpan maxWaitTime = TimeSpan.FromSeconds(5);
            var repository = new HttpClientRepository<object>(
                _clientFactory.Object,
                _messageHandlerFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                TimeSpan.FromMilliseconds(100)) 
            {
                _activeHandlerLifetime = TimeSpan.FromMilliseconds(100)
            };
            repository.AddIfNotPresent("alpha");
            _ = repository.CreateClient("alpha", new object());
            repository.TryRemoveClient("alpha");

            repository._activeHandlerLifetime = TimeSpan.FromMinutes(2);
            repository.AddOrReplace("alpha");
            _ = repository.CreateClient("alpha", new object());

            DateTime startTime = DateTime.UtcNow;
            while (repository._expiredHandlers.Count != 0 && (DateTime.UtcNow - startTime) < maxWaitTime)
            {
                GC.Collect();
                await Task.Delay(1500);
                GC.Collect();
            }

            // bit complicated but we have 2 clients, 1 was forcibly removed but still set to expire - when it expires
            // it would attempt to remove the active handler by name - and succeed becuase of the second client
            // upon checking the value of the removed object we see it's not the one we expected to remove - so we re-add it
            // leaving us with a total of 1 active handlers
            Assert.That(repository._activeHandlers.Count, Is.EqualTo(1));
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


        /// <summary>
        /// <para>
        /// Based on
        /// <a href="https://github.com/dotnet/runtime/blob/8b42b7fa31c7c1a55b1b36213db9ef6f562ed61e/src/libraries/Microsoft.Extensions.Http/tests/Microsoft.Extensions.Http.Tests/DefaultHttpClientFactoryTest.cs#L63">
        /// DefaultHttpClientFactoryTest.Factory_MultipleCalls_CachesHandler
        /// </a>
        /// </para>
        /// <para>
        /// Licensed to the .NET Foundation under one or more agreements.<br/>
        /// The .NET Foundation licenses this file to you under the MIT license.
        /// </para>
        /// </summary>
        [Test]
        public void Repository_MultipleCalls_CachesHandler()
        {
            // Arrange
            var repository = new HttpClientRepository<object>(_clientFactory.Object, _messageHandlerFactory.Object, _serviceScopeFactory.Object, _logger.Object);
            var count = 0;
            repository.AddIfNotPresent("alpha")
                .ConfigurePrimaryHandler((@object, serviceProvider) =>
                {
                    count++;
                    return DefaultHttpMessageHandlerBuilder<object>.DefaultHandler(@object, serviceProvider);
                });

            // Act 1
            var client1 = repository.CreateClient("alpha", new object());

            // Act 2
            var client2 = repository.CreateClient("alpha", new object());

            // Assert
            Assert.That(count, Is.EqualTo(1));
            Assert.That(client1, Is.Not.EqualTo(client2));
        }

        /// <summary>
        /// <para>
        /// Based on
        /// <a href="https://github.com/dotnet/runtime/blob/8b42b7fa31c7c1a55b1b36213db9ef6f562ed61e/src/libraries/Microsoft.Extensions.Http/tests/Microsoft.Extensions.Http.Tests/DefaultHttpClientFactoryTest.cs#L86">
        /// DefaultHttpClientFactoryTest.Factory_DisposeClient_DoesNotDisposeHandler
        /// </a>
        /// </para>
        /// <para>
        /// Licensed to the .NET Foundation under one or more agreements.<br/>
        /// The .NET Foundation licenses this file to you under the MIT license.
        /// </para>
        /// </summary>
        [Test]
        public void Repository_DisposeClient_DoesNotDisposeHandler()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup("Dispose", ItExpr.IsAny<bool>())
                .Throws(new Exception("Dispose should not be called"));
            var repository = new HttpClientRepository<object>(_clientFactory.Object, _messageHandlerFactory.Object, _serviceScopeFactory.Object, _logger.Object);
            repository.AddIfNotPresent("alpha")
                .ConfigurePrimaryHandler((_, _) => mockHandler.Object);

            // Act / Assert 
            Assert.DoesNotThrow(() => 
            {
                using (repository.CreateClient("alpha", new object()))
                {
                }
            });
        }
    }
}