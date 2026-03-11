using DOA_API_Exchange_Service_For_Gateway.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DOA_API_Exchange_Service_For_Gateway.Helpers
{
    public interface IResponseHelper
    {
        ApiResponse<T> CreateSuccess<T>(T data, string detail = "The application process successful.", int systemCode = 200);
        ApiResponse<object> CreateError(string detail, int systemCode, object? data = null, List<ApiValidation>? validations = null);
    }

    public class ResponseHelper : IResponseHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _defaultTitle;

        public ResponseHelper(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _defaultTitle = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
        }

        public ApiResponse<T> CreateSuccess<T>(T data, string detail = "The application process successful.", int systemCode = 200)
        {
            return new ApiResponse<T>
            {
                Info = new ApiInfo
                {
                    Title = _defaultTitle,
                    Detail = detail,
                    SystemCode = systemCode
                },
                Data = data
            };
        }

        public ApiResponse<object> CreateError(string detail, int systemCode, object? data = null, List<ApiValidation>? validations = null)
        {
            var context = _httpContextAccessor.HttpContext;
            
            return new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = _defaultTitle,
                    Detail = detail,
                    SystemCode = systemCode
                },
                Data = data,
                Validations = validations,
                Error = new ApiError
                {
                    TraceId = context?.TraceIdentifier ?? string.Empty,
                    Instance = context?.Request.Path ?? string.Empty
                }
            };
        }
    }
}
