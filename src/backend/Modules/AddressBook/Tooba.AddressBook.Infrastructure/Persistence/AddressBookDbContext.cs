using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.AddressBook.Domain.Aggregates;
using Tooba.AddressBook.Infrastructure.Persistence.Configurations;
using Tooba.Persistence;

namespace Tooba.AddressBook.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل address_book و Outbox همان ماژول.</summary>
public sealed class AddressBookDbContext : DbContext
{
    /// <summary>نام schema اختصاصی دفترچهٔ آدرس.</summary>
    public const string Schema = "address_book";

    /// <summary>DbContext را با گزینه‌های ماژول می‌سازد.</summary>
    public AddressBookDbContext(DbContextOptions<AddressBookDbContext> options) : base(options)
    {
    }

    /// <summary>ردیف‌های خصوصی نشانی مشتری.</summary>
    public DbSet<CustomerAddress> Addresses => Set<CustomerAddress>();

    /// <summary>پیام‌های Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new CustomerAddressConfiguration());
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت‌های AddressBook.</summary>
public sealed class AddressBookDbContextFactory : IDesignTimeDbContextFactory<AddressBookDbContext>
{
    /// <inheritdoc />
    public AddressBookDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AddressBookDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            AddressBookDbContext.Schema,
            typeof(AddressBookDbContext));
        return new AddressBookDbContext(options.Options);
    }
}
