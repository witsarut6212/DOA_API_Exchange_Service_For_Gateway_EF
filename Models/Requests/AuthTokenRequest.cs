using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class AuthTokenRequest
    {
        [Required]
        [JsonProperty("credential_value")]
        public string CredentialValue { get; set; } = string.Empty;
    }
}

