namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class CertificateBackgroundService : BackgroundService
    {
        private readonly ICertificateQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CertificateBackgroundService> _logger;

        public CertificateBackgroundService(
            ICertificateQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<CertificateBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CertificateBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (payloadId, request, source) = await _queue.DequeueAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var submissionService = scope.ServiceProvider.GetRequiredService<ISubmissionService>();

                    await submissionService.ProcessCertificatePayloadAsync(payloadId, request, source);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CertificateBackgroundService: Error during processing.");
                }
            }

            _logger.LogInformation("CertificateBackgroundService stopped.");
        }
    }
}
