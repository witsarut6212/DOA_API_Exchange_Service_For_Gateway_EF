namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    // BackgroundService = base class ของ .NET สำหรับงานที่รันตลอดเวลาใน background
    // จดทะเบียนด้วย AddHostedService → เริ่มทำงานตอน app start และหยุดตอน app ปิด
    //
    // ทำไมต้องใช้ BackgroundService แทน fire-and-forget?
    // - BackgroundService เป็น Singleton → อยู่ตลอดชีวิต app
    // - ใช้ IServiceScopeFactory สร้าง Scope ใหม่ทุกครั้งที่ทำงาน
    //   → ได้ DbContext ใหม่ที่ไม่ถูก dispose ไปพร้อมกับ HTTP request
    public class ProgressBackgroundService : BackgroundService
    {
        private readonly IProgressQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory; // ใช้สร้าง Scope ใหม่
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

        // ExecuteAsync() = method หลักที่ .NET จะเรียกตอน app start
        // วนลูปตลอดเวลา รอรับงานจาก queue แล้วประมวลผล
        // stoppingToken = signal จาก .NET ว่า app กำลังจะปิด → หยุดลูป
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProgressBackgroundService started.");

            // วนลูปตลอด จนกว่า app จะปิด (stoppingToken ถูก cancel)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // รอรับงานจาก queue
                    // ถ้า queue ว่าง → await อยู่ตรงนี้เฉยๆ ไม่กิน CPU
                    // พอมี request เข้ามา → ทำงานต่อ
                    var request = await _queue.DequeueAsync(stoppingToken);

                    _logger.LogInformation("Background processing Ref: {Ref}", request.DocumentControl.ReferenceNumber);

                    // สร้าง Scope ใหม่สำหรับงานนี้โดยเฉพาะ
                    // ทำไมต้องสร้าง Scope?
                    // - BackgroundService เป็น Singleton
                    // - แต่ SubmissionService และ DbContext เป็น Scoped
                    // - Singleton ไม่สามารถ inject Scoped ได้โดยตรง
                    // - ต้องสร้าง Scope เองเพื่อขอ Scoped service มาใช้
                    using var scope = _scopeFactory.CreateScope();

                    // ขอ SubmissionService จาก Scope ใหม่
                    // → ได้ DbContext ใหม่ที่สะอาด ไม่ถูก dispose ไปพร้อม HTTP request
                    var submissionService = scope.ServiceProvider
                        .GetRequiredService<ISubmissionService>();

                    // ทำ Process Payload ด้วย Scope ใหม่
                    await submissionService.ProcessPayloadAsync(request);

                    // เมื่อออกจาก using → scope.Dispose() ถูกเรียกอัตโนมัติ
                    // → DbContext ถูกปิดสะอาด พร้อมรับงานชิ้นต่อไป
                }
                catch (OperationCanceledException)
                {
                    // เกิดขึ้นตอน app กำลังปิด stoppingToken ถูก cancel
                    // ไม่ใช่ error จริง → break ออกจาก loop เพื่อปิด service
                    break;
                }
                catch (Exception ex)
                {
                    // error จาก ProcessPayload → log แล้ววนต่อ ไม่หยุด service
                    _logger.LogError(ex, "Background processing failed.");
                }
            }

            _logger.LogInformation("ProgressBackgroundService stopped.");
        }
    }
}
