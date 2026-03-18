using DOA_API_Exchange_Service_For_Gateway.Data;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ICommonService
    {
        Task<string?> GetEffectiveRegistrationIdAsync(string? requestRegId);
    }

    public class CommonService : ICommonService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CommonService> _logger;

        public CommonService(AppDbContext context, IWebHostEnvironment env, ILogger<CommonService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public async Task<string?> GetEffectiveRegistrationIdAsync(string? requestRegId)
        {
            // Priority 1: Request field (Provided by caller)
            if (!string.IsNullOrEmpty(requestRegId)) 
            {
                _logger.LogInformation("RegistrationID: Using value from REQUEST: {RegId}", requestRegId);
                return requestRegId;
            }

            // Priority 2: Database authors table
            var author = await _context.Authors.FirstOrDefaultAsync();
            if (author != null)
            {
                bool isDev = _env.EnvironmentName.Equals("Development", StringComparison.OrdinalIgnoreCase) 
                             || _env.EnvironmentName.Equals("Dev", StringComparison.OrdinalIgnoreCase);

                string? nswRid = isDev ? author.NswridDev : author.NswridPro;

                if (!string.IsNullOrEmpty(nswRid)) 
                {
                    _logger.LogInformation("RegistrationID: Using value from DB Authors ({Env}): {RegId}", isDev ? "DEV" : "PRO", nswRid);
                    return nswRid;
                }
                
                _logger.LogWarning("RegistrationID: Author record found but NSW RID for {Env} is Empty.", isDev ? "DEV" : "PRO");
            }
            else
            {
                _logger.LogWarning("RegistrationID: No record found in AUTHORS table.");
            }

            return null;
        }
    }
}
