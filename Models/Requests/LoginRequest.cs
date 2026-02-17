using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
