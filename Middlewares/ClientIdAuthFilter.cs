using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Data;
using Microsoft.Extensions.Caching.Memory;

namespace DOA_API_Exchange_Service_For_Gateway.Middlewares
{
    public class ClientIdAuthFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly IResponseHelper _responseHelper;

        public ClientIdAuthFilter(AppDbContext dbContext, IMemoryCache cache, IResponseHelper responseHelper)
        {
            _dbContext = dbContext;
            _cache = cache;
            _responseHelper = responseHelper;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            // 1. ดึง client_id จาก Header
            if (!request.Headers.TryGetValue("client_id", out var clientIdValues) &&
                !request.Headers.TryGetValue("cliend_id", out clientIdValues))
            {
                context.Result = new UnauthorizedObjectResult(_responseHelper.CreateError(
                    "client_id header is required.", 
                    401));
                return;
            }

            var clientId = clientIdValues.ToString();

            // 2. ตรวจสอบข้อมูลแอปผ่าน Cache
            var application = await ApplicationExternal.GetCachedAsync(_dbContext, _cache, clientId);

            if (application == null)
            {
                context.Result = new UnauthorizedObjectResult(_responseHelper.CreateError(
                    "ไม่ได้ลงทะเบียน.", 
                    401));
                return;
            }

            // 3. ฝากข้อมูลไว้ใน HttpContext
            context.HttpContext.Items["Application"] = application;
            context.HttpContext.Items["client_id"] = clientId;

            await next();
        }
    }
}
