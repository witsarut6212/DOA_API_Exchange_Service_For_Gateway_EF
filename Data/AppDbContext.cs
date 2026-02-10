using Microsoft.EntityFrameworkCore;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;

namespace DOA_API_Exchange_Service_For_Gateway.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<EPhytoDocument> EPhytoDocuments { get; set; }
        public DbSet<DocumentNote> DocumentNotes { get; set; }
        public DbSet<ReferenceDoc> ReferenceDocs { get; set; }
        public DbSet<SignatoryAuthen> SignatoryAuthens { get; set; }
        public DbSet<SignatoryClause> SignatoryClauses { get; set; }
        public DbSet<Consignment> Consignments { get; set; }
        public DbSet<TransitCountry> TransitCountries { get; set; }
        public DbSet<MainCarriage> MainCarriages { get; set; }
        public DbSet<EPhytoItem> EPhytoItems { get; set; }
        public DbSet<ItemDescription> ItemDescriptions { get; set; }
        public DbSet<ItemPackage> ItemPackages { get; set; }
        public DbSet<ItemProcess> ItemProcesses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships if needed (EF usually figures them out by convention)
            modelBuilder.Entity<EPhytoDocument>()
                .HasMany(d => d.Items)
                .WithOne()
                .HasForeignKey(i => i.EPhytoDocumentId);

            modelBuilder.Entity<Consignment>()
                .HasOne<EPhytoDocument>()
                .WithOne(d => d.Consignment)
                .HasForeignKey<Consignment>(c => c.EPhytoDocumentId);
        }
    }
}
