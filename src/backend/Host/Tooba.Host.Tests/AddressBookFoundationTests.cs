using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Tooba.AddressBook.Application;
using Tooba.AddressBook.Domain;
using Tooba.AddressBook.Infrastructure;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.Host.AddressBook;
using Tooba.Host.Storefront;
using Tooba.Order.Domain;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قفل قرارداد، حریم خصوصی، اعتبارسنجی و ترکیب دفترچهٔ آدرس با checkout.</summary>
public sealed class AddressBookFoundationTests
{
    /// <summary>Entity قیمت، موجودی و کلید سفارش ندارد.</summary>
    [Fact]
    public void Entity_is_intentionally_small()
    {
        var names = typeof(CustomerAddress).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(
            [
                "AddressId", "OwnerUserId", "RecipientName", "ContactMobile", "Country", "ProvinceName",
                "CityName", "PostalCode", "PostalAddress", "BuildingUnit", "Label", "IsDefault", "CreatedAt", "UpdatedAt",
            ],
            names);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Stock", names);
        Assert.DoesNotContain("OrderId", names);
        Assert.DoesNotContain("CheckoutId", names);
    }

    /// <summary>بدنه‌های HTTP و DTO نوشتن هیچ اختیار مالک دریافت نمی‌کنند.</summary>
    [Fact]
    public void Http_and_write_contracts_have_no_owner_authority()
    {
        Assert.DoesNotContain("OwnerUserId", typeof(CustomerAddressWriteRequest).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("OwnerUserId", typeof(CustomerAddressWrite).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("OwnerUserId", typeof(CustomerAddressRecord).GetProperties().Select(x => x.Name));
        Assert.Contains("SavedAddressId", typeof(StorefrontCheckoutShippingInput).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("AddressId", typeof(CheckoutGroup).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("SavedAddressId", typeof(CheckoutGroup).GetProperties().Select(x => x.Name));
        Assert.Equal("address_book", AddressBookDbContext.Schema);
    }

    /// <summary>مرز HTTP در production بدون نشست 401 می‌دهد و owner را از body نمی‌خواند.</summary>
    [Fact]
    public void Endpoint_uses_session_and_rejects_missing_production_actor()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "AddressBook", "AddressBookEndpoints.cs"));
        Assert.Contains("session.IsAuthenticated", source, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status401Unauthorized", source, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", source, StringComparison.Ordinal);
        Assert.Contains("/v1/customer/addresses", source, StringComparison.Ordinal);
        Assert.DoesNotContain("{owner", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OwnerUserId", source, StringComparison.Ordinal);
    }

    /// <summary>کشور تهی به IR تبدیل می‌شود و قوانین رقم‌شمار ایران در هسته نیست.</summary>
    [Fact]
    public void Country_defaults_to_ir_and_validation_is_generic()
    {
        var address = CustomerAddress.Create(
            Guid.NewGuid(), "گیرنده", "+989120000000", null, null, "Tehran", "19199",
            "Sample street 14", null, null, false, DateTimeOffset.UtcNow);
        Assert.Equal("IR", address.Country);
        Assert.Throws<InvalidOperationException>(() => CustomerAddress.Create(
            Guid.Empty, "گیرنده", "+989120000000", "IR", null, "Tehran", "19199",
            "Sample street 14", null, null, false, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => CustomerAddress.Create(
            Guid.NewGuid(), "گیرنده", "123", "IR", null, "Tehran", "19199",
            "Sample street 14", null, null, false, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => CustomerAddress.Create(
            Guid.NewGuid(), "گیرنده", "+989120000000", "IR", null, "Tehran", "12",
            "Sample street 14", null, null, false, DateTimeOffset.UtcNow));
    }

    /// <summary>checkout مهمان بدون SavedAddressId همان اعتبارسنجی درون‌خطی را نگه می‌دارد.</summary>
    [Fact]
    public async Task Guest_inline_checkout_keeps_existing_shipping_validation()
    {
        var composer = CreateComposer(new MemoryAddressBook(), environmentName: "Testing");
        var inline = new StorefrontCheckoutShippingInput(
            "مهمان", "09120000000", "تهران", "تهران", "خیابان نمونه", "19199");
        var prepared = await composer.PrepareShippingAsync(inline, CancellationToken.None);
        Assert.Equal(StorefrontCheckoutComposer.StorefrontGuestActorId, prepared.PlacedByUserId);
        Assert.Equal("مهمان", prepared.Shipping.RecipientName);
        Assert.Null(prepared.Shipping.SavedAddressId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            composer.PrepareShippingAsync(
                new StorefrontCheckoutShippingInput("", "", "", "", "", ""),
                CancellationToken.None));
    }

    /// <summary>نشانی غریبه در checkout رد می‌شود و نشانی خودی تصویربرداری می‌شود.</summary>
    [Fact]
    public async Task Checkout_rejects_foreign_address_and_snapshots_own_address()
    {
        var owner = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
        var other = Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc1");
        var ownId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddd1");
        var foreignId = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1");
        var book = new MemoryAddressBook();
        book.Rows.Add(new OwnedAddress
        {
            Owner = owner,
            Record = new CustomerAddressRecord(
                ownId, "مالک", "09121111111", "IR", "تهران", "تهران", "11111",
                "آدرس مالک", null, "خانه", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        });
        book.Rows.Add(new OwnedAddress
        {
            Owner = other,
            Record = new CustomerAddressRecord(
                foreignId, "غریبه", "09122222222", "IR", "اصفهان", "اصفهان", "22222",
                "آدرس غریبه", null, "کار", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        });

        var composer = CreateComposer(book, owner, "Testing");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            composer.PrepareShippingAsync(
                new StorefrontCheckoutShippingInput("", "", "", "", "", "", foreignId),
                CancellationToken.None));

        var prepared = await composer.PrepareShippingAsync(
            new StorefrontCheckoutShippingInput("ignored", "ignored", "ignored", "ignored", "ignored", "ignored", ownId),
            CancellationToken.None);
        Assert.Equal(owner, prepared.PlacedByUserId);
        Assert.Equal("مالک", prepared.Shipping.RecipientName);
        Assert.Equal("09121111111", prepared.Shipping.ContactMobile);
        Assert.Equal("تهران", prepared.Shipping.ProvinceName);
        Assert.Equal("تهران", prepared.Shipping.CityName);
        Assert.Equal("آدرس مالک", prepared.Shipping.PostalAddress);
        Assert.Equal("11111", prepared.Shipping.PostalCode);
        Assert.Null(prepared.Shipping.SavedAddressId);

        book.Rows[0].Record = book.Rows[0].Record with { RecipientName = "ویرایش‌شده", PostalAddress = "آدرس جدید" };
        Assert.Equal("مالک", prepared.Shipping.RecipientName);
        Assert.Equal("آدرس مالک", prepared.Shipping.PostalAddress);
    }

    private static StorefrontCheckoutComposer CreateComposer(
        IAddressBookDirectory directory,
        Guid? headerActor = null,
        string environmentName = "Testing")
    {
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        if (headerActor is Guid actor)
        {
            http.HttpContext.Request.Headers["X-Tooba-Dev-Actor-User-Id"] = actor.ToString("D");
        }

        return new StorefrontCheckoutComposer(
            null!,
            null!,
            directory,
            new CurrentAuthenticatedSession(),
            new TestHostEnvironment { EnvironmentName = environmentName },
            http);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    /// <summary>دفترچهٔ حافظه‌ای برای آزمون تصویربرداری checkout بدون PostgreSQL.</summary>
    private sealed class MemoryAddressBook : IAddressBookDirectory
    {
        /// <summary>ردیف‌های آزمایشی با مالک صریح.</summary>
        public List<OwnedAddress> Rows { get; } = [];

        /// <inheritdoc />
        public Task<CustomerAddressRecord> CreateAsync(Guid actorUserId, CustomerAddressWrite input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public Task<IReadOnlyList<CustomerAddressRecord>> ListAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomerAddressRecord>>(Rows.Where(x => x.Owner == actorUserId).Select(x => x.Record).ToList());

        /// <inheritdoc />
        public Task<CustomerAddressRecord?> GetAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.FirstOrDefault(x => x.Owner == actorUserId && x.Record.AddressId == addressId)?.Record);

        /// <inheritdoc />
        public Task<CustomerAddressRecord> UpdateAsync(Guid actorUserId, Guid addressId, CustomerAddressWrite input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public Task DeleteAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public Task<CustomerAddressRecord> SetDefaultAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.LongCount(x => x.Owner == actorUserId));
    }

    /// <summary>ردیف آزمایشی دفترچه با مالک جدا از DTO عمومی.</summary>
    private sealed class OwnedAddress
    {
        /// <summary>شناسهٔ مالک آزمایش.</summary>
        public Guid Owner { get; init; }
        /// <summary>نمایهٔ عمومی.</summary>
        public CustomerAddressRecord Record { get; set; } = default!;
    }

    /// <summary>محیط میزبان آزمایشی برای seam توسعه.</summary>
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        /// <inheritdoc />
        public string EnvironmentName { get; set; } = "Testing";
        /// <inheritdoc />
        public string ApplicationName { get; set; } = "tests";
        /// <inheritdoc />
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        /// <inheritdoc />
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

/// <summary>اثبات PostgreSQL برای CRUD، پیش‌فرض یکتا، ایزولاسیون و دانهٔ تکرارپذیر.</summary>
[Collection("PostgresSerial")]
public sealed class AddressBookPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _available;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").WithDatabase("tooba_address_book")
                .WithUsername("tooba").WithPassword("dev-placeholder").Build();
            await _container.StartAsync();
            _available = true;
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>مالک می‌تواند بسازد، فهرست کند، بخواند، ویرایش و حذف کند.</summary>
    [SkippableFact]
    public async Task Owner_can_create_list_get_update_and_delete()
    {
        await using var db = await OpenAsync();
        var directory = new AddressBookDirectory(db);
        var owner = Guid.NewGuid();
        var created = await directory.CreateAsync(owner, SampleWrite("خانه", true), CancellationToken.None);
        var listed = await directory.ListAsync(owner, CancellationToken.None);
        var fetched = await directory.GetAsync(owner, created.AddressId, CancellationToken.None);
        Assert.Single(listed);
        Assert.Equal(created.AddressId, fetched?.AddressId);
        var updated = await directory.UpdateAsync(owner, created.AddressId, SampleWrite("محل کار", false), CancellationToken.None);
        Assert.Equal("محل کار", updated.Label);
        await directory.DeleteAsync(owner, created.AddressId, CancellationToken.None);
        Assert.Empty(await directory.ListAsync(owner, CancellationToken.None));
    }

    /// <summary>تنظیم پیش‌فرض قبلی را اتمیک برمی‌دارد و حذف پیش‌فرض جایگزین نمی‌سازد.</summary>
    [SkippableFact]
    public async Task Single_default_invariant_has_no_automatic_replacement()
    {
        await using var db = await OpenAsync();
        var directory = new AddressBookDirectory(db);
        var owner = Guid.NewGuid();
        var first = await directory.CreateAsync(owner, SampleWrite("اول", true), CancellationToken.None);
        var second = await directory.CreateAsync(owner, SampleWrite("دوم", true), CancellationToken.None);
        var listed = await directory.ListAsync(owner, CancellationToken.None);
        Assert.Single(listed, x => x.IsDefault);
        Assert.Equal(second.AddressId, listed.Single(x => x.IsDefault).AddressId);
        await directory.SetDefaultAsync(owner, first.AddressId, CancellationToken.None);
        listed = await directory.ListAsync(owner, CancellationToken.None);
        Assert.Equal(first.AddressId, listed.Single(x => x.IsDefault).AddressId);
        await directory.DeleteAsync(owner, first.AddressId, CancellationToken.None);
        listed = await directory.ListAsync(owner, CancellationToken.None);
        Assert.DoesNotContain(listed, x => x.IsDefault);
        Assert.Single(listed);
    }

    /// <summary>A نمی‌تواند نشانی B را بخواند یا تغییر دهد.</summary>
    [SkippableFact]
    public async Task Actor_cannot_read_or_mutate_foreign_address()
    {
        await using var db = await OpenAsync();
        var directory = new AddressBookDirectory(db);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var owned = await directory.CreateAsync(a, SampleWrite("A", true), CancellationToken.None);
        Assert.Null(await directory.GetAsync(b, owned.AddressId, CancellationToken.None));
        Assert.Empty(await directory.ListAsync(b, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.UpdateAsync(b, owned.AddressId, SampleWrite("هک", false), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.DeleteAsync(b, owned.AddressId, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.SetDefaultAsync(b, owned.AddressId, CancellationToken.None));
        Assert.NotNull(await directory.GetAsync(a, owned.AddressId, CancellationToken.None));
    }

    /// <summary>دانهٔ Development تکرارپذیر است و دو نشانی با یک پیش‌فرض می‌سازد.</summary>
    [SkippableFact]
    public async Task Development_seed_is_idempotent()
    {
        await using var db = await OpenAsync();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        await using var provider = services.BuildServiceProvider();
        await AddressBookDevelopmentSeed.ApplyAsync(provider);
        await AddressBookDevelopmentSeed.ApplyAsync(provider);
        var actor = StorefrontCheckoutComposer.StorefrontGuestActorId;
        var rows = await db.Addresses.AsNoTracking().Where(x => x.OwnerUserId == actor).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Single(rows, x => x.IsDefault);
        Assert.Contains(rows, x => x.AddressId == AddressBookDevelopmentSeed.DefaultAddressId);
        Assert.Contains(rows, x => x.AddressId == AddressBookDevelopmentSeed.AlternateAddressId);
    }

    private async Task<AddressBookDbContext> OpenAsync()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var options = new DbContextOptionsBuilder<AddressBookDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, _container!.GetConnectionString(), AddressBookDbContext.Schema, typeof(AddressBookDbContext));
        var db = new AddressBookDbContext(options.Options);
        await db.Database.MigrateAsync();
        return db;
    }

    private static CustomerAddressWrite SampleWrite(string label, bool isDefault) =>
        new("گیرندهٔ آزمایشی", "+989120000099", "IR", "تهران", "تهران", "19199",
            "خیابان آزمایش، پلاک ۹", "واحد ۲", label, isDefault);
}
