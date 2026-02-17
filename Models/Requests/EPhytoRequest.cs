using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Requests
{
    public class EPhytoRequest
    {
        [Required]
        [JsonProperty("xc_document")]
        public XcDocument XcDocument { get; set; } = null!;

        [Required]
        [JsonProperty("consignment")]
        public Consignment Consignment { get; set; } = null!;

        [Required]
        [JsonProperty("items")]
        public List<EPhytoItem> Items { get; set; } = new();

        [JsonProperty("phytoCerts")]
        public List<ReferenceDocRequest>? PhytoCerts { get; set; }
    }

    public class XcDocument
    {
        [JsonProperty("doc_name")]
        public string? DocName { get; set; }

        [Required]
        [JsonProperty("doc_id")]
        public string DocId { get; set; } = null!;

        [Required]
        [JsonProperty("doc_type")]
        public string DocType { get; set; } = null!;

        [Required]
        [JsonProperty("status_code")]
        public string StatusCode { get; set; } = null!;

        [Required]
        [JsonProperty("issue_date")]
        public string IssueDate { get; set; } = null!;

        [JsonProperty("issue_party_name")]
        public string? IssuePartyName { get; set; }

        [JsonProperty("include_notes")]
        public List<IncludeNote>? IncludeNotes { get; set; }

        [JsonProperty("signatory_authen")]
        public SignatoryAuthen? SignatoryAuthen { get; set; }

        [JsonProperty("reference_docs")]
        public List<ReferenceDocRequest>? ReferenceDocs { get; set; }
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
        public object? IssueLocation { get; set; } // Can be string or object in schema

        [JsonProperty("provider_party")]
        public ProviderParty? ProviderParty { get; set; }
    }

    public class ProviderParty
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("specfied_person_name")]
        public string? SpecfiedPersonName { get; set; }
    }

    public class Consignment
    {
        [Required]
        [JsonProperty("export_country_id")]
        public string ExportCountryId { get; set; } = null!;

        [Required]
        [JsonProperty("import_country_id")]
        public string ImportCountryId { get; set; } = null!;

        [JsonProperty("consignor_party")]
        public Party? ConsignorParty { get; set; }

        [JsonProperty("consignee_party")]
        public Party? ConsigneeParty { get; set; }

        [JsonProperty("unloading_baseport")]
        public Port? UnloadingBasePort { get; set; }

        [JsonProperty("utilize_transport")]
        public UtilizeTransportRequest? UtilizeTransport { get; set; }

        [JsonProperty("main_carriages")]
        public List<MainCarriageRequest>? MainCarriages { get; set; }
    }

    public class Party
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("adress_line1")]
        public string? AddressLine1 { get; set; }
    }

    public class Port
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    public class UtilizeTransportRequest
    {
        [JsonProperty("seal_number")]
        public string? SealNumber { get; set; }
    }

    public class MainCarriageRequest
    {
        [JsonProperty("mode_code")]
        public string? ModeCode { get; set; }

        [JsonProperty("transport_mean_name")]
        public string? TransportMeanName { get; set; }

        [JsonProperty("trasport_mean_name")] // Support for misspelled version in old code
        public string? TrasportMeanName { get; set; }
    }

    public class EPhytoItem
    {
        [JsonProperty("sequence_no")]
        public int SequenceNo { get; set; }

        [JsonProperty("scient_name")]
        public string? ScientName { get; set; }

        [JsonProperty("descriptions")]
        public List<object>? Descriptions { get; set; } // Can be string or object

        [JsonProperty("common_names")]
        public List<object>? CommonNames { get; set; } // Can be string or object

        [JsonProperty("additional_notes")]
        public List<IncludeNote>? AdditionalNotes { get; set; }
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
    }
}
