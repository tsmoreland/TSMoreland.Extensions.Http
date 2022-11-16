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

using NUnit.Framework;

namespace TSMoreland.Extensions.Http.Abstractions.Tests;

[TestFixture]
public sealed class HttpMessageHandlerBuilderExceptionTests
{
    [Test]
    public void Message_ReturnsEmpty_WhenDefaultConstructed()
    {
        var ex = new HttpMessageHandlerBuilderException();
        Assert.That(ex.Message, Is.Not.Empty);
    }

    /// <summary>
    /// overkill test but meh, going for coverage
    /// </summary>
    [Test]
    public void Message_ReturnsExpectedValue_WhenConstructedWithMessage()
    {
        const string message = "sample message";

        var ex = new HttpMessageHandlerBuilderException(message);

        Assert.That(ex.Message, Is.EqualTo(message));
    }

    /// <summary>
    /// overkill test but meh, going for coverage
    /// </summary>
    [Test]
    public void Message_ReturnsExpectedValue_WhenConstructedWithMessageAndInnerException()
    {
        const string message = "sample message";
        var inner = new Exception("Inner Exception");

        var ex = new HttpMessageHandlerBuilderException(message, inner);

        Assert.That(ex.Message, Is.EqualTo(message));
    }

    [Test]
    public void InnerException_ReturnsExpectedValue_WhenConstructedWithMessageAndInnerException()
    {
        const string message = "sample message";
        var inner = new Exception("Inner Exception");

        var ex = new HttpMessageHandlerBuilderException(message, inner);

        Assert.That(ex.InnerException, Is.EqualTo(inner));
    }
}
