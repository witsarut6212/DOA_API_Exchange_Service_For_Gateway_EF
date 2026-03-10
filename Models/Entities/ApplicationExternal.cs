using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DOA_API_Exchange_Service_For_Gateway.Data;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class ApplicationExternal
{
    public int Id { get; set; }

    public int AppRoleId { get; set; }

    public string AppName { get; set; } = null!;

    public string AppNickName { get; set; } = null!;

    public string HostUrl { get; set; } = null!;

    public string? CallbackUrl { get; set; }

    public string CliendId { get; set; } = null!; // System generate UUID v4

    public string? SecretKey { get; set; }

    public string IsActive { get; set; } = "Y";

    public string IsVerified { get; set; } = "N";

    public DateTime? VerfiedAt { get; set; }

    public DateTime SystemTime { get; set; } // Not Null ตามรูป

    public DateTime? CreatedAt { get; set; } // ในรูป allow null (created_at)

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    // --- Cache Management ---
    private const string AppCachePrefix = "App_";

    public static async Task<ApplicationExternal?> GetCachedAsync(AppDbContext dbContext, IMemoryCache cache, string clientId)
    {
        var cacheKey = $"{AppCachePrefix}{clientId}";

        if (!cache.TryGetValue(cacheKey, out ApplicationExternal? application))
        {
            // Cache Miss: Query from Database
            application = await dbContext.ApplicationExternals
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.CliendId == clientId);

            if (application != null)
            {
                // Cache Set (expire in 15 mins, absolute 30 mins)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                cache.Set(cacheKey, application, cacheOptions);
            }
        }

        return application;
    }
}
