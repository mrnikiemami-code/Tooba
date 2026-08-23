using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Identity.Domain;
using Tooba.Persistence;

namespace Tooba.Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>identity</c>. FK به Party/Catalog ندارد و mega-context نیست.
/// </summary>
public sealed class IdentityDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Identity در همان پایگاه Tenant یا Marketplace.
    /// </summary>
    public const string Schema = "identity";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// حساب‌های احراز هویت.
    /// </summary>
    public DbSet<UserAccount> Users => Set<UserAccount>();

    /// <summary>
    /// شناسه‌های ورود typed.
    /// </summary>
    public DbSet<LoginIdentifier> Identifiers => Set<LoginIdentifier>();

    /// <summary>
    /// اتصالات IdP خارجی.
    /// </summary>
    public DbSet<ExternalIdentityBinding> ExternalBindings => Set<ExternalIdentityBinding>();

    /// <summary>
    /// نام‌نویسی عامل MFA.
    /// </summary>
    public DbSet<MfaFactorEnrollment> MfaEnrollments => Set<MfaFactorEnrollment>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Ignore(x => x.DomainEvents);
            entity.HasMany(x => x.Identifiers)
                .WithOne()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.OwnsOne(x => x.Password, password =>
            {
                password.ToTable("password_credentials");
                password.Ignore(x => x.UserId);
                password.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
                password.Property(x => x.HasherFormatVersion).HasColumnName("hasher_format_version");
                password.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });
        });

        modelBuilder.Entity<LoginIdentifier>(entity =>
        {
            entity.ToTable("login_identifiers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.DisplayValue).HasMaxLength(320);
            entity.Property(x => x.NormalizedValue).HasMaxLength(320);
            entity.Property(x => x.VerificationState).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.Kind, x.NormalizedValue }).IsUnique();
        });

        modelBuilder.Entity<ExternalIdentityBinding>(entity =>
        {
            entity.ToTable("external_identity_bindings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Issuer).HasMaxLength(512);
            entity.Property(x => x.Subject).HasMaxLength(512);
            entity.HasIndex(x => new { x.Issuer, x.Subject }).IsUnique();
        });

        modelBuilder.Entity<MfaFactorEnrollment>(entity =>
        {
            entity.ToTable("mfa_factor_enrollments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.FactorKind).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.UserId, x.FactorKind }).IsUnique();
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Identity. Tenant را از Host نمی‌خواند.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            IdentityDbContext.Schema,
            typeof(IdentityDbContext));
        return new IdentityDbContext(options.Options);
    }
}
