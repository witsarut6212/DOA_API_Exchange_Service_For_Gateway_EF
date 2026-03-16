using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class EPhytoCertificateRequest
    {
        public EPhytoDocumentControl DocumentControl { get; set; } = null!;
        public List<EPhytoDetail> Details { get; set; } = new();
        public List<ThirdPartyFumigation>? ThirdPartyFumigations { get; set; }
        public List<TransitCountry>? TransitCountries { get; set; }
        public List<ContainerInfo>? Containers { get; set; }
        public List<EPhytoShippingMark>? ShippingMarks { get; set; }
        public List<EPhytoReferenceDocument>? ReferenceDocuments { get; set; }
    }

    public class EPhytoDocumentControl
    {
        public string ReferenceNumber { get; set; } = null!;
        public string DocumentFunctionCode { get; set; } = null!;
        public string FormType { get; set; } = null!;
        public string ReplacementForm { get; set; } = null!;
        public DateWrapper SubmitForm { get; set; } = null!;
        public string CertificateStatus { get; set; } = null!;
        public string? ApplicantDate { get; set; }
        public string SenderRegistrationID { get; set; } = null!;
        public List<Applicant> Applicant { get; set; } = new();
        public List<ConsignorExporter> ConsignorExporter { get; set; } = new();
        public List<OnBeHalfCareOf>? OnBeHalfCareOf { get; set; }
        public List<ConsigneeImporter> ConsigneeImporter { get; set; } = new();
        public List<ShipmentInfo> ShipmentInfo { get; set; } = new();
        public List<InspectionInfo> InspectionInfo { get; set; } = new();
    }

    public class DateWrapper
    {
        public string Date { get; set; } = null!;
    }

    public class DateTimeWrapper
    {
        public string DateTimeString { get; set; } = null!;
    }

    public class Applicant
    {
        public string Id { get; set; } = null!;
        public string IdType { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? StreetAndNumber { get; set; }
        public string? District { get; set; }
        public string? SubProvice { get; set; }
        public string? City { get; set; }
        public string? Postcode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
        public string? AttorneyIDCard { get; set; }
        public string? AttorneyName { get; set; }
    }

    public class ConsignorExporter
    {
        public string TaxNumber { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string? AttorneyIDCard { get; set; }
        public string? StreetAndNumber { get; set; }
        public string? SubDistrict { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Postcode { get; set; }
        public string? CountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
    }

    public class OnBeHalfCareOf
    {
        public string? Code { get; set; }
        public string? CompanyName { get; set; }
        public string? AddrressLine1 { get; set; }
        public string? AddrressLine2 { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? Postcode { get; set; }
        public string? CountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
    }

    public class ConsigneeImporter
    {
        public string? TaxNumber { get; set; }
        public string CompanyName { get; set; } = null!;
        public string? AddrressLine1 { get; set; }
        public string? AddrressLine2 { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? Postcode { get; set; }
        public string? CountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
    }

    public class ShipmentInfo
    {
        public List<Transportation> Transportation { get; set; } = new();
        public string DestinationCountry { get; set; } = null!;
        public List<PortInfo> DischargePort { get; set; } = new();
        public List<PortInfo> LoadingPort { get; set; } = new();
        public DateWrapper DepartureOnBoard { get; set; } = null!;
        public List<AmountWrapper> TotalAmount { get; set; } = new();
        public List<QuantityWrapper> TotalPackage { get; set; } = new();
        public List<WeightWrapper> GrossWeight { get; set; } = new();
        public List<WeightWrapper> NetWeight { get; set; } = new();
        public string PlaceOriginCountry { get; set; } = null!;
    }

    public class Transportation
    {
        public string ModeCode { get; set; } = null!;
        public string VesselName { get; set; } = null!;
        public string BillNumber { get; set; } = null!;
    }

    public class PortInfo
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class AmountWrapper
    {
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = null!;
    }

    public class QuantityWrapper
    {
        public decimal Quantity { get; set; }
        public string TypeCode { get; set; } = null!;
    }

    public class WeightWrapper
    {
        public decimal Weight { get; set; }
        public string UnitCode { get; set; } = null!;
    }

    public class InspectionInfo
    {
        public string IssueLocationCode { get; set; } = null!;
        public string OperateType { get; set; } = null!;
        public string PlaceCode { get; set; } = null!;
        public List<DateTimeWrapper> Due { get; set; } = new(); // ตาม Schema ใหม่
    }

    public class EPhytoDetail
    {
        public int ItemNumber { get; set; }

        [JsonProperty("TradeDescription")]
        public string TradeDescription { get; set; } = null!;

        public string CommonName { get; set; } = null!;
        public string ScientificName { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public List<TariffInfo> TariffInfo { get; set; } = new();

        [JsonProperty("ThaiGoodsDescription")]
        public string ThaiGoodsDescription { get; set; } = null!;

        public List<WeightWrapper> NetWeight { get; set; } = new();
        public List<QuantityWrapper> Quantityinfo { get; set; } = new();
        public List<PhysicalPackage> PhysicalPackage { get; set; } = new();
        public AmountValueWrapper AmountInfo { get; set; } = null!;
        public string? Remark { get; set; }
        public int Packaging { get; set; }
        public int ObjectiveCode { get; set; }
        public int SendLab { get; set; }
        public List<AdditionalDescription> AdditionalDescription { get; set; } = new();
        public int ACFSCollaboration { get; set; }
        public List<OriginCountryItem> OriginCountries { get; set; } = new();
        public List<TreatmentItem>? Treatments { get; set; }
        public List<AdditionalDeclaration>? AdditionalDeclarations { get; set; }
    }

    public class TariffInfo
    {
        public string Code { get; set; } = null!;
        public string Statistical { get; set; } = null!;
    }

    public class PhysicalPackage
    {
        public int LevelCode { get; set; }
        public decimal Quantity { get; set; }
        public string UnitCode { get; set; } = null!;
    }

    public class AmountValueWrapper
    {
        public decimal Value { get; set; }
        public string CurrencyCode { get; set; } = null!;
    }

    public class AdditionalDescription
    {
        public string? Lab { get; set; }
        public string? Product { get; set; }
    }

    public class OriginCountryItem
    {
        public int ItemNumber { get; set; }
        public string CountryCode { get; set; } = null!;
        public string? RegistrationNumber { get; set; }
        public string? LocationName { get; set; }
        public DateWrapper IssueDate { get; set; } = null!;
        public string? Remark { get; set; }
        public ItemReferenceDocument ItemReferenceDocument { get; set; } = null!;
    }

    public class ItemReferenceDocument
    {
        public int ItemNumber { get; set; }
        public string DocumentNumber { get; set; } = null!;
        public TypeCodeName DocumentType { get; set; } = null!;
        public List<TaxNumberWrapper> ProviderAuthority { get; set; } = new();
        public DateWrapper Issued { get; set; } = null!;
        public DateWrapper Expire { get; set; } = null!;
        public List<EPhytoAttachmentInfo> Attachment { get; set; } = new();
        public string? Remark { get; set; }
    }

    public class TypeCodeName
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class TaxNumberWrapper
    {
        public string TaxNumber { get; set; } = null!;
    }

    public class EPhytoAttachmentInfo
    {
        public string Filename { get; set; } = null!;
        public string FileID { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
    }

    public class TreatmentItem
    {
        public int ItemNumber { get; set; }
        public TypeCodeDescription Method { get; set; } = null!;
        public StartDateTimeWrapper CompletionPeriod { get; set; } = null!;
        public string? Chemical { get; set; }
        public DurationWrapper? DurationMeasure { get; set; }
        public TemperatureWrapper? Temperature { get; set; }
        public ConcentrationWrapper? Concentration { get; set; }
        public string? Remark { get; set; }
    }

    public class TypeCodeDescription
    {
        public string TypeCode { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class StartDateTimeWrapper
    {
        public string StartDateTime { get; set; } = null!;
    }

    public class DurationWrapper
    {
        public decimal Duration { get; set; }
        public string UnitCode { get; set; } = null!;
    }

    public class TemperatureWrapper
    {
        public string ValueMeasure { get; set; } = null!;
        public string UnitCode { get; set; } = null!;
    }

    public class ConcentrationWrapper
    {
        public string ValueMeasure { get; set; } = null!;
        public string UnitCode { get; set; } = null!;
    }

    public class AdditionalDeclaration
    {
        public int ItemNumber { get; set; }
        public string SubjectCode { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<AdditionalReferenceDocument>? AdditionalReferenceDocuments { get; set; }
    }

    public class AdditionalReferenceDocument
    {
        public TypeCodeName DocumentType { get; set; } = null!;
        
        [JsonProperty(".Attachment")]
        public List<EPhytoAttachmentInfo>? Attachment { get; set; }
    }

    public class ThirdPartyFumigation
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyCode { get; set; }
        public string? LocationName { get; set; }
        public string? StreetAndNumber { get; set; }
        public string? SubDistrict { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Postcode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
        public DateTimeWrapper Due { get; set; } = null!;
    }

    public class TransitCountry
    {
        public string CountryCode { get; set; } = null!;
        public string? City { get; set; }
        public string? PortCode { get; set; }
        public string? Remark { get; set; }
    }

    public class ContainerInfo
    {
        public string Number { get; set; } = null!;
        public string SealNumber { get; set; } = null!;
    }

    public class EPhytoShippingMark
    {
        public string Marking { get; set; } = null!;
    }

    public class EPhytoReferenceDocument
    {
        public int ItemNumber { get; set; }
        public string DocumentNumber { get; set; } = null!;
        
        [JsonProperty("DocumentType")] 
        public TypeCodeName DocumentType { get; set; } = null!;
        
        public int DetailItemNumber { get; set; }
        public List<TaxNumberWrapper> ProviderAuthority { get; set; } = new();
        public DateWrapper Issued { get; set; } = null!;
        public DateWrapper Expire { get; set; } = null!;
        public List<AttachmentInfoWithId> Attachment { get; set; } = new();
        public string? Remark { get; set; }
    }

    public class AttachmentInfoWithId
    {
        public string Filename { get; set; } = null!;
        public string FileId { get; set; } = null!; 
        public string FileUrl { get; set; } = null!;
    }
}
