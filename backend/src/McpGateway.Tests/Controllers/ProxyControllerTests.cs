using FluentAssertions;
using McpGateway.Application.Bridging;
using McpGateway.Application.Interfaces;
using McpGateway.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace McpGateway.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="ProxyController"/>. Bridge routes (<c>/sse</c>, <c>/messages</c>,
/// <c>/mcp</c>) must delegate to <see cref="IMcpBridgeService"/>; only the legacy
/// <c>/message</c> path keeps going through <see cref="IProxyService"/>.
/// </summary>
public class ProxyControllerTests
{
    private readonly Mock<IProxyService> _mockProxyService;
    private readonly Mock<IMcpBridgeService> _mockBridge;
    private readonly Mock<ILogger<ProxyController>> _mockLogger;
    private readonly ProxyController _controller;

    public ProxyControllerTests()
    {
        _mockProxyService = new Mock<IProxyService>();
        _mockBridge = new Mock<IMcpBridgeService>();
        _mockLogger = new Mock<ILogger<ProxyController>>();
        _controller = new ProxyController(_mockProxyService.Object, _mockBridge.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task OpenSseStream_DelegatesToBridge()
    {
        var adapterName = "test-adapter";
        _mockBridge.Setup(s => s.HandleSseStreamAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _controller.OpenSseStream(adapterName, CancellationToken.None);

        _mockBridge.Verify(s => s.HandleSseStreamAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostSseMessages_DelegatesToBridge()
    {
        var adapterName = "test-adapter";
        _mockBridge.Setup(s => s.HandleSseMessagesAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _controller.PostSseMessages(adapterName, CancellationToken.None);

        _mockBridge.Verify(s => s.HandleSseMessagesAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostStreamable_DelegatesToBridge()
    {
        var adapterName = "test-adapter";
        var ct = CancellationToken.None;
        _mockBridge.Setup(s => s.HandleStreamablePostAsync(adapterName, It.IsAny<HttpContext>(), ct))
            .Returns(Task.CompletedTask);

        await _controller.PostStreamable(adapterName, ct);

        _mockBridge.Verify(s => s.HandleStreamablePostAsync(adapterName, It.IsAny<HttpContext>(), ct), Times.Once);
    }

    [Fact]
    public async Task OpenStreamableServerStream_DelegatesToBridge()
    {
        var adapterName = "test-adapter";
        var ct = CancellationToken.None;
        _mockBridge.Setup(s => s.HandleStreamableGetAsync(adapterName, It.IsAny<HttpContext>(), ct))
            .Returns(Task.CompletedTask);

        await _controller.OpenStreamableServerStream(adapterName, ct);

        _mockBridge.Verify(s => s.HandleStreamableGetAsync(adapterName, It.IsAny<HttpContext>(), ct), Times.Once);
    }

    [Fact]
    public async Task SendMessage_DelegatesToProxyServiceWithRetry()
    {
        var adapterName = "test-adapter";

        var request = new Mock<HttpRequest>();
        var httpContext = new Mock<HttpContext>();
        request.Setup(r => r.QueryString).Returns(QueryString.Empty);
        httpContext.Setup(c => c.Request).Returns(request.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        _mockProxyService.Setup(s => s.ForwardRequestAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<string>(), true))
            .Returns(Task.CompletedTask);

        await _controller.SendMessage(adapterName);

        _mockProxyService.Verify(s => s.ForwardRequestAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<string>(), true), Times.Once);
    }

    [Theory]
    [InlineData("", "message")]
    [InlineData("?param=value", "message?param=value")]
    [InlineData("?param1=value1&param2=value2", "message?param1=value1&param2=value2")]
    public async Task SendMessage_AppendsQueryString(string queryString, string expectedEndpoint)
    {
        var adapterName = "test-adapter";

        var request = new Mock<HttpRequest>();
        var httpContext = new Mock<HttpContext>();
        request.Setup(r => r.QueryString).Returns(new QueryString(queryString));
        httpContext.Setup(c => c.Request).Returns(request.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var captured = string.Empty;
        _mockProxyService.Setup(s => s.ForwardRequestAsync(adapterName, It.IsAny<HttpContext>(), It.IsAny<string>(), true))
            .Callback<string, HttpContext, string, bool>((_, _, endpoint, _) => captured = endpoint)
            .Returns(Task.CompletedTask);

        await _controller.SendMessage(adapterName);

        captured.Should().Be(expectedEndpoint);
    }
}
