using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests;

public class ApplicationRegisterRequest
{
    [Required]
    [MaxLength(400)]
    public string AppName { get; set; } = null!;

    [Required]
    [MaxLength(400)]
    public string AppNickName { get; set; } = null!;

    [Required]
    [MaxLength(400)]
    [Url]
    public string HostUrl { get; set; } = null!;

    [MaxLength(400)]
    [Url]
    public string? CallbackUrl { get; set; }
}