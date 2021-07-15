using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using TSMoreland.Extensions.Http.Abstractions;

namespace TSMoreland.Extensions.Http.Polly.Tests
{
    public sealed class MockHttpMessageHandlerBuilder<T> : IHttpMessageHandlerBuilder<T>
    {

        public MockHttpMessageHandlerBuilder(string name = "mockBuilder")
        {
            Name = name;
            MockBuilder = new Mock<IHttpMessageHandlerBuilder<T>>();

            PrimaryHandlerFactory = (_, _) => new HttpClientHandler();
            AdditionalHandlers = new List<Func<T, IServiceProvider, DelegatingHandler>>();
        }

        public Mock<IHttpMessageHandlerBuilder<T>> MockBuilder { get; }

        public string Name { get; }
        public Func<T, IServiceProvider, HttpMessageHandler> PrimaryHandlerFactory
        {
            get => MockBuilder.Object.PrimaryHandlerFactory;
            set => MockBuilder.Object.PrimaryHandlerFactory = value;
        }
        /// <summary>
        /// Leave the list as a simple list, it'll be easier to check
        /// </summary>
        public IList<Func<T, IServiceProvider, DelegatingHandler>> AdditionalHandlers { get; }
        public HttpMessageHandler Build(T argument, IServiceProvider serviceProvider)
        {
            return MockBuilder.Object.Build(argument, serviceProvider);
        }
    }
}
