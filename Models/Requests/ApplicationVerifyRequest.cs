using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests;

public class ApplicationVerifyRequest
{
    [Required]
    public string CliendId { get; set; } = null!; // ใช้สะกดตาม DB (มี d)
}
