using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Core.Entities;
using ShopFloorTracker.Core.Enums;

namespace ShopFloorTracker.Infrastructure.Data;

public class ShopFloorDbContext : DbContext
{
    public ShopFloorDbContext(DbContextOptions<ShopFloorDbContext> options) : base(options)
    {
    }

    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Subassembly> Subassemblies { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<Hardware> Hardware { get; set; }
    public DbSet<DetachedProduct> DetachedProducts { get; set; }
    public DbSet<StorageRack> StorageRacks { get; set; }
    public DbSet<ScanActivity> ScanActivities { get; set; }
    public DbSet<PlacedSheet> PlacedSheets { get; set; }
    public DbSet<PartPlacement> PartPlacements { get; set; }
    public DbSet<ImportHistory> ImportHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WorkOrder entity
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.WorkOrderId);
            entity.Property(e => e.WorkOrderId).HasMaxLength(50);
            entity.Property(e => e.WorkOrderNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.MicrovellumLinkID).HasMaxLength(50);
        });

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductId).HasMaxLength(50);
            entity.Property(e => e.WorkOrderId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProductNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductType).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.MicrovellumLinkID).HasMaxLength(50);

            entity.HasOne(d => d.WorkOrder)
                  .WithMany(p => p.Products)
                  .HasForeignKey(d => d.WorkOrderId);
        });

        // Configure Part entity
        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartId);
            entity.Property(e => e.PartId).HasMaxLength(50);
            entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SubassemblyId).HasMaxLength(50);
            entity.Property(e => e.PartNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PartName).HasMaxLength(200);
            entity.Property(e => e.Material).HasMaxLength(100);
            entity.Property(e => e.MaterialName).HasMaxLength(100);
            entity.Property(e => e.MaterialCode).HasMaxLength(50);
            entity.Property(e => e.EdgeBanding).HasMaxLength(200);
            entity.Property(e => e.NestingSheet).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.MicrovellumLinkID).HasMaxLength(50);

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.Parts)
                  .HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.Subassembly)
                  .WithMany(p => p.Parts)
                  .HasForeignKey(d => d.SubassemblyId);

            entity.HasOne(d => d.StorageRack)
                  .WithMany(p => p.Parts)
                  .HasForeignKey(d => d.StorageRackId);
        });

        // Configure Subassembly entity
        modelBuilder.Entity<Subassembly>(entity =>
        {
            entity.HasKey(e => e.SubassemblyId);
            entity.Property(e => e.SubassemblyId).HasMaxLength(50);
            entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SubassemblyNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SubassemblyName).HasMaxLength(200);
            entity.Property(e => e.SubassemblyType).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.Subassemblies)
                  .HasForeignKey(d => d.ProductId);
        });

        // Configure Hardware entity
        modelBuilder.Entity<Hardware>(entity =>
        {
            entity.HasKey(e => e.HardwareId);
            entity.Property(e => e.HardwareId).HasMaxLength(50);
            entity.Property(e => e.HardwareName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.HardwareDescription).HasMaxLength(500);
            entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.WorkOrderId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.MicrovellumLinkID).HasMaxLength(50);

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.Hardware)
                  .HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.WorkOrder)
                  .WithMany(p => p.Hardware)
                  .HasForeignKey(d => d.WorkOrderId);
        });

        // Configure DetachedProduct entity
        modelBuilder.Entity<DetachedProduct>(entity =>
        {
            entity.HasKey(e => e.DetachedProductId);
            entity.Property(e => e.DetachedProductId).HasMaxLength(50);
            entity.Property(e => e.WorkOrderId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProductNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(d => d.WorkOrder)
                  .WithMany(p => p.DetachedProducts)
                  .HasForeignKey(d => d.WorkOrderId);
        });

        // Configure StorageRack entity
        modelBuilder.Entity<StorageRack>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Rows).IsRequired();
            entity.Property(e => e.Columns).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // Configure ScanActivity entity
        modelBuilder.Entity<ScanActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PartId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.StationName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Activity).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OldStatus).HasConversion<string>();
            entity.Property(e => e.NewStatus).HasConversion<string>();
            entity.Property(e => e.StorageLocation).HasMaxLength(50);
            entity.Property(e => e.OperatorId).HasMaxLength(50);

            entity.HasOne(d => d.Part)
                  .WithMany(p => p.ScanActivities)
                  .HasForeignKey(d => d.PartId);
        });

        // Configure PlacedSheet entity
        modelBuilder.Entity<PlacedSheet>(entity =>
        {
            entity.HasKey(e => e.PlacedSheetId);
            entity.Property(e => e.PlacedSheetId).HasMaxLength(50);
            entity.Property(e => e.SheetName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BarCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.WorkOrderId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MaterialType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.MicrovellumLinkID).HasMaxLength(50);

            entity.HasOne(d => d.WorkOrder)
                  .WithMany(p => p.PlacedSheets)
                  .HasForeignKey(d => d.WorkOrderId);
        });

        // Configure PartPlacement entity
        modelBuilder.Entity<PartPlacement>(entity =>
        {
            entity.HasKey(e => e.PlacementId);
            entity.Property(e => e.PartId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PlacedSheetId).HasMaxLength(50).IsRequired();

            entity.HasOne(d => d.Part)
                  .WithMany(p => p.PartPlacements)
                  .HasForeignKey(d => d.PartId);

            entity.HasOne(d => d.PlacedSheet)
                  .WithMany(p => p.PartPlacements)
                  .HasForeignKey(d => d.PlacedSheetId);
        });

        // Configure ImportHistory entity
        modelBuilder.Entity<ImportHistory>(entity =>
        {
            entity.HasKey(e => e.ImportId);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.ImportedBy).HasMaxLength(100);
            entity.Property(e => e.WorkOrderId).HasMaxLength(50);

            entity.HasOne(d => d.WorkOrder)
                  .WithMany()
                  .HasForeignKey(d => d.WorkOrderId);
        });
    }
}