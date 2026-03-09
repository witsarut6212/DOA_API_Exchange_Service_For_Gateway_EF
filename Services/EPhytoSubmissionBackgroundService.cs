namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    /// <summary>
    /// Background Service สำหรับประมวลผล EPhyto Submission
    /// รับงานจาก EPhytoSubmissionQueue แล้วเรียก EPhytoService.ProcessEPhytoPayloadAsync
    /// รองรับ: asw/normal, ippc/normal, ippc/re-export, ippc/withdraw
    /// </summary>
    public class EPhytoSubmissionBackgroundService : BackgroundService
    {
        private readonly IEPhytoSubmissionQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EPhytoSubmissionBackgroundService> _logger;

        public EPhytoSubmissionBackgroundService(
            IEPhytoSubmissionQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EPhytoSubmissionBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EPhytoSubmissionBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (payloadId, request, source, systemOrigin) = await _queue.DequeueAsync(stoppingToken);

                    _logger.LogInformation("[{Source}] Dequeued PayloadId: {Id} — starting background process.", source, payloadId);

                    using var scope = _scopeFactory.CreateScope();
                    var ePhytoService = scope.ServiceProvider.GetRequiredService<IEPhytoService>();

                    await ePhytoService.ProcessEPhytoPayloadAsync(payloadId, request, source, systemOrigin);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EPhytoSubmissionBackgroundService: Unhandled error.");
                }
            }

            _logger.LogInformation("EPhytoSubmissionBackgroundService stopped.");
        }
    }
}
