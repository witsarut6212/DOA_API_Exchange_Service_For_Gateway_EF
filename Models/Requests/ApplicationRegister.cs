using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests;

public class ApplicationRegisterRequest
{
    [Required]
    [MaxLength(400)]
    [JsonProperty("appName")]
    public string AppName { get; set; } = null!;

    [Required]
    [MaxLength(400)]
    [JsonProperty("appNickName")]
    public string AppNickName { get; set; } = null!;

    [Required]
    [MaxLength(400)]
    [Url]
    [JsonProperty("hostUrl")]
    public string HostUrl { get; set; } = null!;

    [MaxLength(400)]
    [Url]
    [JsonProperty("callbackUrl")]
    public string? CallbackUrl { get; set; }
}