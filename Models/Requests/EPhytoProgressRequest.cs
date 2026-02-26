using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class EPhytoProgressRequest
    {
        [Required]
        public string MessageId { get; set; } = null!;

        [Required]
        public string Status { get; set; } = null!;

        public string? Remark { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? LicenseNumber { get; set; }

        public DateTime UpdateTime { get; set; } = DateTime.Now;
        
        public object? AdditionalData { get; set; }
    }
}
