using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services;

public class ApplicationService : IApplicationService
{
    private readonly AppDbContext _context;

    public ApplicationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, object? Data)> RegisterApplicationAsync(ApplicationRegisterRequest request)
    {
        // 1. Check for Duplicate AppName or AppNickName
        var existingApp = await _context.ApplicationExternals
            .FirstOrDefaultAsync(a => a.AppName == request.AppName || a.AppNickName == request.AppNickName);

        if (existingApp != null)
        {
            var duplicateWarning = existingApp.AppName == request.AppName ? "AppName" : "AppNickName";
            return (false, $"{duplicateWarning} is already registered.", null);
        }

        // 2. Create the new Application External Record
        var newApplication = new ApplicationExternal
        {
            AppRoleId = 0,
            CliendId = Guid.NewGuid().ToString(), // UUID v4
            CallbackUrl = request.CallbackUrl,
            HostUrl = request.HostUrl,
            AppName = request.AppName,
            AppNickName = request.AppNickName,
            CreatedAt = DateTime.Now,
            IsActive = 1,      // Default to active
            IsVerified = 0      // Default to not verified
        };

        // 3. Save to Database
        try
        {
            await _context.ApplicationExternals.AddAsync(newApplication);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return (false, $"Internal Error: {ex.Message}", null);
        }

        // 4. Prepare Response Data
        var responseData = new 
        {
            AppName = newApplication.AppName,
            AppNickName = newApplication.AppNickName,
            ClientId = newApplication.CliendId
        };

        return (true, "Application registered successfully.", responseData);
    }
}
