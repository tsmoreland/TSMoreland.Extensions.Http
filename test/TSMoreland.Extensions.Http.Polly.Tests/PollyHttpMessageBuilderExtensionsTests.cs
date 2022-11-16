//
// Copyright (c) 2022 Terry Moreland
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

using Moq;
using NUnit.Framework;
using Polly;
using Polly.Registry;
using TSMoreland.Extensions.Http.Abstractions;

namespace TSMoreland.Extensions.Http.Polly.Tests;

[TestFixture]
public sealed class PollyHttpMessageBuilderExtensionsTests
{
    private Mock<IHttpMessageHandlerBuilder<object>> _builder = null!;
    private Mock<IAsyncPolicy<HttpResponseMessage>> _policy = null!;
    private Mock<Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>>> _policySelector = null!;
    private Mock<Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>>> _policySelectorWithServices = null!;
    private Mock<Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>>> _registryPolicySelector = null!;
    private Mock<Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>>> _configurePolicy = null!;

    [SetUp]
    public void SetUp()
    {
        _builder = new Mock<IHttpMessageHandlerBuilder<object>>();
        _policy = new Mock<IAsyncPolicy<HttpResponseMessage>>();
        _policySelector = new Mock<Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>>>();
        _policySelectorWithServices =
            new Mock<Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>>>();
        _registryPolicySelector =
            new Mock<Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage,
                IAsyncPolicy<HttpResponseMessage>>>();
        _configurePolicy = new Mock<Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>>>();
    }

    [Test]
    public void AddPolicyHandler_FromPolicy_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        IHttpMessageHandlerBuilder<object> builder = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = builder.AddPolicyHandler(_policy.Object));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(builder)));
    }
    [Test]
    public void AddPolicyHandler_FromPolicy_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        IAsyncPolicy<HttpResponseMessage> policy = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = _builder.Object.AddPolicyHandler(policy));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(policy)));
    }

    [Test]
    public void AddPolicyHandler_FromPolicySelector_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        IHttpMessageHandlerBuilder<object> builder = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = builder.AddPolicyHandler(_policySelector.Object));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(builder)));
    }

    [Test]
    public void AddPolicyHandler_FromPolicySelector_ThrowsArgumentNullException_WhenPolicySelectorIsNull()
    {
        Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = _builder.Object.AddPolicyHandler(policySelector));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(policySelector)));
    }

    [Test]
    public void AddPolicyHandler_FromPolicyAndRequest_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        IHttpMessageHandlerBuilder<object> builder = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = builder.AddPolicyHandler(_policySelectorWithServices.Object));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(builder)));
    }

    [Test]
    public void AddPolicyHandler_FromPolicyAndRequest_ThrowsArgumentNullException_WhenPolicySelectorIsNull()
    {
        Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = _builder.Object.AddPolicyHandler(policySelector));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(policySelector)));
    }

    [Test]
    public void AddPolicyHandlerFromRegistry_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        IHttpMessageHandlerBuilder<object> builder = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = builder.AddPolicyHandlerFromRegistry("key"));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(builder)));
    }

    [Test]
    public void AddPolicyHandlerFromRegistry_ThrowsArgumentNullException_WhenPolicyKeyIsNull()
    {
        string policyKey = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = _builder.Object.AddPolicyHandlerFromRegistry(policyKey));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(policyKey)));
    }

    [Test]
    public void AddPolicyHandlerFromRegistry_FromPolicySelector_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        IHttpMessageHandlerBuilder<object> builder = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = builder.AddPolicyHandlerFromRegistry(_registryPolicySelector.Object));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(builder)));
    }

    [Test]
    public void AddPolicyHandlerFromRegistry_FromPolicySelector_ThrowsArgumentNullException_WhensPolicySelectorNull()
    {
        Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = _builder.Object.AddPolicyHandlerFromRegistry(policySelector));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(policySelector)));
    }

    [Test]
    public void AddTransientHttpErrorPolicy_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        IHttpMessageHandlerBuilder<object> builder = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = builder.AddTransientHttpErrorPolicy(_configurePolicy.Object));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(builder)));
    }

    [Test]
    public void AddTransientHttpErrorPolicy_ThrowsArgumentNullException_WhenConfigurePolicyIsNull()
    {
        Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy = null!;
        var ex = Assert.Throws<ArgumentNullException>(() => _ = _builder.Object.AddTransientHttpErrorPolicy(configurePolicy));
        Assert.That(ex!.ParamName, Is.EqualTo(nameof(configurePolicy)));
    }
}
