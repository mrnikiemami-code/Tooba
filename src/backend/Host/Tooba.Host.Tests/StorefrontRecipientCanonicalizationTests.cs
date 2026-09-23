using Tooba.AddressBook.Domain;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قانون نمایش گیرنده: First+Last برنده است؛ RecipientName فقط fallback تاریخی است و شکسته نمی‌شود.</summary>
public sealed class StorefrontRecipientCanonicalizationTests
{
    [Fact]
    public void Display_prefers_first_and_last_over_stale_recipient()
    {
        Assert.Equal("محمد امامی", StorefrontRecipientNames.Display("محمد", "امامی", "محمد لمامی"));
        Assert.Equal("محمد امامی", StorefrontRecipientNames.Display(" محمد ", " امامی ", "علی رضایی"));
    }

    [Fact]
    public void Display_falls_back_to_legacy_recipient_without_splitting()
    {
        Assert.Equal("محمد لمامی", StorefrontRecipientNames.Display("", "", "محمد لمامی"));
        Assert.Equal("محمد لمامی", StorefrontRecipientNames.Display("محمد", "", "محمد لمامی"));
        Assert.Equal("محمد لمامی", StorefrontRecipientNames.Display("", "امامی", "محمد لمامی"));
        Assert.Equal(string.Empty, StorefrontRecipientNames.Display("", "", ""));
        Assert.Equal("مشتری توبا", StorefrontRecipientNames.DisplayOrFallback("", "", ""));
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontRecipientNames.cs"));
        Assert.DoesNotContain("Split(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("recipient.Split", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Explicit_names_win_over_legacy_and_request_recipient_alone_does_not()
    {
        var explicitWin = StorefrontRecipientNames.ResolveExplicitOverLegacy(
            "محمد", "امامی", "محمد لمامی", "", "", "محمد لمامی");
        Assert.Equal(("محمد", "امامی", "محمد امامی"), explicitWin);

        var recipientAlone = StorefrontRecipientNames.ResolveExplicitOverLegacy(
            "", "", "علی رضایی", "", "", "محمد لمامی");
        Assert.Equal(("محمد لمامی"), recipientAlone.Recipient);
        Assert.Equal(string.Empty, recipientAlone.First);
        Assert.Equal(string.Empty, recipientAlone.Last);
    }

    [Fact]
    public void New_address_requires_first_and_last_and_mirrors_recipient()
    {
        Assert.Throws<StorefrontOrderException>(() => StorefrontRecipientNames.EnsureNewAddressNames("", "امامی"));
        Assert.Throws<StorefrontOrderException>(() => StorefrontRecipientNames.EnsureNewAddressNames("محمد", ""));
        StorefrontRecipientNames.EnsureNewAddressNames("محمد", "امامی");
        var resolved = StorefrontRecipientNames.Resolve("محمد", "امامی", "باید نادیده شود");
        Assert.Equal("محمد", resolved.First);
        Assert.Equal("امامی", resolved.Last);
        Assert.Equal("محمد امامی", resolved.Recipient);
    }

    [Fact]
    public void Address_entity_keeps_legacy_recipient_until_both_parts_exist()
    {
        var address = CustomerAddress.Create(
            Guid.NewGuid(), "محمد لمامی", "+989120000000", "IR", null, "Tehran", "19199",
            "Sample street 14", null, null, false, DateTimeOffset.UtcNow);
        address.ApplyRecipientNames("", "");
        Assert.Equal("محمد لمامی", address.RecipientName);
        Assert.Equal(string.Empty, address.FirstName);
        Assert.Equal(string.Empty, address.LastName);

        address.ApplyRecipientNames("محمد", "امامی");
        Assert.Equal("محمد", address.FirstName);
        Assert.Equal("امامی", address.LastName);
        Assert.Equal("محمد امامی", address.RecipientName);
        Assert.Throws<InvalidOperationException>(() => address.ApplyRecipientNames("علی", ""));
    }

    [Fact]
    public void Shipping_draft_mirrors_first_last_without_guessing_legacy_split()
    {
        var draft = CartShippingDraft.Create(
            Guid.NewGuid(),
            string.Empty,
            1,
            "محمد لمامی",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان نمونه",
            "19199",
            null,
            "post:express",
            "پست",
            0m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow),
            "9-12",
            null,
            DateTimeOffset.UtcNow);
        draft.ApplyRecipientNames("", "");
        Assert.Equal("محمد لمامی", draft.RecipientName);
        Assert.Equal(string.Empty, draft.FirstName);

        draft.ApplyRecipientNames("محمد", "امامی");
        Assert.Equal("محمد", draft.FirstName);
        Assert.Equal("امامی", draft.LastName);
        Assert.Equal("محمد امامی", draft.RecipientName);
    }

    [Fact]
    public void Payment_customer_and_admin_projections_share_the_same_display_helper()
    {
        var checkout = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontCheckoutService.cs"));
        var shipping = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Services", "StorefrontShippingService.cs"));
        var customer = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Customer", "CustomerOrderComposer.cs"));
        var adminCustomers = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Admin", "Customers", "AdminCustomersGridReader.cs"));
        Assert.Contains("ResolveExplicitOverLegacy", checkout, StringComparison.Ordinal);
        Assert.Contains("StorefrontRecipientNames.Display", checkout, StringComparison.Ordinal);
        Assert.Contains("shipping?.FirstName", checkout, StringComparison.Ordinal);
        Assert.Contains("shipping?.LastName", checkout, StringComparison.Ordinal);
        Assert.Contains("StorefrontRecipientNames.Display", shipping, StringComparison.Ordinal);
        Assert.Contains("StorefrontRecipientNames.Display", customer, StringComparison.Ordinal);
        Assert.Contains("StorefrontRecipientNames.Display", adminCustomers, StringComparison.Ordinal);
        Assert.DoesNotContain("RecipientName.Split", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("addressBook.GetAsync", checkout.Split("MapPage")[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Historical_order_fallback_uses_recipient_when_split_fields_absent()
    {
        Assert.Equal(
            "محمد لمامی",
            StorefrontRecipientNames.Display(string.Empty, string.Empty, "محمد لمامی"));
        Assert.Equal(
            "محمد امامی",
            StorefrontRecipientNames.Display("محمد", "امامی", "محمد لمامی"));
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
}
