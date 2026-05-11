using McpGateway.Application.Bridging;

namespace McpGateway.Hosting;

/// <summary>
/// Background service that periodically asks <see cref="McpBridgeSessionStore"/> to
/// drop bridge sessions whose last activity exceeds <see cref="McpBridgeSessionStore.IdleTimeout"/>.
/// Without this, idle sessions would hold an open upstream connection forever.
/// </summary>
public sealed class BridgeSessionSweeperService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

    private readonly McpBridgeSessionStore _store;
    private readonly ILogger<BridgeSessionSweeperService> _logger;

    public BridgeSessionSweeperService(McpBridgeSessionStore store, ILogger<BridgeSessionSweeperService> logger)
    {
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await _store.SweepIdleAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bridge session sweep failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }
}
