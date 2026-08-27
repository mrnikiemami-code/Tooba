using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Party.Domain;
using Tooba.Persistence;

namespace Tooba.Party.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>party</c>. FK به Identity ندارد و mega-context نیست.
/// دادهٔ Marketplace در پایگاه marketplace و دادهٔ Single-Store در پایگاه همان Tenant است.
/// </summary>
public sealed class PartyDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Party در همان پایگاه Tenant یا Marketplace.
    /// </summary>
    public const string Schema = "party";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public PartyDbContext(DbContextOptions<PartyDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// ریشه‌های شخص/سازمان.
    /// </summary>
    public DbSet<BusinessParty> Parties => Set<BusinessParty>();

    /// <summary>
    /// قابلیت‌های تجاری گسترش‌پذیر سازمان.
    /// </summary>
    public DbSet<PartyCapability> Capabilities => Set<PartyCapability>();

    /// <summary>
    /// پیوند UserId مبهم به Party.
    /// </summary>
    public DbSet<UserPartyLink> UserLinks => Set<UserPartyLink>();

    /// <summary>
    /// عضویت‌ها. مجوز نهایی نیستند.
    /// </summary>
    public DbSet<PartyMembership> Memberships => Set<PartyMembership>();

    /// <summary>
    /// روابط سازمان‌به‌سازمان.
    /// </summary>
    public DbSet<OrganizationRelationship> OrganizationRelationships => Set<OrganizationRelationship>();

    /// <summary>
    /// Outbox همین ماژول برای تصویرسازی بعدی SpiceDB.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<BusinessParty>(entity =>
        {
            entity.ToTable("parties");
            entity.HasKey(x => x.PartyId);
            entity.Property(x => x.PartyId).ValueGeneratedNever();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(BusinessParty.DescriptionMaxLength);
            entity.Property(x => x.SupportPhone).HasMaxLength(BusinessParty.SupportPhoneMaxLength);
            entity.Property(x => x.SupportEmail).HasMaxLength(BusinessParty.SupportEmailMaxLength);
            entity.Property(x => x.AddressLine).HasMaxLength(BusinessParty.AddressLineMaxLength);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Ignore(x => x.DomainEvents);
            entity.HasMany(x => x.Capabilities)
                .WithOne()
                .HasForeignKey(x => x.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartyCapability>(entity =>
        {
            entity.ToTable("party_capabilities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.CapabilityCode).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.PartyId, x.CapabilityCode }).IsUnique();
        });

        modelBuilder.Entity<UserPartyLink>(entity =>
        {
            entity.ToTable("user_party_links");
            entity.HasKey(x => x.LinkId);
            entity.Property(x => x.LinkId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.UserId, x.PartyId }).IsUnique();
        });

        modelBuilder.Entity<PartyMembership>(entity =>
        {
            entity.ToTable("memberships");
            entity.HasKey(x => x.MembershipId);
            entity.Property(x => x.MembershipId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.RelationCode).HasMaxLength(64).IsRequired();
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => new { x.UserId, x.PartyId, x.RelationCode }).IsUnique();
        });

        modelBuilder.Entity<OrganizationRelationship>(entity =>
        {
            entity.ToTable("organization_relationships");
            entity.HasKey(x => x.RelationshipId);
            entity.Property(x => x.RelationshipId).ValueGeneratedNever();
            entity.Property(x => x.RelationCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.FromPartyId, x.ToPartyId, x.RelationCode }).IsUnique();
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Party. Tenant را از Host نمی‌خواند.
/// </summary>
public sealed class PartyDbContextFactory : IDesignTimeDbContextFactory<PartyDbContext>
{
    /// <inheritdoc />
    public PartyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PartyDbContext.Schema,
            typeof(PartyDbContext));
        return new PartyDbContext(options.Options);
    }
}
