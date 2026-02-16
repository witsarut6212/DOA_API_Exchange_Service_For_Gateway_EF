using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Models
{
    public class ApiResponse<T>
    {
        [JsonProperty("info")]
        public ApiInfo Info { get; set; } = new ApiInfo();
        
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public T? Data { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public ApiError? Error { get; set; }

        [JsonProperty("validations", NullValueHandling = NullValueHandling.Ignore)]
        public List<ApiValidation>? Validations { get; set; }
    }

    public class ApiInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonProperty("systemCode", NullValueHandling = NullValueHandling.Ignore)]
        public int? SystemCode { get; set; }

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public int? Status { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
    }

    public class ApiError
    {
        [JsonProperty("traceId")]
        public string TraceId { get; set; } = string.Empty;

        [JsonProperty("instance")]
        public string Instance { get; set; } = string.Empty;
    }

    public class ApiValidation
    {
        [JsonProperty("field")]
        public string Field { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
    }
}
