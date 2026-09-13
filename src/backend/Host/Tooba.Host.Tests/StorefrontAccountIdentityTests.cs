using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>هدر ویترین نام پروفایل یا موبایل را نشان می‌دهد، نه Recipient ارسال.</summary>
public sealed class StorefrontAccountIdentityTests
{
    [Fact]
    public void Profile_display_name_wins()
    {
        Assert.Equal("علی رضایی", StorefrontAccountIdentity.CanonicalName("علی رضایی", "علی", "رضایی"));
        Assert.Equal("علی رضایی", StorefrontAccountIdentity.Label("علی رضایی", "09111111111", "حساب کاربری"));
    }

    [Fact]
    public void First_and_last_compose_when_display_name_absent()
    {
        Assert.Equal("محمد امامی", StorefrontAccountIdentity.CanonicalName("  ", "محمد", "امامی"));
    }

    [Fact]
    public void Mobile_is_fallback_when_no_profile_name()
    {
        Assert.Null(StorefrontAccountIdentity.CanonicalName(null, null, null));
        Assert.Equal("09111111111", StorefrontAccountIdentity.Label(null, "09111111111", "حساب کاربری"));
    }

    [Fact]
    public void Generic_label_is_last_fallback()
    {
        Assert.Equal("حساب کاربری", StorefrontAccountIdentity.Label(null, "  ", "حساب کاربری"));
    }

    [Fact]
    public void Me_projection_does_not_read_shipping_recipient()
    {
        var source = System.IO.File.ReadAllText(
            System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", "AuthenticationHttpBoundary.cs"));
        Assert.Contains("ICustomerProfileDirectory", source, System.StringComparison.Ordinal);
        Assert.Contains("IIdentityContactLookup", source, System.StringComparison.Ordinal);
        Assert.DoesNotContain("RecipientName", source, System.StringComparison.Ordinal);
        Assert.Contains("StorefrontAccountIdentity.CanonicalName", source, System.StringComparison.Ordinal);
    }
}
