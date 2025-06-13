using Microsoft.AspNetCore.SignalR;
using Moq;
using ShopFloorTracker.Core.Entities;
using ShopFloorTracker.Core.Enums;
using ShopFloorTracker.Web.Services;
using ShopFloorTracker.Web.Hubs;
using Xunit;

namespace ShopFloorTracker.Tests.Unit;

public class StatusBroadcasterTests
{
    private readonly Mock<IHubContext<StatusHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly StatusBroadcaster _statusBroadcaster;

    public StatusBroadcasterTests()
    {
        _mockHubContext = new Mock<IHubContext<StatusHub>>();
        _mockClientProxy = new Mock<IClientProxy>();
        
        _mockHubContext.Setup(h => h.Clients.All).Returns(_mockClientProxy.Object);
        
        _statusBroadcaster = new StatusBroadcaster(_mockHubContext.Object);
    }

    [Fact]
    public async Task SendHeartbeatAsync_ShouldSendHeartbeatMessage()
    {
        // Act
        await _statusBroadcaster.SendHeartbeatAsync();

        // Assert
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("Heartbeat", 
                It.Is<object[]>(args => args.Length == 1 && args[0] is DateTime), 
                default), 
            Times.Once);
    }

    [Fact]
    public async Task BroadcastPartStatusAsync_ShouldSendPartStatusChangedMessage()
    {
        // Arrange
        var part = new Part
        {
            PartId = "TEST-001",
            Status = PartStatus.Sorted
        };

        // Act
        await _statusBroadcaster.BroadcastPartStatusAsync(part);

        // Assert
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("PartStatusChanged", 
                It.Is<object[]>(args => 
                    args.Length == 2 && 
                    args[0].Equals("TEST-001") && 
                    args[1].Equals("Sorted")), 
                default), 
            Times.Once);
    }
}