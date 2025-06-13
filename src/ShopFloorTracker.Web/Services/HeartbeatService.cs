using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShopFloorTracker.Web.Services;

public class HeartbeatService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(IServiceProvider serviceProvider, ILogger<HeartbeatService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statusBroadcaster = scope.ServiceProvider.GetRequiredService<IStatusBroadcaster>();
                
                await statusBroadcaster.SendHeartbeatAsync();
                _logger.LogDebug("Heartbeat sent at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}