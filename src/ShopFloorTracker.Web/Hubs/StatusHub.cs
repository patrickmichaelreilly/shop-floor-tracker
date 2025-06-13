using Microsoft.AspNetCore.SignalR;

namespace ShopFloorTracker.Web.Hubs;

public class StatusHub : Hub
{
    public async Task Heartbeat(DateTime serverUtc)
    {
        await Clients.All.SendAsync("Heartbeat", serverUtc);
    }

    public async Task PartStatusChanged(string partId, string newStatus)
    {
        await Clients.All.SendAsync("PartStatusChanged", partId, newStatus);
    }
}