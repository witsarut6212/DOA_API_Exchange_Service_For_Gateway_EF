using System;
using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Models
{
    public class ApiResponse<T>
    {
        public ApiInfo Info { get; set; } = new ApiInfo();
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public T? Data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ApiError? Error { get; set; }
    }

    public class ApiInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public int SystemCode { get; set; }
        public string Timestamp { get; set; } = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
    }

    public class ApiError
    {
        public string TraceId { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
    }
}
