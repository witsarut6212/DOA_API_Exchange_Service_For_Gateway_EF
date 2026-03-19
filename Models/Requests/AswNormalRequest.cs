using Newtonsoft.Json;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class AswNormalRequest
    {
        [JsonProperty("detail")]
        public List<AswDetail> Detail { get; set; } = new();

        [JsonProperty("doc_name")]
        public string? DocName { get; set; }

        [JsonProperty("doc_id")]
        public string? DocId { get; set; }

        [JsonProperty("doc_description")]
        public string? DocDescription { get; set; }

        [JsonProperty("doc_type")]
        public string? DocType { get; set; }

        [JsonProperty("doc_status")]
        public string? DocStatus { get; set; }

        [JsonProperty("issuer_id")]
        public string? IssuerId { get; set; }

        [JsonProperty("issuer_name")]
        public string? IssuerName { get; set; }

        [JsonProperty("ref_issue_datetime")]
        public string? RefIssueDatetime { get; set; }

        [JsonProperty("ref_type")]
        public string? RefType { get; set; }

        [JsonProperty("ref_relation")]
        public string? RefRelation { get; set; }

        [JsonProperty("ref_id")]
        public string? RefId { get; set; }

        [JsonProperty("ref_info")]
        public string? RefInfo { get; set; }

        [JsonProperty("auth_location_id")]
        public string? AuthLocationId { get; set; }

        [JsonProperty("auth_location_name")]
        public string? AuthLocationName { get; set; }

        [JsonProperty("auth_provider_id")]
        public string? AuthProviderId { get; set; }

        [JsonProperty("auth_provider_name")]
        public string? AuthProviderName { get; set; }

        [JsonProperty("auth_person")]
        public string? AuthPerson { get; set; }

        [JsonProperty("auth_position_name")]
        public string? AuthPositionName { get; set; }

        [JsonProperty("auth_position_abb")]
        public string? AuthPositionAbb { get; set; }

        [JsonProperty("exporter_name")]
        public string? ExporterName { get; set; }

        [JsonProperty("exporter_line1")]
        public string? ExporterLine1 { get; set; }

        [JsonProperty("exporter_line2")]
        public string? ExporterLine2 { get; set; }

        [JsonProperty("exporter_line3")]
        public string? ExporterLine3 { get; set; }

        [JsonProperty("exporter_line4")]
        public string? ExporterLine4 { get; set; }

        [JsonProperty("exporter_line5")]
        public string? ExporterLine5 { get; set; }

        [JsonProperty("exporter_city_name")]
        public string? ExporterCityName { get; set; }

        [JsonProperty("exporter_post_code")]
        public string? ExporterPostCode { get; set; }

        [JsonProperty("exporter_country_code")]
        public string? ExporterCountryCode { get; set; }

        [JsonProperty("exporter_country_name")]
        public string? ExporterCountryName { get; set; }

        [JsonProperty("exporter_address_type")]
        public string? ExporterAddressType { get; set; }

        [JsonProperty("consignee_name")]
        public string? ConsigneeName { get; set; }

        [JsonProperty("consignee_line1")]
        public string? ConsigneeLine1 { get; set; }

        [JsonProperty("consignee_line2")]
        public string? ConsigneeLine2 { get; set; }

        [JsonProperty("consignee_line3")]
        public string? ConsigneeLine3 { get; set; }

        [JsonProperty("consignee_line4")]
        public string? ConsigneeLine4 { get; set; }

        [JsonProperty("consignee_line5")]
        public string? ConsigneeLine5 { get; set; }

        [JsonProperty("consignee_city_name")]
        public string? ConsigneeCityName { get; set; }

        [JsonProperty("consignee_post_code")]
        public string? ConsigneePostCode { get; set; }

        [JsonProperty("consignee_country_code")]
        public string? ConsigneeCountryCode { get; set; }

        [JsonProperty("export_country_code")]
        public string? ExportCountryCode { get; set; }

        [JsonProperty("export_country_name")]
        public string? ExportCountryName { get; set; }

        [JsonProperty("reexport_country_code")]
        public string? ReexportCountryCode { get; set; }

        [JsonProperty("reexport_country_name")]
        public string? ReexportCountryName { get; set; }

        [JsonProperty("import_country_code")]
        public string? ImportCountryCode { get; set; }

        [JsonProperty("import_country_name")]
        public string? ImportCountryName { get; set; }

        [JsonProperty("unloading_port_code")]
        public string? UnloadingPortCode { get; set; }

        [JsonProperty("unloading_port_name")]
        public string? UnloadingPortName { get; set; }

        [JsonProperty("transport_mode")]
        public string? TransportMode { get; set; }

        [JsonProperty("vessel_number")]
        public string? VesselNumber { get; set; }

        [JsonProperty("vessel_name")]
        public string? VesselName { get; set; }

        [JsonProperty("notes")]
        public List<AswNote> Notes { get; set; } = new();

        [JsonProperty("clauses")]
        public List<AswClause> Clauses { get; set; } = new();

        [JsonProperty("transits")]
        public List<object> Transits { get; set; } = new();
    }

    public class AswDetail
    {
        [JsonProperty("item_no")]
        public int ItemNo { get; set; }

        [JsonProperty("descriptions")]
        public List<NameEntry> Descriptions { get; set; } = new();

        [JsonProperty("product_name")]
        public string? ProductName { get; set; }

        [JsonProperty("product_scient_name")]
        public string? ProductScientName { get; set; }

        [JsonProperty("product_batch_id")]
        public string? ProductBatchId { get; set; }

        [JsonProperty("product_intended_use")]
        public string? ProductIntendedUse { get; set; }

        [JsonProperty("net_weight")]
        public decimal NetWeight { get; set; }

        [JsonProperty("net_weight_unit")]
        public string? NetWeightUnit { get; set; }

        [JsonProperty("gross_weight")]
        public decimal GrossWeight { get; set; }

        [JsonProperty("gross_weight_unit")]
        public string? GrossWeightUnit { get; set; }

        [JsonProperty("package_level")]
        public string? PackageLevel { get; set; }

        [JsonProperty("package_type")]
        public string? PackageType { get; set; }

        [JsonProperty("package_amount")]
        public decimal PackageAmount { get; set; }

        [JsonProperty("shipping_marks")]
        public string? ShippingMarks { get; set; }

        [JsonProperty("producer_location_code")]
        public string? ProducerLocationCode { get; set; }

        [JsonProperty("producer_location_name")]
        public string? ProducerLocationName { get; set; }

        [JsonProperty("additionals")]
        public List<AswAdditionalInfo> Additionals { get; set; } = new();

        [JsonProperty("origin_countries")]
        public List<AswOriginCountry> OriginCountries { get; set; } = new();

        [JsonProperty("processes")]
        public List<AswProcess> Processes { get; set; } = new();
    }

    public class NameEntry { [JsonProperty("name")] public string? Name { get; set; } }

    public class AswAdditionalInfo
    {
        [JsonProperty("note_subject")] public string? NoteSubject { get; set; }
        [JsonProperty("note_content")] public string? NoteContent { get; set; }
    }

    public class AswOriginCountry
    {
        [JsonProperty("origin_country_code")] public string? OriginCountryCode { get; set; }
        [JsonProperty("origin_country_name")] public string? OriginCountryName { get; set; }
        [JsonProperty("origin_province_code")] public string? OriginProvinceCode { get; set; }
        [JsonProperty("origin_province_name")] public string? OriginProvinceName { get; set; }
        [JsonProperty("origin_hierarchi_level")] public string? OriginHierarchiLevel { get; set; }
        [JsonProperty("producer_code")] public string? ProducerCode { get; set; }
        [JsonProperty("producer_name")] public string? ProducerName { get; set; }
        [JsonProperty("producer_role")] public string? ProducerRole { get; set; }
    }

    public class AswProcess
    {
        [JsonProperty("process_type")] public string? ProcessType { get; set; }
        [JsonProperty("process_start_date")] public string? ProcessStartDate { get; set; }
        [JsonProperty("process_end_date")] public string? ProcessEndDate { get; set; }
        [JsonProperty("process_duration")] public decimal? ProcessDuration { get; set; }
        [JsonProperty("process_duration_unit")] public string? ProcessDurationUnit { get; set; }
        [JsonProperty("characteristics")] public List<AswCharacteristic> Characteristics { get; set; } = new();
    }

    public class AswCharacteristic
    {
        [JsonProperty("process_character_code")] public string? ProcessCharacterCode { get; set; }
        [JsonProperty("process_character_desc")] public string? ProcessCharacterDesc { get; set; }
        [JsonProperty("process_value")] public string? ProcessValue { get; set; }
        [JsonProperty("process_value_unit")] public string? ProcessValueUnit { get; set; }
    }

    public class AswNote
    {
        [JsonProperty("note_subject_code")] public string? NoteSubjectCode { get; set; }
        [JsonProperty("note_content")] public string? NoteContent { get; set; }
    }

    public class AswClause
    {
        [JsonProperty("clause_id")] public int ClauseId { get; set; }
        [JsonProperty("clause_content")] public string? ClauseContent { get; set; }
    }
}
