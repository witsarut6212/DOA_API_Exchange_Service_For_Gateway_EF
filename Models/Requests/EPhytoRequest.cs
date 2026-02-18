using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class EPhytoRequest
    {
        [JsonProperty("xc_document")]
        public XcDocument XcDocument { get; set; } = null!;

        [JsonProperty("consignment")]
        public Consignment Consignment { get; set; } = null!;

        [JsonProperty("items")]
        public List<EPhytoItem> Items { get; set; } = new();

        [JsonProperty("phytoCerts")]
        public List<ReferenceDocRequest>? PhytoCerts { get; set; }
    }

    public class XcDocument
    {
        [JsonProperty("doc_name")]
        public string? DocName { get; set; }

        [JsonProperty("doc_id")]
        public string DocId { get; set; } = null!;

        [JsonProperty("doc_description")]
        public string? DocDescription { get; set; }

        [JsonProperty("doc_type")]
        public string DocType { get; set; } = null!;

        [JsonProperty("status_code")]
        public string StatusCode { get; set; } = null!;

        [JsonProperty("issue_date")]
        public string IssueDate { get; set; } = null!;

        [JsonProperty("issue_party_id")]
        public string? IssuePartyId { get; set; }

        [JsonProperty("issue_party_name")]
        public string? IssuePartyName { get; set; }

        [JsonProperty("include_notes")]
        public List<IncludeNote>? IncludeNotes { get; set; }

        [JsonProperty("reference_docs")]
        public List<ReferenceDocRequest>? ReferenceDocs { get; set; }

        [JsonProperty("signatory_authen")]
        public SignatoryAuthen? SignatoryAuthen { get; set; }
    }

    public class IncludeNote
    {
        [JsonProperty("subject")]
        public string? Subject { get; set; }

        [JsonProperty("contents")]
        public List<NoteContent>? Contents { get; set; }
    }

    public class NoteContent
    {
        [JsonProperty("content")]
        public string? Content { get; set; }
    }

    public class SignatoryAuthen
    {
        [JsonProperty("actual_datetime")]
        public string? ActualDatetime { get; set; }

        [JsonProperty("issue_location")]
        public object? IssueLocation { get; set; }

        [JsonProperty("provider_party")]
        public ProviderParty? ProviderParty { get; set; }

        [JsonProperty("include_clauses")]
        public List<ClauseItem>? IncludeClauses { get; set; }
    }

    public class NameLocation
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    public class ProviderParty
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("specfied_person")]
        public SpecifiedPerson? SpecifiedPerson { get; set; }
    }

    public class SpecifiedPerson
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("attained_qualification")]
        public Qualification? AttainedQualification { get; set; }
    }

    public class Qualification
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("abbrev_name")]
        public string? AbbrevName { get; set; }
    }

    public class ClauseItem
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("content")]
        public string? Content { get; set; }
    }

    public class Consignment
    {
        [JsonProperty("consignor_party")]
        public Party? ConsignorParty { get; set; }

        [JsonProperty("consignee_party")]
        public Party? ConsigneeParty { get; set; }

        [JsonProperty("export_country")]
        public CountryInfo? ExportCountry { get; set; }

        [JsonProperty("reexport_country")]
        public CountryInfo? ReexportCountry { get; set; }

        [JsonProperty("import_country")]
        public CountryInfo? ImportCountry { get; set; }

        [JsonProperty("transit_countries")]
        public List<CountryInfo>? TransitCountries { get; set; }

        [JsonProperty("unloading_baseport")]
        public NameLocation? UnloadingBasePort { get; set; }

        [JsonProperty("examination_event")]
        public ExaminationEvent? ExaminationEvent { get; set; }

        [JsonProperty("main_carriages")]
        public List<MainCarriageRequest>? MainCarriages { get; set; }

        [JsonProperty("utilize_transport")]
        public List<UtilizeTransportRequest>? UtilizeTransport { get; set; }

        // Keeping old fields for a while to avoid immediate service breakage if possible, 
        // but transitioning to object structure
        [JsonIgnore]
        public string ExportCountryId => ExportCountry?.Id ?? "";
        [JsonIgnore]
        public string ImportCountryId => ImportCountry?.Id ?? "";
    }

    public class Party
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("postcode")]
        public string? Postcode { get; set; }
        [JsonProperty("adress_line1")]
        public string? AddressLine1 { get; set; }
        [JsonProperty("addres_line2")]
        public string? AddressLine2 { get; set; }
        [JsonProperty("city_name")]
        public string? CityName { get; set; }
        [JsonProperty("country_id")]
        public string? CountryId { get; set; }
    }

    public class CountryInfo
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("subordinary")]
        public SubordinaryInfo? Subordinary { get; set; }
    }

    public class SubordinaryInfo
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("hierachi_level")]
        public string? HierachiLevel { get; set; }
    }

    public class ExaminationEvent
    {
        [JsonProperty("occur_location_name")]
        public string? OccurLocationName { get; set; }
    }

    public class UtilizeTransportRequest
    {
        [JsonProperty("equipment_id")]
        public string? EquipmentId { get; set; }
        [JsonProperty("seal_number")]
        public string? SealNumber { get; set; }
    }

    public class MainCarriageRequest
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("mode_code")]
        public string? ModeCode { get; set; }
        [JsonProperty("transport_mean_name")]
        public string? TransportMeanName { get; set; }
    }

    public class EPhytoItem
    {
        [JsonProperty("sequence_no")]
        public string? SequenceNo { get; set; }

        [JsonProperty("descriptions")]
        public List<NameLocation>? Descriptions { get; set; }

        [JsonProperty("common_names")]
        public List<NameLocation>? CommonNames { get; set; }

        [JsonProperty("scient_name")]
        public string? ScientName { get; set; }

        [JsonProperty("intend_uses")]
        public List<NameLocation>? IntendUses { get; set; }

        [JsonProperty("net_weight")]
        public WeightVolume? NetWeight { get; set; }

        [JsonProperty("gross_weight")]
        public WeightVolume? GrossWeight { get; set; }

        [JsonProperty("additional_notes")]
        public List<IncludeNote>? AdditionalNotes { get; set; }

        [JsonProperty("applicable_classifications")]
        public List<Classification>? ApplicableClassifications { get; set; }

        [JsonProperty("physical_packages")]
        public List<Package>? PhysicalPackages { get; set; }

        [JsonProperty("origin_countries")]
        public List<OriginCountry>? OriginCountries { get; set; }

        [JsonProperty("applied_processes")]
        public List<AppliedProcess>? AppliedProcesses { get; set; }
    }

    public class WeightVolume
    {
        [JsonProperty("weight")]
        public string? Weight { get; set; }
        [JsonProperty("volume")]
        public string? Volume { get; set; }
        [JsonProperty("unit_code")]
        public string? UnitCode { get; set; }
    }

    public class Classification
    {
        [JsonProperty("system_name")]
        public string? SystemName { get; set; }
        [JsonProperty("class_code")]
        public string? ClassCode { get; set; }
        [JsonProperty("class_names")]
        public List<ClassNameItem>? ClassNames { get; set; }
    }

    public class ClassNameItem
    {
        [JsonProperty("class_name")]
        public string? ClassName { get; set; }
    }

    public class Package
    {
        [JsonProperty("level_code")]
        public string? LevelCode { get; set; }
        [JsonProperty("type_code")]
        public string? TypeCode { get; set; }
        [JsonProperty("quantity")]
        public string? Quantity { get; set; }
        [JsonProperty("shipping_marks")]
        public List<ShippingMark>? ShippingMarks { get; set; }
    }

    public class ShippingMark
    {
        [JsonProperty("marking")]
        public string? Marking { get; set; }
    }

    public class OriginCountry
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("subordinary_country")]
        public SubordinaryCountry? SubordinaryCountry { get; set; }
    }

    public class SubordinaryCountry
    {
        [JsonProperty("subdivision_id")]
        public string? SubdivisionId { get; set; }
        [JsonProperty("subdivision_name")]
        public string? SubdivisionName { get; set; }
        [JsonProperty("hierachi_level")]
        public string? HierachiLevel { get; set; }
        [JsonProperty("activity_authorize")]
        public ActivityAuthorize? ActivityAuthorize { get; set; }
    }

    public class ActivityAuthorize
    {
        [JsonProperty("party_id")]
        public string? PartyId { get; set; }
        [JsonProperty("party_name")]
        public string? PartyName { get; set; }
    }

    public class AppliedProcess
    {
        [JsonProperty("type_code")]
        public string? TypeCode { get; set; }
        [JsonProperty("complete_period")]
        public CompletePeriod? CompletePeriod { get; set; }
        [JsonProperty("duration_measure")]
        public string? DurationMeasure { get; set; }
        [JsonProperty("duration_measuer_unit")]
        public string? DurationMeasuerUnit { get; set; }
        [JsonProperty("characteristics")]
        public List<Newtonsoft.Json.Linq.JObject>? Characteristics { get; set; }
    }

    public class CompletePeriod
    {
        [JsonProperty("start_date")]
        public string? StartDate { get; set; }
        [JsonProperty("end_date")]
        public string? EndDate { get; set; }
    }

    public class ReferenceDocRequest
    {
        [JsonProperty("doc_id")]
        public string? DocId { get; set; }

        [JsonProperty("documentNo")]
        public string? DocumentNo { get; set; }

        [JsonProperty("filename")]
        public string? Filename { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("PdfObject")]
        public string? PdfObject { get; set; }

        [JsonProperty("issue_date")]
        public string? IssueDate { get; set; }

        [JsonProperty("type_code")]
        public string? TypeCode { get; set; }

        [JsonProperty("relation_type_code")]
        public string? RelationTypeCode { get; set; }

        [JsonProperty("informaton")]
        public string? Information { get; set; }
    }
}
