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
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using TSMoreland.Extensions.Http.Abstractions;

namespace TSMoreland.Extensions.Http.Polly
{
    public static class PollyHttpMessageBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with the provided
        /// <see cref="IAsyncPolicy{HttpResponseMessage}"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="policy">The <see cref="IAsyncPolicy{HttpResponseMessage}"/>.</param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T}"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddPolicyHandler<T>(this IHttpMessageHandlerBuilder<T> builder, IAsyncPolicy<HttpResponseMessage> policy)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            return builder.AddHttpMessageHandler((_, _) => new PolicyHttpMessageHandler(policy));
        }

        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
        /// by the <paramref name="policySelector"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="policySelector">
        /// Selects an <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to the current request.
        /// </param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T}"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddPolicyHandler<T>(
            this IHttpMessageHandlerBuilder<T> builder, 
            Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policySelector == null)
            {
                throw new ArgumentNullException(nameof(policySelector));
            }

            return builder.AddHttpMessageHandler((_, _) => new PolicyHttpMessageHandler(policySelector));
        }

        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
        /// by the <paramref name="policySelector"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="policySelector">
        /// Selects an <see cref="IAsyncPolicy{HttpResponseMessage}"/> to apply to the current request.
        /// </param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T}"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddPolicyHandler<T>(
            this IHttpMessageHandlerBuilder<T> builder,
            Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policySelector == null)
            {
                throw new ArgumentNullException(nameof(policySelector));
            }

            return builder.AddHttpMessageHandler((_, services) =>
            {
                return new PolicyHttpMessageHandler((request) => policySelector(services, request));
            });
        }

        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
        /// by the <see cref="IReadOnlyPolicyRegistry{String}"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="policyKey">
        /// The key used to resolve a policy from the <see cref="IReadOnlyPolicyRegistry{String}"/>.
        /// </param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T}"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddPolicyHandlerFromRegistry<T>(this IHttpMessageHandlerBuilder<T> builder, string policyKey)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policyKey == null)
            {
                throw new ArgumentNullException(nameof(policyKey));
            }

            return builder.AddHttpMessageHandler((_, services) =>
            {
                var registry = services.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

                var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>(policyKey);

                return new PolicyHttpMessageHandler(policy);
            });
        }


        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a policy returned
        /// by the <see cref="IReadOnlyPolicyRegistry{TKey}"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="policySelector">
        /// Selects an <see cref="IAsyncPolicy"/> to apply to the current request.
        /// </param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T}"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddPolicyHandlerFromRegistry<T>(
            this IHttpMessageHandlerBuilder<T> builder,
            Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policySelector == null)
            {
                throw new ArgumentNullException(nameof(policySelector));
            }

            return builder.AddHttpMessageHandler((_, services) =>
            {
                var registry = services.GetRequiredService<IReadOnlyPolicyRegistry<string>>();
                return new PolicyHttpMessageHandler((request) => policySelector(registry, request));
            });
        }

        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a <see cref="Policy"/>
        /// created by executing the provided configuration delegate. The policy builder will be preconfigured to trigger
        /// application of the policy for requests that fail with conditions that indicate a transient failure.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpMessageHandlerBuilder{T}"/>.</param>
        /// <param name="configurePolicy">A delegate used to create a <see cref="IAsyncPolicy{HttpResponseMessage}"/>.</param>
        /// <returns>An <see cref="IHttpMessageHandlerBuilder{T}"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// <para>
        /// The <see cref="PolicyBuilder{HttpResponseMessage}"/> provided to <paramref name="configurePolicy"/> has been
        /// preconfigured errors to handle errors in the following categories:
        /// <list type="bullet">
        /// <item><description>Network failures (as <see cref="HttpRequestException"/>)</description></item>
        /// <item><description>HTTP 5XX status codes (server errors)</description></item>
        /// <item><description>HTTP 408 status code (request timeout)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The policy created by <paramref name="configurePolicy"/> will be cached indefinitely per named client. Policies
        /// are generally designed to act as singletons, and can be shared when appropriate. To share a policy across multiple
        /// named clients, first create the policy and then pass it to multiple calls to 
        /// <see cref="AddPolicyHandler{T}(IHttpMessageHandlerBuilder{T}, IAsyncPolicy{HttpResponseMessage})"/> as desired.
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder<T> AddTransientHttpErrorPolicy<T>(
            this IHttpMessageHandlerBuilder<T> builder, 
            Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configurePolicy == null)
            {
                throw new ArgumentNullException(nameof(configurePolicy));
            }
            
            var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError();

            // Important - cache policy instances so that they are singletons per handler.
            var policy = configurePolicy(policyBuilder);

            return builder.AddHttpMessageHandler((_, _) => new PolicyHttpMessageHandler(policy));
        }
    }
}
