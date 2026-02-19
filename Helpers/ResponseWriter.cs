using DOA_API_Exchange_Service_For_Gateway.Models;

namespace DOA_API_Exchange_Service_For_Gateway.Helpers
{
    public static class ResponseWriter
    {
        public static ApiResponse<T> CreateSuccess<T>(string title, T data, string detail = "The application process successful.", int systemCode = 200)
        {
            return new ApiResponse<T>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = detail,
                    SystemCode = systemCode
                },
                Data = data
            };
        }

        public static ApiResponse<object> CreateError(string title, string detail, int systemCode, string? traceId = null, string? instance = null, object? data = null, List<ApiValidation>? validations = null)
        {
            return new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = detail,
                    SystemCode = systemCode
                },
                Data = data,
                Validations = validations,
                Error = (traceId == null && instance == null) ? null : new ApiError
                {
                    TraceId = traceId ?? string.Empty,
                    Instance = instance ?? string.Empty
                }
            };
        }
    }
}
