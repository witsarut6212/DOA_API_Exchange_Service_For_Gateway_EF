using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests;

public class ApplicationVerifyRequest
{
    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string AppName { get; set; } = null!;
}
