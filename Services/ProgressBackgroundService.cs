namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class ProgressBackgroundService : BackgroundService
    {
        private readonly IProgressQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProgressBackgroundService> _logger;

        public ProgressBackgroundService(
            IProgressQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ProgressBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProgressBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (payloadId, request) = await _queue.DequeueAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var submissionService = scope.ServiceProvider.GetRequiredService<ISubmissionService>();

                    await submissionService.ProcessPayloadAsync(payloadId, request);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background processing failed.");
                }
            }

            _logger.LogInformation("ProgressBackgroundService stopped.");
        }
    }
}
