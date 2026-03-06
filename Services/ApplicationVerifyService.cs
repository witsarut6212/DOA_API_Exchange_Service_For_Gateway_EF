using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using Microsoft.Extensions.Logging;

namespace DOA_API_Exchange_Service_For_Gateway.Services;

public interface IApplicationVerifyService
{
    Task<(int StatusCode, string Message, object? Data)> VerifyApplicationAsync(ApplicationVerifyRequest request);
}

public class ApplicationVerifyService : IApplicationVerifyService
{
    private readonly AppDbContext _context;
    private readonly IVerifyStatusService _statusService;
    private readonly ILogger<ApplicationVerifyService> _logger;

    public ApplicationVerifyService(AppDbContext context, IVerifyStatusService statusService, ILogger<ApplicationVerifyService> logger)
    {
        _context = context;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<(int StatusCode, string Message, object? Data)> VerifyApplicationAsync(ApplicationVerifyRequest request)
    {
        // 1. Check in database
        var app = await _context.ApplicationExternals
            .FirstOrDefaultAsync(a => a.CliendId == request.CliendId);

        if (app == null)
        {
            return (404, "ไม่พบข้อมูลลงทะเบียนแอพพลิเคชั่น", null);
        }

        // 2. Delegate Status Check
        var statusResult = _statusService.ValidateStatus(app);
        if (statusResult.StatusCode != 200)
        {
            return (statusResult.StatusCode, statusResult.Message, null);
        }

        // 3. Update (Using Execution Strategy)
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                app.IsVerified = "Y";
                app.VerfiedAt = DateTime.Now;
                app.UpdatedAt = DateTime.Now;
                app.UpdatedBy = "SYSTEM_VERIFY";

                _context.ApplicationExternals.Update(app);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();

                /*
                var responseData = new
                {
                    app.AppName,
                    app.AppNickName,
                    app.CliendId,
                    app.IsVerified,
                    app.VerfiedAt
                };
                */

                return (200, "ระบบยืนยันการตรวจสอบแล้ว", null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "VerifyApplicationAsync: Failed to verify application for CliendId {CliendId}", request.CliendId);
                var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return (500, $"Internal Error: {innerError}", (object)null!);
            }
        });
    }
}
