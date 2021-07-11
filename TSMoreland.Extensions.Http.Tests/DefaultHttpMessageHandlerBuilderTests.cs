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
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using TSMoreland.Extensions.Http.Abstractions;

namespace TSMoreland.Extensions.Http.Tests
{
    [TestFixture]
    public sealed class DefaultHttpMessageHandlerBuilderTests
    {
        private Mock<IServiceProvider> _serviceProvider = null!;


        [SetUp]
        public void Setup()
        {
            _serviceProvider = new Mock<IServiceProvider>();
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new DefaultHttpMessageHandlerBuilder<object>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void Constructor_ThrowsArgumentException_WhenNameIsEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(() => _ = new DefaultHttpMessageHandlerBuilder<object>(string.Empty));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void PrimaryHandlerFactory_ThrowsArgumentNullException_WhenSetValueIsNull()
        {
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha");

            var ex = Assert.Throws<ArgumentNullException>(() => builder.PrimaryHandlerFactory = null!);
            Assert.That(ex!.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void PrimaryHandlerFactory_ReturnsDefaultHandler_WhenNotSet()
        {
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha");
            Assert.That(builder.PrimaryHandlerFactory, Is.EqualTo((Func<object, IServiceProvider, HttpMessageHandler>) DefaultHttpMessageHandlerBuilder<object>.DefaultHandler));
        }

        [Test]
        public void PrimaryHandlerFactory_ReturnsSetvalue_WhenSet()
        {
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha");
            static HttpMessageHandler Expected(object @object, IServiceProvider _) => new MockHttpMessageHandler<object>(@object);

            builder.PrimaryHandlerFactory = Expected;

            Assert.That(builder.PrimaryHandlerFactory, Is.EqualTo((Func<object, IServiceProvider, HttpMessageHandler>) Expected));
        }

        [Test]
        public void Build_ThrowsHttpMessageHandlerBuilderException_WhenPrimaryHandlerFactoryReturnsNull()
        {
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha")
                .ConfigurePrimaryHandler((_, _) => null!);

            Assert.Throws<HttpMessageHandlerBuilderException>(() =>
                _ = builder.Build(new object(), _serviceProvider.Object));
        }

        [Test]
        public void Build_ThrowsHttpMessageHandlerBuilderException_WhenAnyAdditionalHandlerReturnsNull()
        {
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha")
                .AddHttpMessageHandler((_, _) => null!);

            Assert.Throws<HttpMessageHandlerBuilderException>(() =>
                _ = builder.Build(new object(), _serviceProvider.Object));
        }

        /// <summary>
        /// like a stack the handlers are used in order of last added down to the primary handler
        /// </summary>
        [Test]
        public void Build_UsesAdditionalHandlersInReverseOrder_WhenValid()
        {
            // Arrange
            var expected = new[]
            {
                new EmptyDelegatingHandler(1),
                new EmptyDelegatingHandler(2),
                new EmptyDelegatingHandler(3),
                new EmptyDelegatingHandler(4),
            };

            var primaryHandler = Mock.Of<HttpMessageHandler>();
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha")
                .ConfigurePrimaryHandler((_, _) => primaryHandler);
            builder = expected.Aggregate(builder, (current, next) => current.AddHttpMessageHandler((_, _) => next));

            // Act
            var handler = builder.Build(new object(), Mock.Of<IServiceProvider>());

            // Assert

            var delegatingHandler = (DelegatingHandler) handler; 
            for (int i = expected.Length - 1; i > 0; i--)
            {
                Assert.That(delegatingHandler, Is.EqualTo(expected[i]));
                if (delegatingHandler.InnerHandler is DelegatingHandler innerDelegatingHandler)
                {
                    delegatingHandler = innerDelegatingHandler;
                }
            }

        }

        [Test]
        public void Build_UsesPrimaryHandlerLast_WhenValid()
        {
            // Arrange
            var expected = new[]
            {
                new EmptyDelegatingHandler(1),
                new EmptyDelegatingHandler(2),
            };

            var expectedPrimary = Mock.Of<HttpMessageHandler>();
            var builder = new DefaultHttpMessageHandlerBuilder<object>("alpha")
                .ConfigurePrimaryHandler((_, _) => expectedPrimary);
            builder = expected.Aggregate(builder, (current, next) => current.AddHttpMessageHandler((_, _) => next));

            // Act
            var handler = builder.Build(new object(), Mock.Of<IServiceProvider>());

            // Assert

            HttpMessageHandler? actualPrimary = null;
            var delegatingHandler = (DelegatingHandler) handler; 
            for (int i = expected.Length - 1; i >= 0; i--)
            {
                if (delegatingHandler.InnerHandler is DelegatingHandler innerDelegatingHandler)
                {
                    delegatingHandler = innerDelegatingHandler;
                }
                else
                {
                    actualPrimary = delegatingHandler.InnerHandler;
                }
            }

            Assert.That(actualPrimary, Is.EqualTo(expectedPrimary));

        }

    }
}
