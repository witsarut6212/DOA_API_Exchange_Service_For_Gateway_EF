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

    public string CliendId { get; set; } = null!; // สังเกตว่าสะกด CliendId ตามรูป

    public string? SecretKey { get; set; }

    public sbyte? IsActive { get; set; }

    public sbyte? IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? SystemTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
