using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Geography.Domain.Aggregates;
using Tooba.Persistence;

namespace Tooba.Geography.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema مستقل <c>geography</c>: فقط هویت مکان و نام‌های محلی‌شدهٔ آن.
/// آدرس مشتری، سرویس‌پذیری، پوشش و نرخ ارسال در این schema نگه‌داری نمی‌شود.
/// </summary>
/// <remarks>
/// این کلاس در این Task به Host وصل نمی‌شود (بدون DI، بدون endpoint) و هیچ مهاجرتی تولید یا اجرا
/// نشده است؛ مدل صرفاً inert و آمادهٔ بازبینی معماری است.
/// </remarks>
public sealed class GeographyDbContext : DbContext
{
    /// <summary>schema اختصاصی Geography.</summary>
    public const string Schema = "geography";

    /// <summary>DbContext را با گزینه‌های میزبان می‌سازد.</summary>
    /// <param name="options">گزینه‌های EF.</param>
    public GeographyDbContext(DbContextOptions<GeographyDbContext> options) : base(options)
    {
    }

    /// <summary>کشورها.</summary>
    public DbSet<Country> Countries => Set<Country>();

    /// <summary>ناحیه‌های اداری با سلسله‌مراتب داده‌محور.</summary>
    public DbSet<AdministrativeArea> AdministrativeAreas => Set<AdministrativeArea>();

    /// <summary>شهرها.</summary>
    public DbSet<City> Cities => Set<City>();

    /// <summary>ناحیه‌های خدمت زیر شهر.</summary>
    public DbSet<ServiceArea> ServiceAreas => Set<ServiceArea>();

    /// <summary>سطرهای نام محلی‌شدهٔ کشورها؛ کلید یکتایی (کشور، زبان) و کلید خارجی واقعی.</summary>
    public DbSet<CountryNameTranslation> CountryNameTranslations => Set<CountryNameTranslation>();

    /// <summary>سطرهای نام محلی‌شدهٔ ناحیه‌های اداری؛ کلید یکتایی (ناحیه، زبان) و کلید خارجی واقعی.</summary>
    public DbSet<AdministrativeAreaNameTranslation> AdministrativeAreaNameTranslations =>
        Set<AdministrativeAreaNameTranslation>();

    /// <summary>سطرهای نام محلی‌شدهٔ شهرها؛ کلید یکتایی (شهر، زبان) و کلید خارجی واقعی.</summary>
    public DbSet<CityNameTranslation> CityNameTranslations => Set<CityNameTranslation>();

    /// <summary>سطرهای نام محلی‌شدهٔ ناحیه‌های خدمت؛ کلید یکتایی (ناحیهٔ خدمت، زبان) و کلید خارجی واقعی.</summary>
    public DbSet<ServiceAreaNameTranslation> ServiceAreaNameTranslations =>
        Set<ServiceAreaNameTranslation>();

