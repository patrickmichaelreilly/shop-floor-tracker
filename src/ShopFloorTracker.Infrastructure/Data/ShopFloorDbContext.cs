using Microsoft.EntityFrameworkCore;
using ShopFloorTracker.Core.Entities;

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
            entity.Property(e => e.Status).HasMaxLength(20);

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
            entity.Property(e => e.EdgeBanding).HasMaxLength(200);
            entity.Property(e => e.NestingSheet).HasMaxLength(100);

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.Parts)
                  .HasForeignKey(d => d.ProductId);

            entity.HasOne(d => d.Subassembly)
                  .WithMany(p => p.Parts)
                  .HasForeignKey(d => d.SubassemblyId);
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
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Product)
                  .WithMany(p => p.Subassemblies)
                  .HasForeignKey(d => d.ProductId);
        });

        // Configure Hardware entity
        modelBuilder.Entity<Hardware>(entity =>
        {
            entity.HasKey(e => e.HardwareId);
            entity.Property(e => e.HardwareId).HasMaxLength(50);
            entity.Property(e => e.WorkOrderId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.HardwareNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.HardwareName).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20);

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
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.WorkOrder)
                  .WithMany(p => p.DetachedProducts)
                  .HasForeignKey(d => d.WorkOrderId);
        });
    }
}