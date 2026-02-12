using System;
using System.Collections.Generic;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TabMessageThphyto> TabMessageThphytos { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoIncludedClause> TabMessageThphytoIncludedClauses { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoIncludedNote> TabMessageThphytoIncludedNotes { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItem> TabMessageThphytoItems { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemAdditionalNote> TabMessageThphytoItemAdditionalNotes { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemAdditionalNoteContent> TabMessageThphytoItemAdditionalNoteContents { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemApplicableClassification> TabMessageThphytoItemApplicableClassifications { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemCommonName> TabMessageThphytoItemCommonNames { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemDescription> TabMessageThphytoItemDescriptions { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemIntended> TabMessageThphytoItemIntendeds { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemOriginCountry> TabMessageThphytoItemOriginCountries { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemPhysicalPackage> TabMessageThphytoItemPhysicalPackages { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemProcess> TabMessageThphytoItemProcesses { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemProcessCharacteristic> TabMessageThphytoItemProcessCharacteristics { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemTransport> TabMessageThphytoItemTransports { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoItemTransportEquipment> TabMessageThphytoItemTransportEquipments { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoMainCarriage> TabMessageThphytoMainCarriages { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoReferenceDoc> TabMessageThphytoReferenceDocs { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoResponse> TabMessageThphytoResponses { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoResponseReason> TabMessageThphytoResponseReasons { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoResponseRemark> TabMessageThphytoResponseRemarks { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoTransitCountry> TabMessageThphytoTransitCountries { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoUtilizeTransport> TabMessageThphytoUtilizeTransports { get; set; } = null!;

    public virtual DbSet<TabMessageThphytoXml> TabMessageThphytoXmls { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<TabMessageThphyto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto");

            entity.HasIndex(e => new { e.PhytoTo, e.TimeStamp, e.QueueStatus, e.ResponseStatus, e.AcfsQueueStatus, e.ImportCountryId, e.DocId }, "idx10_tab_message_thphyto");

            entity.HasIndex(e => new { e.PhytoTo, e.MarkSendAsw, e.MarkSendIppc, e.MarkSendAcfs, e.DocId, e.TimeStamp }, "idx1_tab_message_thphyto");

            entity.HasIndex(e => new { e.PhytoTo, e.MarkSendAsw, e.MarkSendIppc, e.ImportCountryId, e.TimeStamp }, "idx2_tab_message_thphyto");

            entity.HasIndex(e => e.NswMessageId, "idx3_tab_message_thphyto");

            entity.HasIndex(e => e.MessageId, "idx4_tab_message_thphyto");

            entity.HasIndex(e => new { e.PhytoTo, e.QueueStatus, e.AcfsQueueStatus, e.ImportCountryId, e.DocId, e.TimeStamp }, "idx5_tab_message_thphyto");

            entity.HasIndex(e => new { e.MarkSendAcfs, e.TimeStamp }, "idx6_tab_message_thphyto");

            entity.HasIndex(e => e.QueueId, "idx7_tab_message_thphyto");

            entity.HasIndex(e => e.AcfsQueueId, "idx8_tab_message_thphyto");

            entity.HasIndex(e => e.HubDeliveryNumber, "idx9_tab_message_thphyto");

            entity.HasIndex(e => new { e.DocType, e.DocStatus, e.DocId }, "uk1_tab_message_thphyto").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcfsQueueStatus).HasMaxLength(10);
            entity.Property(e => e.AuthAbbrevName).HasMaxLength(255);
            entity.Property(e => e.AuthActualDateTime).HasMaxLength(25);
            entity.Property(e => e.AuthAttainedQualificationName).HasMaxLength(255);
            entity.Property(e => e.AuthLocationId).HasMaxLength(5);
            entity.Property(e => e.AuthLocationName).HasMaxLength(256);
            entity.Property(e => e.AuthProviderId).HasMaxLength(35);
            entity.Property(e => e.AuthProviderName).HasMaxLength(200);
            entity.Property(e => e.AuthSpecifyPersonName).HasMaxLength(255);
            entity.Property(e => e.ConsigneeAddrLine1).HasMaxLength(200);
            entity.Property(e => e.ConsigneeAddrLine2).HasMaxLength(200);
            entity.Property(e => e.ConsigneeAddrLine3).HasMaxLength(200);
            entity.Property(e => e.ConsigneeAddrLine4).HasMaxLength(200);
            entity.Property(e => e.ConsigneeAddrLine5).HasMaxLength(200);
            entity.Property(e => e.ConsigneeAddressType).HasMaxLength(1);
            entity.Property(e => e.ConsigneeCityName).HasMaxLength(256);
            entity.Property(e => e.ConsigneeCountryId).HasMaxLength(2);
            entity.Property(e => e.ConsigneeCountryName).HasMaxLength(200);
            entity.Property(e => e.ConsigneeId).HasMaxLength(35);
            entity.Property(e => e.ConsigneeName).HasMaxLength(200);
            entity.Property(e => e.ConsigneePostcode).HasMaxLength(17);
            entity.Property(e => e.ConsignorAddrLine1).HasMaxLength(200);
            entity.Property(e => e.ConsignorAddrLine2).HasMaxLength(200);
            entity.Property(e => e.ConsignorAddrLine3).HasMaxLength(200);
            entity.Property(e => e.ConsignorAddrLine4).HasMaxLength(200);
            entity.Property(e => e.ConsignorAddrLine5).HasMaxLength(200);
            entity.Property(e => e.ConsignorCityName).HasMaxLength(256);
            entity.Property(e => e.ConsignorCounrtyName).HasMaxLength(100);
            entity.Property(e => e.ConsignorCountryId).HasMaxLength(2);
            entity.Property(e => e.ConsignorId).HasMaxLength(35);
            entity.Property(e => e.ConsignorName).HasMaxLength(200);
            entity.Property(e => e.ConsignorPostcode).HasMaxLength(17);
            entity.Property(e => e.ConsignorTypeCode).HasMaxLength(1);
            entity.Property(e => e.DocDescription).HasMaxLength(512);
            entity.Property(e => e.DocId).HasMaxLength(60);
            entity.Property(e => e.DocName).HasMaxLength(100);
            entity.Property(e => e.DocStatus).HasMaxLength(2);
            entity.Property(e => e.DocType).HasMaxLength(3);
            entity.Property(e => e.ExamEventOccrurLocationName).HasMaxLength(50);
            entity.Property(e => e.ExportCountryId).HasMaxLength(2);
            entity.Property(e => e.ExportCountryName).HasMaxLength(200);
            entity.Property(e => e.ExportSubordinaryHeirachiLevel)
                .HasMaxLength(3)
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.ExportSubordinaryId).HasMaxLength(9);
            entity.Property(e => e.ExportSubordinaryName)
                .HasMaxLength(200)
                .HasDefaultValueSql("'-'");
            entity.Property(e => e.HubDeliveryNumber).HasMaxLength(50);
            entity.Property(e => e.ImportCountryId).HasMaxLength(2);
            entity.Property(e => e.ImportCountryName).HasMaxLength(200);
            entity.Property(e => e.IssueDateTime)
                .HasComment("=Created At (UTC Time)")
                .HasColumnType("datetime");
            entity.Property(e => e.IssuerId).HasMaxLength(35);
            entity.Property(e => e.IssuerName).HasMaxLength(200);
            entity.Property(e => e.LastUpdate).HasColumnType("datetime");
            entity.Property(e => e.MarkSendAcfs)
                .HasComment("'Y'=send, 'N'=not send, 'F'=send failed, 'Q'= in queue outbound")
                .HasColumnType("enum('Y','N','F')")
                .HasColumnName("MarkSendACFS");
            entity.Property(e => e.MarkSendAsw)
                .HasDefaultValueSql("'N'")
                .HasComment("'Y'=send, 'N'=not send, 'F'=send failed, 'U'=unsend")
                .HasColumnType("enum('Y','N','F','U')")
                .HasColumnName("MarkSendASW");
            entity.Property(e => e.MarkSendIppc)
                .HasComment("'Y'=send, 'N'=not send, 'F'=send failed, 'U'=unsend")
                .HasColumnType("enum('Y','N','F','U')")
                .HasColumnName("MarkSendIPPC");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.MessageStatus).HasMaxLength(35);
            entity.Property(e => e.NswMessageId).HasMaxLength(200);
            entity.Property(e => e.PhytoTo)
                .HasMaxLength(20)
                .HasComment("ASW,IPPC,P2P");
            entity.Property(e => e.QueueStatus)
                .HasMaxLength(10)
                .HasComment("IN-QUEUE,FAIL,SUCCESS");
            entity.Property(e => e.ReexportCountryId)
                .HasMaxLength(2)
                .HasComment("ASW Only");
            entity.Property(e => e.ReexportCountryName).HasMaxLength(200);
            entity.Property(e => e.RequestDateTime).HasColumnType("datetime");
            entity.Property(e => e.ResponseAt).HasColumnType("datetime");
            entity.Property(e => e.ResponseStatus)
                .HasMaxLength(4)
                .HasComment("0101=New,0201=Receive Success,0501=Receive Fail");
            entity.Property(e => e.SendDateAcfs)
                .HasColumnType("datetime")
                .HasColumnName("SendDateACFS");
            entity.Property(e => e.SendDateAsw)
                .HasColumnType("datetime")
                .HasColumnName("SendDateASW");
            entity.Property(e => e.SendDateIppc)
                .HasColumnType("datetime")
                .HasColumnName("SendDateIPPC");
            entity.Property(e => e.TimeStamp)
                .HasComment("=Created At (TH Time)")
                .HasColumnType("datetime");
            entity.Property(e => e.UnloadingBasePortId).HasMaxLength(5);
            entity.Property(e => e.UnloadingBasePortName).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(20);
        });

        modelBuilder.Entity<TabMessageThphytoIncludedClause>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_included_clause");

            entity.HasIndex(e => new { e.MessageId, e.ClauseId }, "idx1_tab_message_thphyto_included_clause");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasMaxLength(670);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoIncludedNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_included_note");

            entity.HasIndex(e => new { e.MessageId, e.Subject }, "idx1_tab_message_thphyto_included_note");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.NoteId).HasMaxLength(50);
            entity.Property(e => e.Subject).HasMaxLength(10);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item");

            entity.HasIndex(e => e.MessageId, "idx1_tab_message_thphyto_item");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.SequenceNo }, "uk1_tab_message_thphyto_item").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.GrossVolume).HasColumnType("double(5,4)");
            entity.Property(e => e.GrossVolumeUnit).HasMaxLength(3);
            entity.Property(e => e.GrossWeight).HasPrecision(15, 4);
            entity.Property(e => e.GrossWeightUnit).HasMaxLength(3);
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.NetVolume).HasColumnType("double(5,4)");
            entity.Property(e => e.NetVolumeUnit).HasMaxLength(3);
            entity.Property(e => e.NetWeight).HasPrecision(15, 4);
            entity.Property(e => e.NetWeightUnit).HasMaxLength(3);
            entity.Property(e => e.ProductBatchId).HasMaxLength(17);
            entity.Property(e => e.ProductScientName).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemAdditionalNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_additional_note");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.AdditionalNoteId }, "idx1_tab_message_thphyto_item_additional_note");

            entity.HasIndex(e => new { e.ItemId, e.AdditionalNoteId }, "idx2_tab_message_thphyto_item_additional_note");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdditionalNoteId).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.Subject).HasMaxLength(8);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemAdditionalNoteContent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_additional_note_content");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.AdditionalNoteId }, "idx1_tab_message_thphyto_item_additional_note_content");

            entity.HasIndex(e => new { e.ItemId, e.AdditionalNoteId }, "idx2_tab_message_thphyto_item_additional_note_content");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdditionalNoteId).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.NoteContent).HasColumnType("text");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemApplicableClassification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_applicable_classification");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.ApplicableId }, "idx1_tab_message_thphyto_item_applicable_classification");

            entity.HasIndex(e => new { e.ItemId, e.ApplicableId }, "idx2_tab_message_thphyto_item_applicable_classification");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApplicableId).HasMaxLength(50);
            entity.Property(e => e.ClassCode).HasMaxLength(10);
            entity.Property(e => e.ClassName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.SystemName).HasMaxLength(10);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemCommonName>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_common_name");

            entity.HasIndex(e => new { e.MessageId, e.ItemId }, "idx1_tab_message_thphyto_item_common_name");

            entity.HasIndex(e => e.ItemId, "idx2_tab_message_thphyto_item_common_name");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.ProudctCommonName)
                .HasMaxLength(350)
                .HasComment("35*9");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemDescription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_description");

            entity.HasIndex(e => new { e.MessageId, e.ItemId }, "idx1_tab_message_thphyto_item_description");

            entity.HasIndex(e => e.ItemId, "idx2_tab_message_thphyto_item_description");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.ProductDescription)
                .HasMaxLength(2304)
                .HasComment("256*9");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemIntended>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_intended");

            entity.HasIndex(e => new { e.MessageId, e.ItemId }, "idx1_tab_message_thphyto_item_intended");

            entity.HasIndex(e => e.ItemId, "idx2_tab_message_thphyto_item_intended");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.ProductIntendUse).HasMaxLength(35);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemOriginCountry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_origin_country");

            entity.HasIndex(e => new { e.MessageId, e.ItemId }, "idx1_tab_message_thphyto_item_origin_country");

            entity.HasIndex(e => e.ItemId, "idx2_tab_message_thphyto_item_origin_country");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthorizePartyId).HasMaxLength(35);
            entity.Property(e => e.AuthorizePartyName).HasMaxLength(35);
            entity.Property(e => e.AuthorizeRoleCode).HasMaxLength(3);
            entity.Property(e => e.CountryId).HasMaxLength(2);
            entity.Property(e => e.CountryName).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.HeirachiLevel)
                .HasMaxLength(3)
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.SpecifyAddrPostCode).HasMaxLength(10);
            entity.Property(e => e.SubDivisionId).HasMaxLength(9);
            entity.Property(e => e.SubDivisionName)
                .HasMaxLength(70)
                .HasDefaultValueSql("'-'");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemPhysicalPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_physical_package");

            entity.HasIndex(e => new { e.MessageId, e.ItemId }, "idx1_tab_message_thphyto_item_physical_package");

            entity.HasIndex(e => e.ItemId, "idx2_tab_message_thphyto_item_physical_package");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.LevelCode).HasMaxLength(1);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.ShippingMarks)
                .HasMaxLength(4608)
                .HasComment("512*9");
            entity.Property(e => e.TypeCode).HasMaxLength(3);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemProcess>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_process");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.ProcessId }, "idx1_tab_message_thphyto_item_process");

            entity.HasIndex(e => new { e.ItemId, e.ProcessId }, "idx2_tab_message_thphyto_item_process");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Duration).HasColumnType("double(14,4)");
            entity.Property(e => e.DurationUnit).HasMaxLength(3);
            entity.Property(e => e.EndDate).HasMaxLength(27);
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.ProcessId).HasMaxLength(50);
            entity.Property(e => e.StartDate).HasMaxLength(27);
            entity.Property(e => e.TypeCode).HasMaxLength(3);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemProcessCharacteristic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_process_characteristic");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.ProcessId }, "idx1_tab_message_thphyto_item_process_characteristic");

            entity.HasIndex(e => new { e.ItemId, e.ProcessId }, "idx2_tab_message_thphyto_item_process_characteristic");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Description1).HasMaxLength(4);
            entity.Property(e => e.Description2).HasColumnType("text");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.ProcessId).HasMaxLength(50);
            entity.Property(e => e.TypeCode).HasMaxLength(3);
            entity.Property(e => e.UnitCode).HasMaxLength(3);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.ValueMeasure).HasMaxLength(20);
        });

        modelBuilder.Entity<TabMessageThphytoItemTransport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_transport");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.TransportId }, "idx1_tab_message_thphyto_item_transport");

            entity.HasIndex(e => new { e.ItemId, e.TransportId }, "idx2_tab_message_thphyto_item_transport");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.SealNumber).HasMaxLength(100);
            entity.Property(e => e.TransportId).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoItemTransportEquipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_item_transport_equipment");

            entity.HasIndex(e => new { e.MessageId, e.ItemId, e.TransportId }, "idx1_tab_message_thphyto_item_transport_equipment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasMaxLength(50);
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.SealNumber).HasMaxLength(50);
            entity.Property(e => e.TransportId).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoMainCarriage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_main_carriage");

            entity.HasIndex(e => e.MessageId, "idx1_tab_message_thphyto_main_carriage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.MovementId)
                .HasMaxLength(255)
                .HasComment("MainCarriageSPSTransportMovement.ID");
            entity.Property(e => e.TransportMeanName).HasMaxLength(255);
            entity.Property(e => e.TransportModeCode)
                .HasMaxLength(2)
                .HasComment("MainCarriageSPSTransportMovement.ModeCode");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoReferenceDoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_reference_doc");

            entity.HasIndex(e => e.MessageId, "idx1_tab_message_thphyto_reference_doc");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.DocId)
                .HasMaxLength(60)
                .HasComment("tab_message_thphyto.DocID");
            entity.Property(e => e.Filename).HasMaxLength(255);
            entity.Property(e => e.Information).HasMaxLength(512);
            entity.Property(e => e.IssueDate).HasColumnType("datetime");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.RefDocId)
                .HasMaxLength(50)
                .HasComment("ID of Reference to pdf object");
            entity.Property(e => e.RelationTypeCode).HasMaxLength(3);
            entity.Property(e => e.TypeCode).HasMaxLength(3);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_response");

            entity.HasIndex(e => e.ReferenceDocumentId, "IDX1_TAB_MESSAGE_THPHYTO_RESPONSE");

            entity.HasIndex(e => e.DocId, "UK1_TAB_MESSAGE_THPHYTO_RESPONSE");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryCode).HasMaxLength(3);
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DocCode).HasMaxLength(3);
            entity.Property(e => e.DocId).HasMaxLength(35);
            entity.Property(e => e.DocName).HasMaxLength(35);
            entity.Property(e => e.FlagUpdate)
                .HasMaxLength(10)
                .HasColumnName("flag_update");
            entity.Property(e => e.HeaderMessageId)
                .HasMaxLength(255)
                .HasColumnName("header_message_id");
            entity.Property(e => e.HeaderRefMessageId)
                .HasMaxLength(255)
                .HasColumnName("header_ref_message_id");
            entity.Property(e => e.HeaderTimeStamp)
                .HasColumnType("datetime")
                .HasColumnName("header_time_stamp");
            entity.Property(e => e.InboundAt)
                .HasColumnType("datetime")
                .HasColumnName("inbound_at");
            entity.Property(e => e.IssueCountryId).HasMaxLength(2);
            entity.Property(e => e.IssueCountryName).HasMaxLength(35);
            entity.Property(e => e.RecipientPartyId).HasMaxLength(35);
            entity.Property(e => e.ReferenceDocumentCode).HasMaxLength(3);
            entity.Property(e => e.ReferenceDocumentDate).HasColumnType("datetime");
            entity.Property(e => e.ReferenceDocumentId)
                .HasMaxLength(35)
                .HasColumnName("ReferenceDocumentID");
            entity.Property(e => e.SenderPartyId).HasMaxLength(35);
            entity.Property(e => e.SubmissionDate).HasColumnType("datetime");
            entity.Property(e => e.SystemTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("system_time");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<TabMessageThphytoResponseReason>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_response_reason");

            entity.HasIndex(e => e.ResponseId, "idx1_tab_message_thphyto_response_reason");

            entity.HasIndex(e => e.DocId, "idx2_tab_message_thphyto_response_reason");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DocId).HasMaxLength(35);
            entity.Property(e => e.ReasonCode).HasMaxLength(3);
            entity.Property(e => e.SystemTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("system_time");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<TabMessageThphytoResponseRemark>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_response_remark");

            entity.HasIndex(e => e.ResponseId, "idx1_tab_message_thphyto_response_remark");

            entity.HasIndex(e => e.DocId, "idx2_tab_message_thphyto_response_remark");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DocId).HasMaxLength(35);
            entity.Property(e => e.Remark).HasMaxLength(512);
            entity.Property(e => e.SystemTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("system_time");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<TabMessageThphytoTransitCountry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_transit_country");

            entity.HasIndex(e => new { e.MessageId, e.CountryId }, "idx1_tab_message_thphyto_transit_country");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CountryId).HasMaxLength(2);
            entity.Property(e => e.CountryName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoUtilizeTransport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_utilize_transport");

            entity.HasIndex(e => e.MessageId, "idx1_tab_message_thphyto_utilize_transport");

            entity.Property(e => e.Id)
                .HasComment("ID Records")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.EquipmentId)
                .HasMaxLength(100)
                .HasComment("UtilizedSPSTransportEquipment.ID");
            entity.Property(e => e.MessageId)
                .HasMaxLength(50)
                .HasComment("ID of THPHYTO");
            entity.Property(e => e.SealNumber)
                .HasMaxLength(255)
                .HasComment("AffixedSPSSeal.ID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TabMessageThphytoXml>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tab_message_thphyto_xml");

            entity.HasIndex(e => e.MessageId, "idx1_tab_message_thphyto_xml");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.MessageId).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
