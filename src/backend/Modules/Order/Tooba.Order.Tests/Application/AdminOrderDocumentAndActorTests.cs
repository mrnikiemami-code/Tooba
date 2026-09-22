using Tooba.Identity.Contracts;
using Tooba.OperatorProfile.Contracts;
using Tooba.Order.Application.Admin.Completeness.Documents;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Payment.Contracts.Admin;
using Xunit;

namespace Tooba.Order.Tests.Application;

/// <summary>فاکتور/رسید snapshot و برچسب انسانی Actor.</summary>
public sealed class AdminOrderDocumentAndActorTests
{
    [Fact]
    public void Invoice_prints_total_quantity_only_when_all_lines_share_one_unit()
    {
        var shared = AdminOrderDocumentRenderer.RenderInvoiceHtml(
            OrderTestData.Checkout(unitCodes: ["kg", "kg"]),
            payment: null);
        var mixed = AdminOrderDocumentRenderer.RenderInvoiceHtml(
            OrderTestData.Checkout(unitCodes: ["kg", "pcs"]),
            payment: null);

        Assert.Contains("تعداد اقلام: <strong dir=\"ltr\">2</strong>", shared, StringComparison.Ordinal);
        Assert.Contains("جمع مقدار:", shared, StringComparison.Ordinal);
        Assert.Contains("تعداد اقلام: <strong dir=\"ltr\">2</strong>", mixed, StringComparison.Ordinal);
        Assert.DoesNotContain("جمع مقدار:", mixed, StringComparison.Ordinal);
    }

    [Fact]
    public void Invoice_uses_snapshot_money_and_leaks_no_commission_or_secret()
    {
        var html = AdminOrderDocumentRenderer.RenderInvoiceHtml(
            OrderTestData.Checkout(unitPrice: 7777.5m),
            payment: null);

        Assert.Contains("7777.5 IRR", html, StringComparison.Ordinal);
        Assert.Contains("فاکتور فروش", html, StringComparison.Ordinal);
        Assert.DoesNotContain("commission", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Receipt_masks_wallet_provider_reference()
    {
        var group = OrderTestData.Checkout();
        var payment = new PaymentAdminOperationalSnapshot(
            Guid.NewGuid(),
            group.CheckoutId,
            "Succeeded",
            1000m,
            "IRR",
            "wallet",
            "w|abcdef0123456789secret",
            null,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            DateTimeOffset.UnixEpoch.AddMinutes(2),
            null,
            false,
            false,
            false);

        var html = AdminOrderDocumentRenderer.RenderReceiptHtml(group, payment);

        Assert.Contains("کیف پول", html, StringComparison.Ordinal);
        Assert.Contains("wallet:abcdef01", html, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", html, StringComparison.Ordinal);
        Assert.Contains("1000 IRR", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Actor_label_prefers_display_name_then_names_then_contact()
    {
        var withDisplay = Guid.NewGuid();
        var withNames = Guid.NewGuid();
        var withContact = Guid.NewGuid();
        var ids = new[] { withDisplay, withNames, withContact };
        var displays = new Dictionary<Guid, ActorDisplayProjection>
        {
            [withDisplay] = new(withDisplay, "اپراتور آلفا", "آلفا", "بتا"),
            [withNames] = new(withNames, "   ", "آلفا", "بتا"),
            [withContact] = new(withContact, "??", null, null),
        };
        var contacts = new Dictionary<Guid, ActorContactProjection>
        {
            [withContact] = new(withContact, null, "09120000000"),
        };

        var labels = AdminOrderActorLabels.Build(ids, displays, contacts);

        Assert.Equal("اپراتور آلفا", labels[withDisplay].DisplayName);
        Assert.Equal("آلفا بتا", labels[withNames].DisplayName);
        Assert.Equal("09120000000", labels[withContact].DisplayName);
    }

    [Fact]
    public void Actor_label_falls_back_to_system_or_unknown_without_technical_ids()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var empty = new Dictionary<Guid, AdminOrderActorLabel>();

        var system = AdminOrderActorLabels.Resolve(null, empty);
        var empty2 = AdminOrderActorLabels.Resolve(Guid.Empty, empty);
        var unknown = AdminOrderActorLabels.Resolve(userId, empty);

        Assert.Equal("system", system.Kind);
        Assert.Equal("توسط سیستم", system.DisplayFa);
        Assert.Equal("system", empty2.Kind);
        Assert.Equal("user", unknown.Kind);
        Assert.Equal("توسط کاربر نامشخص", unknown.DisplayFa);
        Assert.DoesNotContain(userId.ToString("N"), unknown.DisplayFa, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(userId.ToString("N")[..8], unknown.DisplayFa, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unresolvable_display_and_contact_become_unknown_user()
    {
        var id = Guid.NewGuid();

        var labels = AdminOrderActorLabels.Build(
            [id],
            new Dictionary<Guid, ActorDisplayProjection> { [id] = new(id, "?", " ", null) },
            new Dictionary<Guid, ActorContactProjection>());

        Assert.Equal("کاربر نامشخص", labels[id].DisplayName);
        Assert.Equal("user", labels[id].Kind);
    }
}
