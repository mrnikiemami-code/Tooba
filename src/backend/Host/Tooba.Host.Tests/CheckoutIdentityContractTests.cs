using System.Reflection;
using Tooba.Cart.Domain;
using Tooba.Catalog.Domain;
using Tooba.Offer.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قرارداد هویت خرید، ورود OTP و ادغام سبد بدون اجرای Host کامل.
/// </summary>
public sealed class CheckoutIdentityContractTests
{
    [Fact]
    public void Default_checkout_identity_policy_is_authenticated_only()
    {
        var settings = StoreCheckoutIdentitySettings.CreateDefault(DateTimeOffset.UtcNow);
        Assert.Equal(CheckoutIdentityPolicyKind.AuthenticatedOnly, settings.Policy);
        settings.Replace(CheckoutIdentityPolicyKind.GuestAllowed, DateTimeOffset.UtcNow);
        Assert.Equal(CheckoutIdentityPolicyKind.GuestAllowed, settings.Policy);
    }

    [Fact]
    public void Guest_cart_can_be_adopted_without_reservation_or_order()
    {
        var now = DateTimeOffset.UtcNow;
        var cart = ShoppingCart.CreateGuest("hash", "IR", "IRR", SalesChannel.Marketplace, now, now.AddHours(2));
        var userId = Guid.NewGuid();
        cart.AdoptAuthenticatedOwner(userId, now.AddMinutes(1));
        Assert.Equal(CartAccessKind.Authenticated, cart.AccessKind);
        Assert.Equal(userId, cart.OwnerUserId);
        Assert.Null(cart.GuestCredentialHash);
        Assert.Equal(CartStatus.Active, cart.Status);
        Assert.DoesNotContain("Reservation", typeof(ShoppingCart).GetMethod(nameof(ShoppingCart.AdoptAuthenticatedOwner))!.Name);
    }

    [Fact]
    public void Storefront_and_auth_boundaries_expose_otp_login_and_merge()
    {
        var auth = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "AuthenticationHttpBoundary.cs"));
        var endpoints = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontEndpoints.cs"));
        var checkout = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontCheckoutComposer.cs"));
        var login = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "frontend", "app", "login", "storefront-login.tsx"));
        Assert.Contains("/otp-login/request", auth, StringComparison.Ordinal);
        Assert.Contains("/otp-login/complete", auth, StringComparison.Ordinal);
        Assert.Contains("checkout.authentication_required", endpoints, StringComparison.Ordinal);
        Assert.Contains("/cart/merge", endpoints, StringComparison.Ordinal);
        Assert.Contains("checkout-identity-policy", endpoints, StringComparison.Ordinal);
        Assert.Contains("_session.IsAuthenticated ? _session.UserId", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"password\"", login, StringComparison.Ordinal);
        Assert.DoesNotContain("TB-P10-T005", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Decimal_merge_quantity_stays_exact()
    {
        const decimal guest = 1.25m;
        const decimal authenticated = 1.25m;
        CartLine.EnsureQuantity(guest);
        CartLine.EnsureQuantity(authenticated);
        Assert.Equal(2.50m, guest + authenticated);
    }

    [Fact]
    public void Development_otp_fixture_is_environment_gated()
    {
        var module = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Identity", "Tooba.Identity.Infrastructure", "IdentityModule.cs"));
        var life = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Identity", "Tooba.Identity.Infrastructure", "IdentityLifecycleService.cs"));
        Assert.Contains("IsDevelopment()", module, StringComparison.Ordinal);
        Assert.Contains("IsEnvironment(\"Testing\")", module, StringComparison.Ordinal);
        Assert.Contains("_otpFixture.Enabled", life, StringComparison.Ordinal);
        Assert.DoesNotContain("09111111111", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "appsettings.Production.json")), StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
