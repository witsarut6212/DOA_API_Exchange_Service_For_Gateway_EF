using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities
{
    public class Consignment
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoDocumentId { get; set; }

        // Consignor (Exporter)
        public string? ConsignorId { get; set; }
        public string ConsignorName { get; set; } = string.Empty;
        public string? ConsignorPostcode { get; set; }
        public string ConsignorAddressLine1 { get; set; } = string.Empty;
        public string? ConsignorAddressLine2 { get; set; }
        public string? ConsignorAddressLine3 { get; set; }
        public string? ConsignorCity { get; set; }
        public string? ConsignorCountryId { get; set; }

        // Consignee
        public string? ConsigneeId { get; set; }
        public string ConsigneeName { get; set; } = string.Empty;
        public string? ConsigneePostcode { get; set; }
        public string ConsigneeAddressLine1 { get; set; } = string.Empty;
        public string? ConsigneeAddressLine2 { get; set; }
        public string? ConsigneeCountryId { get; set; }

        // Countries
        public string ExportCountryId { get; set; } = string.Empty;
        public string ExportCountryName { get; set; } = string.Empty;
        public string? ImportCountryId { get; set; }
        public string? ImportCountryName { get; set; }
        
        public string? UnloadingPortId { get; set; }
        public string? UnloadingPortName { get; set; }
        public string? ExaminationLocation { get; set; }

        public virtual List<TransitCountry> TransitCountries { get; set; } = new();
        public virtual List<MainCarriage> MainCarriages { get; set; } = new();
    }

    public class TransitCountry
    {
        [Key]
        public int Id { get; set; }
        public int ConsignmentId { get; set; }
        public string CountryId { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
    }

    public class MainCarriage
    {
        [Key]
        public int Id { get; set; }
        public int ConsignmentId { get; set; }
        public string? VesselId { get; set; }
        public string ModeCode { get; set; } = string.Empty;
        public string? VesselName { get; set; }
    }

    public class EPhytoItem
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoDocumentId { get; set; }
        public string? SequenceNo { get; set; }
        public string? ScientificName { get; set; }
        
        public decimal? NetWeight { get; set; }
        public string? NetWeightUnit { get; set; }
        public decimal? GrossWeight { get; set; }
        public string? GrossWeightUnit { get; set; }

        public virtual List<ItemDescription> Descriptions { get; set; } = new();
        public virtual List<ItemPackage> Packages { get; set; } = new();
        public virtual List<ItemOriginCountry> OriginCountries { get; set; } = new();
        public virtual List<ItemProcess> Processes { get; set; } = new();
    }

    public class ItemDescription
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoItemId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ItemPackage
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoItemId { get; set; }
        public string LevelCode { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class ItemProcess
    {
        [Key]
        public int Id { get; set; }
        public int EPhytoItemId { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Duration { get; set; }
        public string? DurationUnit { get; set; }
    }
}