    /// <summary>Outbox همین ماژول؛ پیروی از اسکلت متعارف DbContext ماژول‌ها، بدون هیچ مصرف فعلی.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // ---- Country -------------------------------------------------------
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("countries");
            entity.HasKey(x => x.CountryId);
            entity.Property(x => x.CountryId).ValueGeneratedNever();
            entity.Property(x => x.CountryCode)
                .HasMaxLength(Country.CountryCodeMaxLength)
                .IsRequired();
            entity.HasIndex(x => x.CountryCode).IsUnique();
            entity.HasIndex(x => x.IsActive);
        });

        // ---- AdministrativeArea (self-referencing, data-driven depth) ------
        modelBuilder.Entity<AdministrativeArea>(entity =>
        {
            entity.ToTable("administrative_areas");
            entity.HasKey(x => x.AdministrativeAreaId);
            entity.Property(x => x.AdministrativeAreaId).ValueGeneratedNever();
            entity.Property(x => x.Code)
                .HasMaxLength(AdministrativeArea.CodeMaxLength)
                .IsRequired();

            // Scoped code: a code is unique inside its country. Codes are admin-assigned,
            // case-sensitive-stable identifiers; equal display names never collide because
            // names live only in administrative_area_name_translations.
            entity.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.CountryId, x.IsActive });

            entity.HasOne<Country>()
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self hierarchy: reference-locked (never cascade-delete a subtree of reference data).
            entity.HasOne<AdministrativeArea>()
                .WithMany()
                .HasForeignKey(x => x.ParentAdministrativeAreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- City ----------------------------------------------------------
        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("cities");
            entity.HasKey(x => x.CityId);
            entity.Property(x => x.CityId).ValueGeneratedNever();
            entity.Property(x => x.Code)
                .HasMaxLength(City.CodeMaxLength)
                .IsRequired();

            entity.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.CountryId, x.AdministrativeAreaId, x.IsActive });

            entity.HasOne<Country>()
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            // AdministrativeAreaId is optional: a city may exist without an owning area.
            entity.HasOne<AdministrativeArea>()
                .WithMany()
                .HasForeignKey(x => x.AdministrativeAreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ServiceArea ---------------------------------------------------
        modelBuilder.Entity<ServiceArea>(entity =>
        {
            entity.ToTable("service_areas");
            entity.HasKey(x => x.ServiceAreaId);
            entity.Property(x => x.ServiceAreaId).ValueGeneratedNever();
            entity.Property(x => x.Code)
                .HasMaxLength(ServiceArea.CodeMaxLength)
                .IsRequired();

            entity.HasIndex(x => new { x.CityId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.CityId, x.IsActive });

            entity.HasOne<City>()
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Per-kind localized names ---------------------------------------
        // Each place kind owns its own translation table with a REAL parent foreign key, so a
        // localized name can never reference a nonexistent place and the polymorphic orphan path
        // that an unchecked (PlaceKind, PlaceId) pair would allow no longer exists in the model.
        modelBuilder.Entity<CountryNameTranslation>(entity =>
        {
            entity.ToTable("country_name_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name)
                .HasMaxLength(CountryNameTranslation.NameMaxLength)
                .IsRequired();

            // One display name per (place, language).
            entity.HasIndex(x => new { x.CountryId, x.LanguageId }).IsUnique();

            // Cascade matches the established repo translation convention (Catalog category /
            // attribute translations): a name row is a strictly owned child with no independent
            // identity, so it must never outlive its place. Places themselves are never physically
            // deleted (IsActive lifecycle, Restrict elsewhere), so this cascade is inert today.
            entity.HasOne<Country>()
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdministrativeAreaNameTranslation>(entity =>
        {
            entity.ToTable("administrative_area_name_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name)
                .HasMaxLength(AdministrativeAreaNameTranslation.NameMaxLength)
                .IsRequired();
            entity.HasIndex(x => new { x.AdministrativeAreaId, x.LanguageId }).IsUnique();
            entity.HasOne<AdministrativeArea>()
                .WithMany()
                .HasForeignKey(x => x.AdministrativeAreaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CityNameTranslation>(entity =>
        {
            entity.ToTable("city_name_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name)
                .HasMaxLength(CityNameTranslation.NameMaxLength)
                .IsRequired();
            entity.HasIndex(x => new { x.CityId, x.LanguageId }).IsUnique();
            entity.HasOne<City>()
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceAreaNameTranslation>(entity =>
        {
            entity.ToTable("service_area_name_translations");
            entity.HasKey(x => x.TranslationId);
            entity.Property(x => x.TranslationId).ValueGeneratedNever();
            entity.Property(x => x.Name)
                .HasMaxLength(ServiceAreaNameTranslation.NameMaxLength)
                .IsRequired();
            entity.HasIndex(x => new { x.ServiceAreaId, x.LanguageId }).IsUnique();
            entity.HasOne<ServiceArea>()
                .WithMany()
                .HasForeignKey(x => x.ServiceAreaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Conventional module outbox skeleton (shared physical contract, module-owned schema).
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Geography. اتصال فقط از متغیر محیطی ابزار EF خوانده می‌شود؛
/// هیچ رشتهٔ اتصال runtime یا credential در این پروژه وجود ندارد.
/// </summary>
public sealed class GeographyDbContextFactory : IDesignTimeDbContextFactory<GeographyDbContext>
{
    /// <inheritdoc />
    public GeographyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GeographyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            GeographyDbContext.Schema,
            typeof(GeographyDbContext));
        return new GeographyDbContext(options.Options);
    }
}
