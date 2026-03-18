using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities
{
    [Table("authors")]
    public class Author
    {
        [Key]
        public int Id { get; set; }

        [Column("nswrid_dev")]
        public string? NswridDev { get; set; }

        [Column("nswrid_pro")]
        public string? NswridPro { get; set; }
    }
}
