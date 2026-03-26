namespace G2CCRMPortal.Services;

/// Background service that checks SLA breaches every 4 hours.
/// Runs in the background while the application is running.
public class SlaBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(4);

    public SlaBackgroundService(IServiceProvider serviceProvider, ILogger<SlaBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();
                    await slaService.CheckAndEscalateSlaBreachesAsync();
                }

                // Wait 4 hours before next check
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SLA Background Service is stopping.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SLA Background Service: {ex.Message}");
                // Wait a bit before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("SLA Background Service stopped.");
    }
}