using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests;

public class ApplicationRegisterRequest
{
    public string? CallbackUrl { get; set; }

    [Required]
    public string HostUrl { get; set; } = null!;

    [Required]
    public string AppName { get; set; } = null!;

    [Required]
    public string AppNickName { get; set; } = null!;
}

}
