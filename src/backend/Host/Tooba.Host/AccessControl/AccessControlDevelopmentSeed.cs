using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using Tooba.Host.Storefront;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Host.AccessControl;

/// <summary>
/// دانهٔ Development-only برای سناریوی Mobile-vs-Books کنترل دسترسی.
/// idempotent، بدون bypass مجوز، فقط از قراردادهای مالک.
/// </summary>
internal static class AccessControlDevelopmentSeed
{
    internal const string MobileCategoryFa = "موبایل";
    internal const string BooksCategoryFa = "کتاب";
    internal const string DemoRootCategoryFa = "دمو کنترل دسترسی";
    internal const string MobileProductSlug = "acc-demo-mobile-phone";
    internal const string BooksProductSlug = "acc-demo-books-novel";
    internal const string EmployeeEmail = "seller-employee-mobile@tooba.local";
    internal const string RoleCode = "mobile-order-op";
    internal const string RoleName = "Mobile Order Operator";
    internal const string MobileOrderIdempotency = "acc-demo-seed-mobile-v1";
    internal const string BooksOrderIdempotency = "acc-demo-seed-books-v1";
    internal const string MixedOrderIdempotency = "acc-demo-seed-mixed-v1";

    /// <summary>سناریوی ACC را پس از bootstrap کاتالوگ/فروشنده اعمال می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        {
            return;
        }

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            new TenantContext(
                tenant.TenantId,
                tenant.Status,
                tenant.ConnectionReference,
                tenant.DisplayName,
                tenant.ThemeReference,
                tenant.DefaultMarketReference,
                tenant.Hosts[0],
                tenant.PrimaryDomain),
            tenant.ConnectionReference,
            "acc-demo-seed"));

        var partyDb = provider.GetRequiredService<PartyDbContext>();
        var catalogDb = provider.GetRequiredService<CatalogDbContext>();
        var orderDb = provider.GetRequiredService<OrderDbContext>();
        var offerDb = provider.GetRequiredService<OfferDbContext>();
        var inventoryDb = provider.GetRequiredService<InventoryDbContext>();

        var seller = await partyDb.Parties.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DisplayName == SellerDevActorBootstrap.SellerADisplayName, cancellationToken);
        if (seller is null)
        {
            return;
        }

        await AdminDevActorBootstrap.EnsureAsync(provider, cancellationToken);
        await SellerDevActorBootstrap.EnsureAsync(provider, cancellationToken);
        var adminSnapshot = AdminDevActorBootstrap.Snapshot;
        var sellerSnapshot = SellerDevActorBootstrap.Snapshot;
        if (adminSnapshot is null || sellerSnapshot is null)
        {
            return;
        }

        var catalog = provider.GetRequiredService<ICatalogDirectory>();
        var offers = provider.GetRequiredService<IOfferDirectory>();
        var prices = provider.GetRequiredService<IPriceDirectory>();
        var inventory = provider.GetRequiredService<IInventoryDirectory>();
        var tax = provider.GetRequiredService<ITaxDirectory>();
        var taxDb = provider.GetRequiredService<TaxDbContext>();
        var access = provider.GetRequiredService<IAccessControlDirectory>();
        var auth = provider.GetRequiredService<IIdentityAuthenticationService>();
        var carts = provider.GetRequiredService<ICartDirectory>();
        var checkouts = provider.GetRequiredService<ICheckoutDirectory>();

        var mobileCategory = await EnsureDemoCategoryAsync(
            catalog,
            catalogDb,
            DemoRootCategoryFa,
            MobileCategoryFa,
            cancellationToken);
        var booksCategory = await EnsureDemoCategoryAsync(
            catalog,
            catalogDb,
            DemoRootCategoryFa,
            BooksCategoryFa,
            cancellationToken);

        var mobileOffer = await EnsureDemoOfferAsync(
            catalog,
            offers,
            prices,
            inventory,
            tax,
            taxDb,
            catalogDb,
            offerDb,
            inventoryDb,
            seller.PartyId,
            MobileProductSlug,
            "گوشی دمو موبایل",
            "ACC Demo Mobile Phone",
            mobileCategory.CategoryId,
            2500000m,
            "WH-ACC-MOB",
            cancellationToken);
        var booksOffer = await EnsureDemoOfferAsync(
            catalog,
            offers,
            prices,
            inventory,
            tax,
            taxDb,
            catalogDb,
            offerDb,
            inventoryDb,
            seller.PartyId,
            BooksProductSlug,
            "کتاب دمو",
            "ACC Demo Book",
            booksCategory.CategoryId,
            350000m,
            "WH-ACC-BOOK",
            cancellationToken);

        var tenantId = tenant.TenantId.Value;
        var sellerOwner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, seller.PartyId, tenantId);
        await access.EnsureBootstrapAsync(adminSnapshot.ActorUserId, [seller.PartyId], tenantId, cancellationToken);
        await EnsureSellerOwnerAssignmentAsync(
            access,
            sellerOwner,
            sellerSnapshot.ActorA.ActorUserId,
            adminSnapshot.ActorUserId,
            cancellationToken);

        var employeeId = await EnsureEmployeeUserAsync(auth, cancellationToken);
        await EnsurePartyMembershipAsync(provider, employeeId, seller.PartyId, cancellationToken);

        var operatorRole = await EnsureMobileOperatorRoleAsync(
            access,
            sellerOwner,
            mobileCategory.CategoryId,
            sellerSnapshot.ActorA.ActorUserId,
            cancellationToken);
        await EnsureEmployeeAssignmentAsync(
            access,
            sellerOwner,
            employeeId,
            operatorRole.Id,
            sellerSnapshot.ActorA.ActorUserId,
            cancellationToken);
        await access.SyncUserCapabilityTuplesAsync(employeeId, sellerOwner, cancellationToken);

        var mobileOrder = await EnsureDemoOrderAsync(
            orderDb,
            carts,
            checkouts,
            mobileOffer.OfferId,
            MobileOrderIdempotency,
            cancellationToken);
        var booksOrder = await EnsureDemoOrderAsync(
            orderDb,
            carts,
            checkouts,
            booksOffer.OfferId,
            BooksOrderIdempotency,
            cancellationToken);
        var mixedOrder = await EnsureMixedDemoOrderAsync(
            orderDb,
            carts,
            checkouts,
            mobileOffer.OfferId,
            booksOffer.OfferId,
            MixedOrderIdempotency,
            cancellationToken);

        var demo = new AccessControlDemoContext(
            adminSnapshot.ActorUserId,
            seller.PartyId,
            seller.DisplayName,
            sellerSnapshot.ActorA.ActorUserId,
            sellerSnapshot.ActorA.ActorLabel,
            employeeId,
            "اپراتور سفارش موبایل",
            mobileCategory.CategoryId,
            MobileCategoryFa,
            booksCategory.CategoryId,
            BooksCategoryFa,
            mobileOffer.OfferId,
            booksOffer.OfferId,
            mobileOrder.SellerOrderId,
            mobileOrder.OrderNumber,
            booksOrder.SellerOrderId,
            booksOrder.OrderNumber,
            mixedOrder.SellerOrderId,
            mixedOrder.OrderNumber,
            operatorRole.Id,
            RoleCode);
        AccessControlDemoSnapshot.Publish(demo);
        SellerDevActorBootstrap.PublishScopedEmployee(
            new SellerDevActorPair(employeeId, "اپراتور سفارش موبایل", seller.PartyId, seller.DisplayName));
    }

    private static async Task<CategoryReference> EnsureDemoCategoryAsync(
        ICatalogDirectory catalog,
        CatalogDbContext catalogDb,
        string rootNameFa,
        string leafNameFa,
        CancellationToken cancellationToken)
    {
        var existingLeaf = await FindCategoryByPersianNameAsync(catalogDb, leafNameFa, cancellationToken);
        if (existingLeaf is not null)
        {
            var parentById = await catalogDb.Categories.AsNoTracking()
                .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
            if (CatalogCategoryTreeRules.IsAssignableProductCategory(existingLeaf.CategoryId, parentById))
            {
                return existingLeaf;
            }
        }

        var root = await FindCategoryByPersianNameAsync(catalogDb, rootNameFa, cancellationToken)
            ?? await catalog.CreateCategoryAsync(
                null,
                new Dictionary<string, string> { ["fa-IR"] = rootNameFa, ["en-US"] = "Access Control Demo" },
                cancellationToken);
        await catalog.PublishCategoryAsync(root.CategoryId, cancellationToken);

        var midNameFa = leafNameFa == MobileCategoryFa ? "موبایل و تبلت" : "عمومی";
        var mid = await FindCategoryByPersianNameAsync(catalogDb, midNameFa, cancellationToken)
            ?? await catalog.CreateCategoryAsync(
                root.CategoryId,
                new Dictionary<string, string>
                {
                    ["fa-IR"] = midNameFa,
                    ["en-US"] = leafNameFa == MobileCategoryFa ? "Mobile & tablet" : "General",
                },
                cancellationToken);
        await catalog.PublishCategoryAsync(mid.CategoryId, cancellationToken);

        var leaf = await FindCategoryByPersianNameAsync(catalogDb, leafNameFa, cancellationToken);
        if (leaf is null)
        {
            leaf = await catalog.CreateCategoryAsync(
                mid.CategoryId,
                new Dictionary<string, string>
                {
                    ["fa-IR"] = leafNameFa,
                    ["en-US"] = leafNameFa == MobileCategoryFa ? "Mobile" : "Books",
                },
                cancellationToken);
            await catalog.PublishCategoryAsync(leaf.CategoryId, cancellationToken);
            return leaf;
        }

        var parents = await catalogDb.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        if (CatalogCategoryTreeRules.IsAssignableProductCategory(leaf.CategoryId, parents))
        {
            return leaf;
        }

        // Legacy L2 leaf: create a true L3 under mid for assignment.
        var l3 = await catalog.CreateCategoryAsync(
            mid.CategoryId,
            new Dictionary<string, string>
            {
                ["fa-IR"] = $"{leafNameFa} (سطح ۳)",
                ["en-US"] = leafNameFa == MobileCategoryFa ? "Mobile L3" : "Books L3",
            },
            cancellationToken);
        await catalog.PublishCategoryAsync(l3.CategoryId, cancellationToken);
        return l3;
    }

    private static async Task<CategoryReference?> FindCategoryByPersianNameAsync(
        CatalogDbContext catalogDb,
        string persianName,
        CancellationToken cancellationToken)
    {
        var ownerId = await catalogDb.LocalizedTexts.AsNoTracking()
            .Where(text =>
                text.OwnerKind == CatalogLocalizedOwnerKind.Category
                && text.FieldKey == "name"
                && text.Locale.StartsWith("fa")
                && text.Value == persianName)
            .Select(text => text.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);
        if (ownerId == Guid.Empty)
        {
            return null;
        }

        var category = await catalogDb.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == ownerId, cancellationToken);
        return category is null
            ? null
            : new CategoryReference(category.CategoryId, category.ParentCategoryId, category.Status);
    }

    private static async Task<OfferReference> EnsureDemoOfferAsync(
        ICatalogDirectory catalog,
        IOfferDirectory offers,
        IPriceDirectory prices,
        IInventoryDirectory inventory,
        ITaxDirectory tax,
        TaxDbContext taxDb,
        CatalogDbContext catalogDb,
        OfferDbContext offerDb,
        InventoryDbContext inventoryDb,
        Guid sellerPartyId,
        string slug,
        string faName,
        string enName,
        Guid categoryId,
        decimal amount,
        string locationCode,
        CancellationToken cancellationToken)
    {
        var taxCategory = await EnsureAccTaxCategoryAsync(tax, taxDb, cancellationToken);
        await EnsureAccTaxRuleAsync(tax, taxDb, taxCategory.CategoryId, cancellationToken);

        var existingProduct = await catalogDb.Products.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SlugSeam == slug, cancellationToken);
        if (existingProduct is not null)
        {
            var variantId = await catalogDb.Variants.AsNoTracking()
                .Where(x => x.ProductId == existingProduct.ProductId)
                .OrderBy(x => x.CombinationFingerprint)
                .Select(x => x.VariantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (variantId != Guid.Empty)
            {
                var existingOffer = await offerDb.Offers.AsNoTracking()
                    .Where(x => x.SellerPartyId == sellerPartyId && x.CatalogVariantId == variantId)
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existingOffer is not null)
                {
                    await tax.AssignOfferCategoryAsync(existingOffer.OfferId, taxCategory.CategoryId, cancellationToken);
                    await EnsureOfferStockAsync(inventory, inventoryDb, existingOffer.OfferId, locationCode, cancellationToken);
                    return new OfferReference(
                        existingOffer.OfferId,
                        existingOffer.CatalogVariantId,
                        existingOffer.SellerPartyId,
                        existingOffer.Channel,
                        existingOffer.Status,
                        existingOffer.SellerSku);
                }
            }
        }

        ProductReference product;
        if (existingProduct is null)
        {
            product = await catalog.CreateProductAsync(
                CatalogProductKind.PhysicalGood,
                slug,
                null,
                new Dictionary<string, string> { ["fa-IR"] = faName, ["en-US"] = enName },
                cancellationToken);
            await catalog.AssignCategoryAsync(product.ProductId, categoryId, cancellationToken);
            await catalog.PublishProductAsync(product.ProductId, cancellationToken);
        }
        else
        {
            product = new ProductReference(existingProduct.ProductId, existingProduct.Kind, existingProduct.Status);
        }

        var variant = await catalogDb.Variants.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .OrderBy(x => x.CombinationFingerprint)
            .FirstOrDefaultAsync(cancellationToken);
        VariantReference variantRef;
        if (variant is null)
        {
            var colorId = await EnsureDemoColorAttributeAsync(catalog, catalogDb, cancellationToken);
            var defaultOption = await catalog.AddAttributeOptionAsync(
                colorId,
                $"acc-{slug}-default",
                new Dictionary<string, string> { ["fa-IR"] = "پیش‌فرض", ["en-US"] = "Default" },
                cancellationToken);
            variantRef = await catalog.CreateVariantAsync(
                product.ProductId,
                slug.ToUpperInvariant().Replace('-', '_'),
                [(colorId, "ignored", defaultOption)],
                cancellationToken);
        }
        else
        {
            variantRef = new VariantReference(variant.VariantId, variant.ProductId, variant.CombinationFingerprint, variant.Status);
        }

        var offer = await offers.CreateOfferAsync(
            variantRef.VariantId,
            sellerPartyId,
            SalesChannel.Marketplace,
            slug.ToUpperInvariant(),
            cancellationToken);
        await offers.ActivateAsync(offer.OfferId, cancellationToken);
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var price = await prices.CreatePriceAsync(offer.OfferId, "IR", SalesChannel.Marketplace, amount, "IRR", start, null, cancellationToken);
        await prices.ActivateAsync(price.PriceId, cancellationToken);
        await tax.AssignOfferCategoryAsync(offer.OfferId, taxCategory.CategoryId, cancellationToken);
        await EnsureOfferStockAsync(inventory, inventoryDb, offer.OfferId, locationCode, cancellationToken);
        return offer;
    }

    private static async Task EnsureOfferStockAsync(
        IInventoryDirectory inventory,
        InventoryDbContext inventoryDb,
        Guid offerId,
        string locationCode,
        CancellationToken cancellationToken)
    {
        var existingLocation = await inventoryDb.Locations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == locationCode, cancellationToken);
        var locationId = existingLocation?.LocationId
            ?? await inventory.CreateLocationAsync(locationCode, "انبار دمو ACC", cancellationToken);

        var existingStock = await inventoryDb.Positions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OfferId == offerId && x.LocationId == locationId, cancellationToken);
        if (existingStock is not null)
        {
            if (existingStock.OnHand < 10)
            {
                await inventory.AdjustAsync(existingStock.StockItemId, StockAdjustmentKind.Increase, 50, "acc-demo-seed-topup", null, cancellationToken);
            }

            return;
        }

        var stock = await inventory.OpenPositionAsync(offerId, locationId, cancellationToken);
        await inventory.AdjustAsync(stock, StockAdjustmentKind.Increase, 50, "acc-demo-seed", null, cancellationToken);
    }

    private static async Task<TaxCategoryReference> EnsureAccTaxCategoryAsync(
        ITaxDirectory tax,
        TaxDbContext taxDb,
        CancellationToken cancellationToken)
    {
        var existing = await taxDb.Categories.AsNoTracking()
            .FirstOrDefaultAsync(category => category.Code == "standard" || category.Code == "standard-demo", cancellationToken);
        if (existing is not null)
        {
            return new TaxCategoryReference(existing.CategoryId, existing.Code, existing.DisplayName);
        }

        return await tax.CreateCategoryAsync("standard", "استاندارد", cancellationToken);
    }

    private static async Task EnsureAccTaxRuleAsync(
        ITaxDirectory tax,
        TaxDbContext taxDb,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var active = await taxDb.Rules.AsNoTracking()
            .AnyAsync(
                rule => rule.CategoryId == categoryId
                    && rule.Jurisdiction == "IR-NAT"
                    && rule.Market == "IR"
                    && rule.Status == TaxRuleStatus.Active,
                cancellationToken);
        if (active)
        {
            return;
        }

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var rule = await tax.CreateRuleAsync(
            "IR-NAT",
            "IR",
            categoryId,
            TaxRuleKind.Percentage,
            0.09m,
            start,
            null,
            10,
            TaxOverridePolicy.Disabled,
            cancellationToken);
        await tax.ActivateRuleAsync(rule.RuleId, cancellationToken);
    }

    private static async Task<Guid> EnsureDemoColorAttributeAsync(
        ICatalogDirectory catalog,
        CatalogDbContext catalogDb,
        CancellationToken cancellationToken)
    {
        const string code = "acc-demo-color";
        var existing = await catalogDb.AttributeDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (existing is not null)
        {
            return existing.DefinitionId;
        }

        return await catalog.CreateAttributeDefinitionAsync(
            code,
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ دمو", ["en-US"] = "Demo Color" },
            cancellationToken);
    }

    private static async Task EnsureSellerOwnerAssignmentAsync(
        IAccessControlDirectory access,
        AccessOwnerScope sellerOwner,
        Guid ownerActorId,
        Guid adminActorId,
        CancellationToken cancellationToken)
    {
        var roles = await access.ListRolesAsync(sellerOwner, includeArchived: false, cancellationToken);
        var ownerRole = roles.FirstOrDefault(r => r.Code == "seller-owner");
        if (ownerRole is null)
        {
            return;
        }

        var assignments = await access.ListAssignmentsAsync(sellerOwner, userId: ownerActorId, cancellationToken);
        if (assignments.Any(a => a.RoleId == ownerRole.Id))
        {
            return;
        }

        await access.AssignRoleAsync(sellerOwner, ownerActorId, ownerRole.Id, adminActorId, "acc-demo-owner", cancellationToken);
        await access.SyncUserCapabilityTuplesAsync(ownerActorId, sellerOwner, cancellationToken);
    }

    private static async Task<Guid> EnsureEmployeeUserAsync(
        IIdentityAuthenticationService auth,
        CancellationToken cancellationToken)
    {
        var existing = await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, EmployeeEmail, cancellationToken);
        if (existing is { } userId)
        {
            return userId;
        }

        try
        {
            return (await auth.RegisterAsync(
                new RegisterUserCommand
                {
                    IdentifierKind = LoginIdentifierKind.Email,
                    Identifier = EmployeeEmail,
                    Password = "seller-dev-horse-1",
                },
                cancellationToken)).UserId;
        }
        catch (IdentityDuplicateIdentifierException)
        {
            return await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, EmployeeEmail, cancellationToken)
                ?? throw new InvalidOperationException("Employee demo actor missing after duplicate.");
        }
    }

    private static async Task EnsurePartyMembershipAsync(
        IServiceProvider provider,
        Guid userId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var parties = provider.GetRequiredService<Tooba.Party.Application.IPartyDirectory>();
        var partyDb = provider.GetRequiredService<PartyDbContext>();
        var tuples = provider.GetRequiredService<IAuthorizationTupleWriter>();
        var exists = await partyDb.Memberships.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.PartyId == sellerPartyId && x.RelationCode == MembershipRelationCodes.Member,
            cancellationToken);
        if (!exists)
        {
            await parties.EstablishMembershipAsync(userId, sellerPartyId, MembershipRelationCodes.Member, cancellationToken);
        }

        // Party DB membership alone is not enough for SellerPanelAccess (party#view = member tuple).
        // InMemory auth is empty after Host restart — always re-touch like SellerDevActorBootstrap.
        await tuples.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(userId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Party,
                    Id = sellerPartyId.ToString("D"),
                },
                Relation = AuthorizationRelations.Member,
            },
            cancellationToken);
    }

    private static async Task<AccessRoleDto> EnsureMobileOperatorRoleAsync(
        IAccessControlDirectory access,
        AccessOwnerScope sellerOwner,
        Guid mobileCategoryId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var roles = await access.ListRolesAsync(sellerOwner, includeArchived: false, cancellationToken);
        var role = roles.FirstOrDefault(r => r.Code == RoleCode)
            ?? await access.CreateRoleAsync(
                sellerOwner,
                new CreateAccessRoleCommand(RoleName, RoleCode, "دمو: فقط سفارش‌های دستهٔ موبایل"),
                actorUserId,
                "acc-demo-role",
                cancellationToken);

        await access.SetRolePermissionsAsync(
            role.Id,
            sellerOwner,
            [
                new RolePermissionGrant("order.view", AccessScopeKind.Category, mobileCategoryId, true),
                new RolePermissionGrant("order.handle", AccessScopeKind.Category, mobileCategoryId, true),
                new RolePermissionGrant("accesscontrol.view", AccessScopeKind.GlobalWithinOwner, null, true),
            ],
            actorUserId,
            "acc-demo-perms",
            cancellationToken);
        return role;
    }

    private static async Task EnsureEmployeeAssignmentAsync(
        IAccessControlDirectory access,
        AccessOwnerScope sellerOwner,
        Guid employeeId,
        Guid roleId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var assignments = await access.ListAssignmentsAsync(sellerOwner, userId: employeeId, cancellationToken);
        if (assignments.Any(a => a.RoleId == roleId))
        {
            return;
        }

        await access.AssignRoleAsync(sellerOwner, employeeId, roleId, actorUserId, "acc-demo-assign", cancellationToken);
    }

    private sealed record SeededSellerOrder(Guid SellerOrderId, string OrderNumber);

    private static async Task<SeededSellerOrder> EnsureDemoOrderAsync(
        OrderDbContext orderDb,
        ICartDirectory carts,
        ICheckoutDirectory checkouts,
        Guid offerId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existingCheckout = await orderDb.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existingCheckout?.SellerOrders.FirstOrDefault() is { } existingOrder)
        {
            return new SeededSellerOrder(existingOrder.SellerOrderId, existingOrder.OrderNumber);
        }

        var guest = await carts.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, cancellationToken);
        var cartAccess = new CartAccess(null, guest.GuestSecret);
        var cart = await carts.AddOrIncreaseLineAsync(
            guest.Cart.CartId,
            cartAccess,
            guest.Cart.Version,
            offerId,
            1,
            cancellationToken);
        var submitted = await checkouts.SubmitAsync(
            new SubmitCheckoutCommand(
                cart.CartId,
                cartAccess,
                cart.Version,
                OrderMode.OnlinePurchase,
                null,
                StorefrontCheckoutComposer.StorefrontGuestActorId,
                idempotencyKey,
                "IR-NAT",
                RecipientName: "گیرنده دمو ACC",
                ContactMobile: "09120000000",
                ProvinceName: "تهران",
                CityName: "تهران",
                PostalAddress: "خیابان دمو ۱",
                PostalCode: "1234567890",
                ShippingMethodCode: "storefront-default",
                ShippingMethodLabel: "ارسال پیش‌فرض"),
            cancellationToken);
        var sellerOrder = submitted.SellerOrders.First();
        return new SeededSellerOrder(sellerOrder.SellerOrderId, sellerOrder.OrderNumber);
    }

    private static async Task<SeededSellerOrder> EnsureMixedDemoOrderAsync(
        OrderDbContext orderDb,
        ICartDirectory carts,
        ICheckoutDirectory checkouts,
        Guid mobileOfferId,
        Guid booksOfferId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existingCheckout = await orderDb.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existingCheckout?.SellerOrders.FirstOrDefault() is { } existingOrder)
        {
            return new SeededSellerOrder(existingOrder.SellerOrderId, existingOrder.OrderNumber);
        }

        var guest = await carts.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, cancellationToken);
        var cartAccess = new CartAccess(null, guest.GuestSecret);
        var cart = await carts.AddOrIncreaseLineAsync(
            guest.Cart.CartId,
            cartAccess,
            guest.Cart.Version,
            mobileOfferId,
            1,
            cancellationToken);
        cart = await carts.AddOrIncreaseLineAsync(
            cart.CartId,
            cartAccess,
            cart.Version,
            booksOfferId,
            1,
            cancellationToken);
        var submitted = await checkouts.SubmitAsync(
            new SubmitCheckoutCommand(
                cart.CartId,
                cartAccess,
                cart.Version,
                OrderMode.OnlinePurchase,
                null,
                StorefrontCheckoutComposer.StorefrontGuestActorId,
                idempotencyKey,
                "IR-NAT",
                RecipientName: "گیرنده دمو ترکیبی",
                ContactMobile: "09121111111",
                ProvinceName: "تهران",
                CityName: "تهران",
                PostalAddress: "خیابان دمو ۲",
                PostalCode: "1234567891",
                ShippingMethodCode: "storefront-default",
                ShippingMethodLabel: "ارسال پیش‌فرض"),
            cancellationToken);
        var sellerOrder = submitted.SellerOrders.First();
        return new SeededSellerOrder(sellerOrder.SellerOrderId, sellerOrder.OrderNumber);
    }
}
