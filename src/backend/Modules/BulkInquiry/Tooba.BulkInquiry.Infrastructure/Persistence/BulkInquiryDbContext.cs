using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.BulkInquiry.Domain;
using Tooba.Persistence;

namespace Tooba.BulkInquiry.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل bulk_inquiry.</summary>
public sealed class BulkInquiryDbContext : DbContext
{
    /// <summary>schema اختصاصی BulkInquiry.</summary>
    public const string Schema = "bulk_inquiry";

    /// <summary>DbContext را می‌سازد.</summary>
    public BulkInquiryDbContext(DbContextOptions<BulkInquiryDbContext> options) : base(options) { }

    /// <summary>درخواست‌های خرید عمده.</summary>
    public DbSet<BulkPurchaseInquiry> Inquiries => Set<BulkPurchaseInquiry>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<BulkPurchaseInquiry>(entity =>
        {
            entity.ToTable("bulk_purchase_inquiries");
            entity.HasKey(x => x.InquiryId);
            entity.Property(x => x.InquiryId).ValueGeneratedNever();
            entity.Property(x => x.FullName).HasMaxLength(BulkPurchaseInquiry.FullNameMaxLength);
            entity.Property(x => x.Phone).HasMaxLength(BulkPurchaseInquiry.PhoneLength);
            entity.Property(x => x.Email).HasMaxLength(BulkPurchaseInquiry.EmailMaxLength);
            entity.Property(x => x.CompanyName).HasMaxLength(BulkPurchaseInquiry.CompanyNameMaxLength);
            entity.Property(x => x.Address).HasMaxLength(BulkPurchaseInquiry.AddressMaxLength);
            entity.Property(x => x.Notes).HasMaxLength(BulkPurchaseInquiry.NotesMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.ProductId, x.CreatedAt });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت BulkInquiry.</summary>
public sealed class BulkInquiryDbContextFactory : IDesignTimeDbContextFactory<BulkInquiryDbContext>
{
    /// <inheritdoc />
    public BulkInquiryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BulkInquiryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), BulkInquiryDbContext.Schema, typeof(BulkInquiryDbContext));
        return new BulkInquiryDbContext(options.Options);
    }
}
