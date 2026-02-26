using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageSubmissionRequest
{
    public int Id { get; set; }

    public string ReferenceNumber { get; set; } = null!;

    public string LicenseNumber { get; set; } = null!;

    public string FormType { get; set; } = null!;

    public string ReplacementForm { get; set; } = null!;

    public string RegistrtionId { get; set; } = null!;

    public DateOnly ApplicantDate { get; set; }

    public string ApplicantId { get; set; } = null!;

    public string ApplicantName { get; set; } = null!;

    public string ConsignorTaxNumber { get; set; } = null!;

    public string ConsignorName { get; set; } = null!;

    public string ConsignorCountryCode { get; set; } = null!;

    public string ConsigneeTaxNumber { get; set; } = null!;

    public string ConsigneeName { get; set; } = null!;

    public string ConsigneeCountryCode { get; set; } = null!;

    public string DestinationCountry { get; set; } = null!;

    public string PortDischargeCode { get; set; } = null!;

    public string PortDischargeName { get; set; } = null!;

    public string PortLoadingCode { get; set; } = null!;

    public string PortLoadingName { get; set; } = null!;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = null!;

    public int TotalPackage { get; set; }

    public string PackageUnitCode { get; set; } = null!;

    public decimal GrossWeight { get; set; }

    public string GrossWeightUnitCode { get; set; } = null!;

    public decimal TotalNetWeight { get; set; }

    public string NetWeightUnitCode { get; set; } = null!;

    public string PlaceOfOrigin { get; set; } = null!;

    public int EservicePayloadId { get; set; }

    public string? FlagUpdate { get; set; }

    public string SendStatus { get; set; } = null!;

    public string? MarkSend { get; set; }

    public string? TxSuccess { get; set; }

    public DateTime? SendDate { get; set; }

    public DateTime? SuccessDate { get; set; }

    public DateTime InboundAt { get; set; }

    public DateTime SystemTime { get; set; }

    public string MessageId { get; set; } = null!;

    public string RefMessageId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
