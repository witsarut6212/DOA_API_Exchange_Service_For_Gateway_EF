using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities
{
    public class EPhytoDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string DocId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string DocName { get; set; } = string.Empty;

        public string? DocDescription { get; set; }

        [Required]
        [StringLength(50)]
        public string DocType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string StatusCode { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        public string? IssuePartyId { get; set; }

        [Required]
        public string IssuePartyName { get; set; } = string.Empty;

        // Relationships
        public virtual List<DocumentNote> Notes { get; set; } = new();
        public virtual List<ReferenceDoc> ReferenceDocs { get; set; } = new();
        public virtual SignatoryAuthen? SignatoryAuthen { get; set; }
        public virtual Consignment? Consignment { get; set; }
        public virtual List<EPhytoItem> Items { get; set; } = new();
    }

    public class DocumentNote
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoDocumentId { get; set; }
        
        [Required]
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ReferenceDoc
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoDocumentId { get; set; }
        public DateTime? IssueDate { get; set; }
        public string? TypeCode { get; set; }
        public string RelationTypeCode { get; set; } = string.Empty;
        public string DocId { get; set; } = string.Empty;
        public string? Filename { get; set; }
        public string? Information { get; set; }
        public string? PdfObject { get; set; } // Base64
    }

    public class SignatoryAuthen
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoDocumentId { get; set; }
        public string? ActualDateTime { get; set; }
        public string? LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string? SignatoryPersonName { get; set; }
        public string? QualificationName { get; set; }
        public string? QualificationAbbrev { get; set; }
        
        public virtual List<SignatoryClause> Clauses { get; set; } = new();
    }

    public class SignatoryClause
    {
        [Key]
        public int Id { get; set; }
        public int SignatoryAuthenId { get; set; }
        public string ClauseId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
