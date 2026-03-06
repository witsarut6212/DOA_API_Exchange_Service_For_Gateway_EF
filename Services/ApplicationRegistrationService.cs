using System.Threading.Tasks;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services;

public interface IApplicationRegistrationService
{
    Task<(bool Success, string Message, object? Data)> RegisterApplicationAsync(ApplicationRegisterRequest request);
}

public class ApplicationRegistrationService : IApplicationRegistrationService
{
    private readonly DOA_API_Exchange_Service_For_Gateway.Data.AppDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<ApplicationRegistrationService> _logger;

    public ApplicationRegistrationService(DOA_API_Exchange_Service_For_Gateway.Data.AppDbContext context, Microsoft.Extensions.Logging.ILogger<ApplicationRegistrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, object? Data)> RegisterApplicationAsync(ApplicationRegisterRequest request)
    {
        // 1. Check for Duplicate AppName or AppNickName
        var existingAppName = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_context.ApplicationExternals, a => a.AppName == request.AppName);
        var existingAppNickName = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_context.ApplicationExternals, a => a.AppNickName == request.AppNickName);

        if (existingAppName || existingAppNickName)
        {
            var duplicates = new System.Collections.Generic.List<string>();
            if (existingAppName) duplicates.Add("AppName");
            if (existingAppNickName) duplicates.Add("AppNickName");

            return (false, $"{string.Join(" and ", duplicates)} is already registered.", null);
        }

        // 2. Create the new Application External Record
        var newApplication = new DOA_API_Exchange_Service_For_Gateway.Models.Entities.ApplicationExternal
        {
            AppRoleId = 0,
            CliendId = Guid.NewGuid().ToString(),
            CallbackUrl = request.CallbackUrl,
            HostUrl = request.HostUrl,
            AppName = request.AppName,
            AppNickName = request.AppNickName,
            CreatedAt = DateTime.Now,
            SystemTime = DateTime.Now,
            IsActive = "Y",
            IsVerified = "N"
        };

        // 3. Save to Database
        try
        {
            await _context.ApplicationExternals.AddAsync(newApplication);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterApplicationAsync: Failed to save application {AppName}", request.AppName);
            var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return (false, $"Internal Error: {innerError}", null);
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
