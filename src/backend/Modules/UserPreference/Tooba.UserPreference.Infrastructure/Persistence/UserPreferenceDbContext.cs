using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.UserPreference.Domain;
using UserPreferenceEntity = Tooba.UserPreference.Domain.UserPreference;
using Tooba.Persistence;

namespace Tooba.UserPreference.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل user_preference و Outbox همان ماژول.</summary>
public sealed class UserPreferenceDbContext : DbContext
{
    /// <summary>نام schema اختصاصی ترجیح کاربر.</summary>
    public const string Schema = "user_preference";

    /// <summary>DbContext را با گزینه‌های ماژول می‌سازد.</summary>
    public UserPreferenceDbContext(DbContextOptions<UserPreferenceDbContext> options) : base(options)
    {
    }

    /// <summary>ردیف‌های خصوصی ترجیح کاربر.</summary>
    public DbSet<UserPreferenceEntity> Preferences => Set<UserPreferenceEntity>();

    /// <summary>ترجیح‌های کلیددار UI (مثل نمایش‌های ذخیره‌شدهٔ گرید).</summary>
    public DbSet<UiPreference> UiPreferences => Set<UiPreference>();

    /// <summary>پیام‌های Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<UserPreferenceEntity>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(x => x.OwnerUserId);
            entity.Property(x => x.OwnerUserId).ValueGeneratedNever();
            entity.Property(x => x.Locale).HasMaxLength(UserPreferenceEntity.LocaleMaxLength).IsRequired();
        });
        modelBuilder.Entity<UiPreference>(entity =>
        {
            entity.ToTable("ui_preferences");
            entity.HasKey(x => x.PreferenceId);
            entity.Property(x => x.PreferenceId).ValueGeneratedNever();
            entity.Property(x => x.ActorUserId).IsRequired();
            entity.Property(x => x.Key).HasMaxLength(UiPreference.KeyMaxLength).IsRequired();
            entity.Property(x => x.JsonPayload).IsRequired();
            entity.HasIndex(x => new { x.ActorUserId, x.Key }).IsUnique();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت‌های UserPreference.</summary>
public sealed class UserPreferenceDbContextFactory : IDesignTimeDbContextFactory<UserPreferenceDbContext>
{
    /// <inheritdoc />
    public UserPreferenceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UserPreferenceDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            UserPreferenceDbContext.Schema,
            typeof(UserPreferenceDbContext));
        return new UserPreferenceDbContext(options.Options);
    }
}
