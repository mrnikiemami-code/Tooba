using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.AccessControl.Application;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation اسکیما ویژگی رده و محورهای Variant بدون ماتریس کامل.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogAttributeSchemaTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_catalog_schema_a")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
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

    [Fact]
    public void Permission_catalog_includes_attribute_schema_permissions()
    {
        Assert.Contains(PermissionCatalog.All, p => p.PermissionId == "catalog.attribute.view");
        Assert.Contains(PermissionCatalog.All, p => p.PermissionId == "catalog.attribute.manage");
        Assert.False(PermissionCatalog.IsDelegable("catalog.attribute.manage"));
        Assert.False(PermissionCatalog.IsDelegable("catalog.attribute.view"));
    }

    [Fact]
    public void IsVariantAxisAllowed_aliases_IsVariantAxis_column_semantics()
    {
        var def = CatalogAttributeDefinition.Create("color", CatalogAttributeValueKind.Enumeration, isVariantAxis: true, DateTimeOffset.UtcNow);
        Assert.True(def.IsVariantAxis);
        Assert.True(def.IsVariantAxisAllowed);
        Assert.DoesNotContain("Price", typeof(CatalogAttributeDefinition).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Stock", typeof(CatalogProductVariantAxis).GetProperties().Select(p => p.Name));
    }

    [SkippableFact]
    public async Task Attribute_schema_foundation_covers_inheritance_validation_and_axes()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_catalog_schema_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_catalog_schema_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_catalog_schema_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-schema-a", "tenant-schema-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-schema-b", "tenant-schema-b"));

        await using var dbA = CreateCatalogDb(csA, commerceA);
        await using var dbB = CreateCatalogDb(csB, commerceB);
        await dbA.Database.EnsureCreatedAsync();
        await dbB.Database.EnsureCreatedAsync();

        var dirA = new CatalogDirectory(dbA, new OpenCatalogUseCaseGuard());
        var dirB = new CatalogDirectory(dbB, new OpenCatalogUseCaseGuard());

        // 1+2+3: effective schema + inheritance + child override
        var parent = await dirA.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "الکترونیک" }, CancellationToken.None);
        var mid = await dirA.CreateCategoryAsync(parent.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل و تبلت" }, CancellationToken.None);
        var child = await dirA.CreateCategoryAsync(mid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "گوشی موبایل" }, CancellationToken.None);

        var colorId = await dirA.CreateAttributeDefinitionAsync(
            "color", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" }, CancellationToken.None);
        var storageId = await dirA.CreateAttributeDefinitionAsync(
            "storage", CatalogAttributeValueKind.Enumeration, true,
            new Dictionary<string, string> { ["fa-IR"] = "حافظه" }, CancellationToken.None);
        var screenId = await dirA.CreateAttributeDefinitionAsync(
            "screen_size", CatalogAttributeValueKind.Number, false,
            new Dictionary<string, string> { ["fa-IR"] = "صفحه" }, CancellationToken.None);
        var weightId = await dirA.CreateAttributeDefinitionAsync(
            "weight_g", CatalogAttributeValueKind.Number, false,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" }, CancellationToken.None);

        await dirA.UpdateAttributeDefinitionAsync(
            screenId, "inch", isRequired: true, isFilterable: true, isComparable: true, isMultivalue: false,
            displayOrder: 5, validationMin: 4m, validationMax: 10m, validationMaxLength: null, isActive: true,
            CancellationToken.None);

        await dirA.BindCategoryAttributeAsync(
            parent.CategoryId,
            colorId,
            10,
            new CategoryAttributeAssignmentFlags(false, true, true, false),
            CancellationToken.None);
        await dirA.BindCategoryAttributeAsync(
            parent.CategoryId,
            screenId,
            20,
            new CategoryAttributeAssignmentFlags(false, true, false, true),
            CancellationToken.None);
        await dirA.BindCategoryAttributeAsync(
            child.CategoryId,
            screenId,
            1,
            new CategoryAttributeAssignmentFlags(true, false, false, false),
            CancellationToken.None);
        await dirA.BindCategoryAttributeAsync(
            child.CategoryId,
            storageId,
            2,
            new CategoryAttributeAssignmentFlags(false, true, true, false),
            CancellationToken.None);

        var effective = await dirA.GetEffectiveCategorySchemaAsync(child.CategoryId, CancellationToken.None);
        Assert.Equal(3, effective.Count);
        Assert.Equal(screenId, effective[0].DefinitionId);
        Assert.True(effective[0].IsRequired); // child override required
        Assert.False(effective[0].IsFilterable);
        Assert.Equal(child.CategoryId, effective[0].InheritedFromCategoryId);
        Assert.True(effective[0].IsLocalOverride);
        Assert.Equal(parent.CategoryId, effective[0].OverriddenFromCategoryId);
        Assert.Contains(effective, e => e.DefinitionId == colorId && e.InheritedFromCategoryId == parent.CategoryId && !e.IsLocalOverride);
        Assert.Contains(effective, e => e.DefinitionId == storageId && !e.IsLocalOverride);

        await dirA.UnbindCategoryAttributeAsync(child.CategoryId, screenId, CancellationToken.None);
        var afterReset = await dirA.GetEffectiveCategorySchemaAsync(child.CategoryId, CancellationToken.None);
        var screenAfterReset = Assert.Single(afterReset, e => e.DefinitionId == screenId);
        Assert.Equal(parent.CategoryId, screenAfterReset.InheritedFromCategoryId);
        Assert.False(screenAfterReset.IsLocalOverride);
        Assert.Null(screenAfterReset.OverriddenFromCategoryId);
        // re-bind child override for remainder of test
        await dirA.BindCategoryAttributeAsync(
            child.CategoryId,
            screenId,
            1,
            new CategoryAttributeAssignmentFlags(true, false, false, false),
            CancellationToken.None);

        // 15 default variant BC: no selected axes → any IsVariantAxis definition works
        var productBc = await dirA.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "bc-phone", null,
            new Dictionary<string, string> { ["fa-IR"] = "BC" }, CancellationToken.None);
        var black = await dirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "مشکی" }, CancellationToken.None);
        var white = await dirA.AddAttributeOptionAsync(colorId, "white", new Dictionary<string, string> { ["fa-IR"] = "سفید" }, CancellationToken.None);
        var storage128 = await dirA.AddAttributeOptionAsync(storageId, "128gb", new Dictionary<string, string> { ["fa-IR"] = "۱۲۸" }, CancellationToken.None);
        await dirA.CreateVariantAsync(productBc.ProductId, "BC-BLK", [(colorId, "ignored", black)], CancellationToken.None);

        // schema-bound product
        var product = await dirA.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "schema-phone", null,
            new Dictionary<string, string> { ["fa-IR"] = "گوشی" }, CancellationToken.None);
        await dirA.AssignCategoryAsync(product.ProductId, child.CategoryId, CancellationToken.None);

        // 4 required validated on publish before value is set
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.PublishProductAsync(product.ProductId, CancellationToken.None));

        // 7 typed numeric ok
        await dirA.SetProductAttributeAsync(product.ProductId, screenId, "6.1", null, CancellationToken.None);

        // 7 typed numeric out of range
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.SetProductAttributeAsync(product.ProductId, screenId, "12", null, CancellationToken.None));

        // 6 unknown attribute rejected when schema-bound
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.SetProductAttributeAsync(product.ProductId, weightId, "100", null, CancellationToken.None));

        // 5 invalid option (wrong definition / non-numeric raw for number)
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.SetProductAttributeAsync(product.ProductId, screenId, "ignored", black, CancellationToken.None));

        await dirA.AttachMediaReferenceAsync(
            product.ProductId, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), CancellationToken.None);
        await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
            dirA, product.ProductId, "توضیح سئو گوشی نمونه", CancellationToken.None);
        await dirA.PublishProductAsync(product.ProductId, CancellationToken.None);

        // 8 axis allowed / 9 duplicate axis
        await dirA.SetProductVariantAxesAsync(product.ProductId, [colorId, storageId], CancellationToken.None);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.SetProductVariantAxesAsync(product.ProductId, [colorId, colorId], CancellationToken.None));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.SetProductVariantAxesAsync(product.ProductId, [screenId], CancellationToken.None));

        // selected axes require exact set
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.CreateVariantAsync(product.ProductId, "ONLY-COLOR", [(colorId, "ignored", black)], CancellationToken.None));

        var v1 = await dirA.CreateVariantAsync(
            product.ProductId, "C-BLK-128",
            [(colorId, "ignored", black), (storageId, "ignored", storage128)],
            CancellationToken.None);

        // 10 duplicate variant combo
        await Assert.ThrowsAnyAsync<Exception>(() =>
            dirA.CreateVariantAsync(
                product.ProductId, "DUP",
                [(colorId, "ignored", black), (storageId, "ignored", storage128)],
                CancellationToken.None));

        var storage256 = await dirA.AddAttributeOptionAsync(storageId, "256gb", new Dictionary<string, string> { ["fa-IR"] = "۲۵۶" }, CancellationToken.None);
        var v2 = await dirA.CreateVariantAsync(
            product.ProductId, "C-WHT-256",
            [(colorId, "ignored", white), (storageId, "ignored", storage256)],
            CancellationToken.None);
        Assert.NotEqual(v1.VariantId, v2.VariantId);

        // 11 category change orphan report — do not silently delete
        var otherRoot = await dirA.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "کتاب" }, CancellationToken.None);
        var otherMid = await dirA.CreateCategoryAsync(otherRoot.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "رمان" }, CancellationToken.None);
        var other = await dirA.CreateCategoryAsync(otherMid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "داستان" }, CancellationToken.None);
        var impact = await dirA.PreviewCategoryChangeAsync(product.ProductId, other.CategoryId, CancellationToken.None);
        Assert.Contains(impact.OrphanAttributeValues, o => o.DefinitionId == screenId);
        Assert.Contains(impact.InvalidVariantAxisDefinitionIds, id => id == colorId || id == storageId);
        Assert.True(await dbA.ProductAttributeValues.AnyAsync(v => v.ProductId == product.ProductId && v.DefinitionId == screenId));

        // 14 tenant isolation
        var otherTenantProduct = await dirB.CreateProductAsync(
            CatalogProductKind.PhysicalGood, "other", null,
            new Dictionary<string, string> { ["en-US"] = "Other" }, CancellationToken.None);
        Assert.Null(await dirB.FindProductAsync(product.ProductId, CancellationToken.None));
        Assert.Null(await dirA.FindProductAsync(otherTenantProduct.ProductId, CancellationToken.None));
        Assert.Empty(await dirB.ListAttributeDefinitionsAsync(CancellationToken.None));
    }

    [SkippableFact]
    public async Task Category_assignment_flags_are_per_category_not_global()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-per-cat-flags", "tenant-per-cat-flags"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var catA = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "پوشاک" }, CancellationToken.None);
        var catB = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var brandId = await dir.CreateAttributeDefinitionAsync(
            "brand",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "برند" },
            CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            catA.CategoryId,
            brandId,
            0,
            new CategoryAttributeAssignmentFlags(false, true, false, false),
            CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            catB.CategoryId,
            brandId,
            0,
            new CategoryAttributeAssignmentFlags(false, false, true, true),
            CancellationToken.None);

        var schemaA = await dir.GetEffectiveCategorySchemaAsync(catA.CategoryId, CancellationToken.None);
        var schemaB = await dir.GetEffectiveCategorySchemaAsync(catB.CategoryId, CancellationToken.None);
        var rowA = Assert.Single(schemaA);
        var rowB = Assert.Single(schemaB);
        Assert.True(rowA.IsFilterable);
        Assert.False(rowA.IsVariantAxis);
        Assert.False(rowB.IsFilterable);
        Assert.True(rowB.IsVariantAxis);
        Assert.True(rowB.IsComparable);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.BindCategoryAttributeAsync(
                catB.CategoryId,
                brandId,
                1,
                new CategoryAttributeAssignmentFlags(false, false, true, false),
                CancellationToken.None));
    }

    [SkippableFact]
    public async Task Variant_axis_requires_global_capability_even_when_assignment_requests_it()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-variant-cap", "tenant-variant-cap"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "کتاب" }, CancellationToken.None);
        var weightId = await dir.CreateAttributeDefinitionAsync(
            "weight",
            CatalogAttributeValueKind.Number,
            false,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" },
            CancellationToken.None);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.BindCategoryAttributeAsync(
                category.CategoryId,
                weightId,
                0,
                new CategoryAttributeAssignmentFlags(false, false, true, false),
                CancellationToken.None));
    }

    [SkippableFact]
    public async Task CreateAttributeDefinition_duplicate_code_and_name_throw_stable_messages()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-attr-dup", "tenant-attr-dup"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        await dir.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);

        var codeDup = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.CreateAttributeDefinitionAsync(
                "Color",
                CatalogAttributeValueKind.Text,
                false,
                new Dictionary<string, string> { ["fa-IR"] = "رنگ دیگر" },
                CancellationToken.None));
        Assert.Contains("کد", codeDup.Message);
        Assert.Contains("تکراری", codeDup.Message);

        var nameDup = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.CreateAttributeDefinitionAsync(
                "hue",
                CatalogAttributeValueKind.Text,
                false,
                new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
                CancellationToken.None));
        Assert.Contains("نام", nameDup.Message);
        Assert.Contains("تکراری", nameDup.Message);
    }

    [SkippableFact]
    public async Task AxisAllowed_definition_can_store_product_value_when_binding_is_not_axis()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-attr-axis-product", "tenant-attr-axis-product"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var root = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "گجت" }, CancellationToken.None);
        var mid = await dir.CreateCategoryAsync(root.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "جانبی" }, CancellationToken.None);
        var leaf = await dir.CreateCategoryAsync(mid.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "پاوربانک" }, CancellationToken.None);

        var colorId = await dir.CreateAttributeDefinitionAsync(
            "color_allowed_axis",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ مجاز محور" },
            CancellationToken.None);
        var black = await dir.AddAttributeOptionAsync(
            colorId,
            "black",
            new Dictionary<string, string> { ["fa-IR"] = "مشکی" },
            CancellationToken.None);

        // تعریف مجاز محور است، ولی در schema این دسته به‌عنوان ویژگی عادی (نه محور) بسته شده.
        await dir.BindCategoryAttributeAsync(
            leaf.CategoryId,
            colorId,
            1,
            new CategoryAttributeAssignmentFlags(IsRequired: false, IsFilterable: true, IsVariantAxis: false, IsComparable: false),
            CancellationToken.None);

        var product = await dir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "powerbank-color",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "پاوربانک رنگ" },
            CancellationToken.None);
        await dir.AssignCategoryAsync(product.ProductId, leaf.CategoryId, CancellationToken.None);

        await dir.SetProductAttributeAsync(product.ProductId, colorId, "ignored", black, CancellationToken.None);

        var editor = await dir.GetProductAttributeEditorStateAsync(product.ProductId, "fa-IR", CancellationToken.None);
        var field = Assert.Single(editor.Fields);
        Assert.False(field.IsVariantAxis);
        Assert.Equal(black, field.CurrentEnumOptionId);
    }

    [SkippableFact]
    public async Task Variant_axis_capability_enable_false_to_true_does_not_mutate_bindings()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-vcap-on", "tenant-vcap-on"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var storageId = await dir.CreateAttributeDefinitionAsync(
            "storage_cap",
            CatalogAttributeValueKind.Enumeration,
            false,
            new Dictionary<string, string> { ["fa-IR"] = "حافظه" },
            CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            category.CategoryId,
            storageId,
            1,
            new CategoryAttributeAssignmentFlags(false, true, false, false),
            CancellationToken.None);

        await dir.SetAttributeDefinitionVariantAxisCapabilityAsync(storageId, true, CancellationToken.None);

        var schema = await dir.GetEffectiveCategorySchemaAsync(category.CategoryId, CancellationToken.None);
        var row = Assert.Single(schema, e => e.DefinitionId == storageId);
        Assert.True(row.IsVariantAxisAllowed);
        Assert.False(row.IsVariantAxis);
    }

    [SkippableFact]
    public async Task Variant_axis_capability_disable_blocked_when_binding_uses_variant()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-vcap-off", "tenant-vcap-off"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var colorId = await dir.CreateAttributeDefinitionAsync(
            "color_cap",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            category.CategoryId,
            colorId,
            1,
            new CategoryAttributeAssignmentFlags(false, true, true, false),
            CancellationToken.None);

        var impact = await dir.PreviewVariantAxisCapabilityDisableImpactAsync(colorId, CancellationToken.None);
        Assert.False(impact.CanDisable);
        Assert.Equal(1, impact.CategoryBindingCount);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.SetAttributeDefinitionVariantAxisCapabilityAsync(colorId, false, CancellationToken.None));
        Assert.Equal("catalog.attribute.variant_axis.in_use", ex.Message);
    }

    [SkippableFact]
    public async Task Variant_axis_capability_disable_allowed_with_zero_usage()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-vcap-zero", "tenant-vcap-zero"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var defId = await dir.CreateAttributeDefinitionAsync(
            "storage_zero",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "حافظه" },
            CancellationToken.None);

        var impact = await dir.PreviewVariantAxisCapabilityDisableImpactAsync(defId, CancellationToken.None);
        Assert.True(impact.CanDisable);

        await dir.SetAttributeDefinitionVariantAxisCapabilityAsync(defId, false, CancellationToken.None);
        var view = await dir.GetAttributeDefinitionAsync(defId, CancellationToken.None);
        Assert.NotNull(view);
        Assert.False(view!.IsVariantAxisAllowed);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
