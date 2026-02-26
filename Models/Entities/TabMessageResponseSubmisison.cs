using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageResponseSubmisison
{
    public int Id { get; set; }

    public string ResponseType { get; set; } = null!;

    public string ReferenceNumber { get; set; } = null!;

    public string DocumentNumber { get; set; } = null!;

    public string MessageType { get; set; } = null!;

    public string ResponseCode { get; set; } = null!;

    public string ResponseMessage { get; set; } = null!;

    public DateTime ResponseDateTime { get; set; }

    public string RegistrationId { get; set; } = null!;

    public string ResponseToId { get; set; } = null!;

    public string? QueueStatus { get; set; }

    public int? QueueId { get; set; }

    public DateTime SystemTime { get; set; }

    public string? MessageId { get; set; }

    public string? RefMessageId { get; set; }

    public int ResponsePayloadId { get; set; }

    public string FlagUpdate { get; set; } = null!;

    public string? MarkSend { get; set; }

    public string? TxSuccess { get; set; }

    public DateTime? SendDate { get; set; }

    public DateTime? SuccessDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
