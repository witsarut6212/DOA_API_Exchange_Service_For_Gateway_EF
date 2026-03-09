using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class AuthTokenRequest
    {
        [Required(ErrorMessage = "The credential_type field is required.")]
        [JsonProperty("credential_type")] // สำหรับ Newtonsoft.Json
        [JsonPropertyName("credential_type")] // สำหรับ System.Text.Json
        public string CredentialType { get; set; } = string.Empty;
    }
}
