using Microsoft.Extensions.Hosting;

namespace PubSub.services
{
    public class CupidBackgroundService : BackgroundService
    {
        private readonly ICupidService _cupidService;
        private readonly ILogger<CupidBackgroundService> _logger;

        public CupidBackgroundService(ICupidService cupidService, ILogger<CupidBackgroundService> logger)
        {
            _cupidService = cupidService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kupidon je spreman. Pisma stižu svakih 60 sekundi...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                try
                {
                    _cupidService.SendLetters();
                    _logger.LogInformation("[{Time}] Kupidon je poslao pisma.", DateTime.Now.ToString("HH:mm:ss"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Greška pri slanju pisama.");
                }
            }
        }
    }
}
