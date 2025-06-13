using Microsoft.AspNetCore.SignalR;
using ShopFloorTracker.Web.Hubs;
using ShopFloorTracker.Core.Entities;

namespace ShopFloorTracker.Web.Services;

public interface IStatusBroadcaster
{
    Task SendHeartbeatAsync();
    Task BroadcastPartStatusAsync(Part part);
}

public class StatusBroadcaster : IStatusBroadcaster
{
    private readonly IHubContext<StatusHub> _hubContext;

    public StatusBroadcaster(IHubContext<StatusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendHeartbeatAsync()
    {
        await _hubContext.Clients.All.SendAsync("Heartbeat", DateTime.UtcNow);
    }

    public async Task BroadcastPartStatusAsync(Part part)
    {
        await _hubContext.Clients.All.SendAsync("PartStatusChanged", part.PartId, part.Status.ToString());
    }
}