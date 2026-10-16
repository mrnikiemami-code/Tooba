using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-140: نگاشت EF گروه‌های ارسال Fulfillment باید دقیقاً با ستون‌های
/// اسکیمای مهاجرت‌شدهٔ <c>fulfillment.shipping_groups</c> بخواند. پیش از این تست، مدل یک کلید بیگانهٔ
/// سایه‌ای به نام <c>fulfillment_unit_fulfillment_id</c> می‌ساخت که هیچ مهاجرتی آن را نمی‌سازد؛
/// Provider حافظه‌ای آن را پنهان می‌کرد و فقط اجرای واقعی Postgres با
/// <c>PostgresException 42703</c> آن را آشکار می‌کرد.
/// </summary>
public sealed class FulfillmentShippingGroupSchemaParityTests
{
    private static readonly string[] MigratedColumns =
    [
        "shipping_group_id",
        "fulfillment_id",
        "seller_order_id",
        "seller_party_id",
        "method_code",
        "service_code",
        "option_code",
        "method_label",
        "origin_context",
        "minimum_delivery_date",
        "requested_delivery_date",
        "requested_delivery_time_window",
        "lead_days",
    ];

    /// <summary>ستون‌های نگاشت‌شدهٔ گروه ارسال باید همان مجموعهٔ مهاجرت‌شده باشند؛ نه بیشتر، نه کمتر.</summary>
    [Fact]
    public void Shipping_group_model_columns_match_the_migrated_schema_exactly()
    {
        using var db = CreateModelContext();
        var group = db.Model.FindEntityType(typeof(FulfillmentShippingGroup));

        Assert.NotNull(group);

        var columns = group!.GetProperties()
            .Select(property => property.GetColumnName())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(MigratedColumns.OrderBy(name => name, StringComparer.Ordinal).ToArray(), columns);
    }

    /// <summary>هیچ کلید بیگانهٔ سایه‌ای روی گروه ارسال نباید ساخته شود.</summary>
    [Fact]
    public void Shipping_group_model_declares_no_shadow_foreign_key()
    {
        using var db = CreateModelContext();
        var group = db.Model.FindEntityType(typeof(FulfillmentShippingGroup));

        Assert.NotNull(group);
        Assert.Empty(group!.GetForeignKeys());
        Assert.DoesNotContain(
            group.GetProperties(),
            property => property.GetColumnName() == "fulfillment_unit_fulfillment_id");
    }

    /// <summary>کلید واقعی واحد، ستون اسکالر <c>fulfillment_id</c> باقی می‌ماند.</summary>
    [Fact]
    public void Shipping_group_keeps_the_scalar_fulfillment_id_link()
    {
        using var db = CreateModelContext();
        var group = db.Model.FindEntityType(typeof(FulfillmentShippingGroup));

        var link = group!.FindProperty(nameof(FulfillmentShippingGroup.FulfillmentId));

        Assert.NotNull(link);
        Assert.Equal("fulfillment_id", link!.GetColumnName());
        Assert.Equal("shipping_groups", group.GetTableName());
    }

    private static FulfillmentDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            "Host=127.0.0.1;Database=tooba_model_only;Username=model;Password=model",
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }
}
