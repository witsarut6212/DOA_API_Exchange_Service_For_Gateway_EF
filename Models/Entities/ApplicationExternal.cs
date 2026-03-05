using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class ApplicationExternal
{
    public int Id { get; set; }

    public int AppRoleId { get; set; }

    public string AppName { get; set; } = null!;

    public string AppNickName { get; set; } = null!;

    public string HostUrl { get; set; } = null!;

    public string? CallbackUrl { get; set; }

    public string CliendId { get; set; } = null!; // System generate UUID v4

    public string? SecretKey { get; set; }

    public string IsActive { get; set; } = "Y";

    public string IsVerified { get; set; } = "N";

    public DateTime? VerfiedAt { get; set; }

    public DateTime SystemTime { get; set; } // Not Null ตามรูป

    public DateTime? CreatedAt { get; set; } // ในรูป allow null (created_at)

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
