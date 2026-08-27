using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Support.Domain;

namespace Tooba.Support.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل support.</summary>
public sealed class SupportDbContext : DbContext
{
    /// <summary>schema اختصاصی Support.</summary>
    public const string Schema = "support";

    /// <summary>DbContext را می‌سازد.</summary>
    public SupportDbContext(DbContextOptions<SupportDbContext> options) : base(options)
    {
    }

    /// <summary>تیکت‌ها.</summary>
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();

    /// <summary>پیام‌ها.</summary>
    public DbSet<TicketMessage> Messages => Set<TicketMessage>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("support_tickets");
            entity.HasKey(x => x.TicketId);
            entity.Property(x => x.TicketId).ValueGeneratedNever();
            entity.Property(x => x.Subject).HasMaxLength(SupportTicket.SubjectMaxLength);
            entity.Property(x => x.RequesterKind).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.RelatedEntityType).HasMaxLength(SupportTicket.RelatedEntityTypeMaxLength);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(SupportTicket.IdempotencyKeyMaxLength);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(x => new { x.RequesterActorUserId, x.CreatedAt });
            entity.HasIndex(x => new { x.SellerPartyId, x.CreatedAt });
            entity.HasIndex(x => new { x.Status, x.UpdatedAt });
        });
        modelBuilder.Entity<TicketMessage>(entity =>
        {
            entity.ToTable("ticket_messages");
            entity.HasKey(x => x.MessageId);
            entity.Property(x => x.MessageId).ValueGeneratedNever();
            entity.Property(x => x.AuthorKind).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Body).HasMaxLength(TicketMessage.BodyMaxLength);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(TicketMessage.IdempotencyKeyMaxLength);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(x => new { x.TicketId, x.CreatedAt });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت Support.</summary>
public sealed class SupportDbContextFactory : IDesignTimeDbContextFactory<SupportDbContext>
{
    /// <inheritdoc />
    public SupportDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), SupportDbContext.Schema, typeof(SupportDbContext));
        return new SupportDbContext(options.Options);
    }
}
