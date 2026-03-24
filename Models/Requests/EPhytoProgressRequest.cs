using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class EPhytoProgressRequest
    {
        [Required]
        [JsonProperty("documentControl")]
        public DocumentControl DocumentControl { get; set; } = null!;

        [JsonProperty("details")]
        public List<ProgressDetail>? Details { get; set; }

        [JsonProperty("payment")]
        public PaymentInfo? Payment { get; set; }

        [JsonProperty("additionalDocuments")]
        public List<AdditionalDocument>? AdditionalDocuments { get; set; }

        // --- Logic ควบคุมการแสดงผลฟิลด์ใน JSON ---

        // จะแสดง additionalDocuments เฉพาะเมื่อเป็น ACCEPT และ AC015 เท่านั้น
        public bool ShouldSerializeAdditionalDocuments()
        {
            return DocumentControl?.ResponseInfo?.Status == "ACCEPT" && 
                   DocumentControl?.ResponseInfo?.Code == "AC015";
        }

        // จะแสดง certificateInfo เฉพาะเมื่อเป็น ACCEPT และ AC015 เท่านั้น
        public bool ShouldSerializeCertificateInfo()
        {
            return DocumentControl?.ResponseInfo?.Status == "ACCEPT" &&
                   DocumentControl?.ResponseInfo?.Code == "AC015";
        }

        // จะแสดง payment เฉพาะเมื่อเป็น ACCEPT และ AC009 เท่านั้น
        public bool ShouldSerializePayment()
        {
            return DocumentControl?.ResponseInfo?.Status == "ACCEPT" && 
                   DocumentControl?.ResponseInfo?.Code == "AC009";
        }
    }

    public class DocumentControl
    {
        [Required]
        [JsonProperty("referenceNumber")]
        public string ReferenceNumber { get; set; } = null!;

        [JsonProperty("documentNumber")]
        public string? DocumentNumber { get; set; }

        [Required]
        [JsonProperty("responseInfo")]
        public ResponseInfo ResponseInfo { get; set; } = null!;

        [JsonProperty("messageType")]
        public string? MessageType { get; set; }

        [JsonProperty("remark")]
        public string? Remark { get; set; }

        [JsonProperty("registrationId")]
        public string? RegistrationId { get; set; }

        [JsonProperty("itemNumber")]
        public int? ItemNumber { get; set; }

        [JsonProperty("reasonCode")]
        public string? ReasonCode { get; set; }

        [JsonProperty("reasonDescription")]
        public string? ReasonDescription { get; set; }
    }

    public class ResponseInfo
    {
        [Required]
        [JsonProperty("dateTime")]
        public DateTime DateTime { get; set; }

        [Required]
        [JsonProperty("status")]
        public string Status { get; set; } = null!;

        [Required]
        [JsonProperty("code")]
        public string Code { get; set; } = null!;
    }

    public class ProgressDetail
    {
        [JsonProperty("itemNumber")]
        public int? itemNumber { get; set; }

        [JsonProperty("ItemNumber")]
        public int? ItemNumberPascal { get; set; }

        [JsonIgnore]
        public int? ItemNumber => itemNumber ?? ItemNumberPascal;

        [JsonProperty("reasonCode")]
        public string? ReasonCode { get; set; }

        [JsonProperty("reasonDescription")]
        public string? ReasonDescription { get; set; }
    }

    public class PaymentInfo
    {
        [JsonProperty("itemNumber")]
        public int? ItemNumber { get; set; }

        [JsonProperty("referenceNumber1")]
        public string? ReferenceNumber1 { get; set; }

        [JsonProperty("referenceNumber2")]
        public string? ReferenceNumber2 { get; set; }

        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        [JsonProperty("bankCode")]
        public string? BankCode { get; set; }

        [JsonProperty("compCode")]
        public string? CompCode { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("base64")]
        public string? Billpayment { get; set; }
    }

    public class AdditionalDocument
    {
        [JsonProperty("itemNumber")]
        public int? itemNumber { get; set; }

        [JsonProperty("ItemNumber")]
        public int? ItemNumberPascal { get; set; }

        [JsonIgnore]
        public int? ItemNumber => itemNumber ?? ItemNumberPascal;

        [JsonProperty("documentInfo")]
        public DocumentInfo? DocumentInfo { get; set; }

        [JsonProperty("certificateInfo")]
        public CertificateInfo? CertificateInfo { get; set; }

        [JsonProperty("detail")]
        public AdditionalDocumentDetail? Detail { get; set; }

        [JsonProperty("providerAuthority")]
        public ProviderAuthority? ProviderAuthority { get; set; }

        [JsonProperty("attachment")]
        public AttachmentInfo? Attachment { get; set; }
    }

    public class AdditionalDocumentDetail
    {
        [JsonProperty("itemNumber")]
        public int? ItemNumber { get; set; }
    }

    public class DocumentInfo
    {
        [JsonProperty("typeCode")]
        public string? TypeCode { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("licenseNumber")]
        public string? LicenseNumber { get; set; }

        [JsonProperty("issueDate")]
        public string? IssueDate { get; set; }

        [JsonProperty("expireDate")]
        public string? ExpireDate { get; set; }
    }

    public class CertificateInfo
    {
        [JsonProperty("licenseNumber")]
        public string? LicenseNumber { get; set; }

        [JsonProperty("docType")]
        public string? DocType { get; set; }

        [JsonProperty("docStatus")]
        public string? DocStatus { get; set; }
    }



    public class ProviderAuthority
    {
        [JsonProperty("taxNumber")]
        public string? TaxNumber { get; set; }
    }

    public class AttachmentInfo
    {
        [JsonProperty("filename")]
        public string? Filename { get; set; }

        [JsonProperty("fileType")]
        public string? FileType { get; set; }

        [JsonProperty("fileID")]
        public string? FileID { get; set; }

        [JsonProperty("fileURL")]
        public string? FileURL { get; set; }

        [JsonProperty("fileBase64")]
        public string? FileBase64 { get; set; }

        [JsonProperty("remark")]
        public string? Remark { get; set; }
    }
}
