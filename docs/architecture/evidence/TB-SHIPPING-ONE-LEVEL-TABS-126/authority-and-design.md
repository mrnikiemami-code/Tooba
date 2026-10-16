TB-SHIPPING-ONE-LEVEL-TABS-126 — authority trace and implemented surface (read + static proof, no process/DB)

================================================================================
1. AUTHORITATIVE INPUTS (VERIFIED EXISTING, read-only)
================================================================================
Cart ownership of lines (membership truth):
  src/backend/Modules/Cart/Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs
    CartLineView(LineId, OfferId, CatalogVariantId, SellerPartyId, ..., Quantity, Currency, ...)
    CartPage(CartId, Version, ..., Lines, GuestSecret, Status)
  -> LineId + SellerPartyId + Quantity are authoritative and come from Cart, not from the client.
  Order reads them through ICartPresentationGateway (Cart.Contracts only; no Cart.Application ref).

Eligible methods (method identity + price truth):
  src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs
    LoadEligibleMethodsAsync(cartSubtotal, provinceName, language, ct)  -> active service/option catalog
      filtered by ShippingMethodRegistry.Enabled(_shippingOptions)
    TryAddMethod -> StorefrontShippingMethodView(methodCode "service:option", label, serviceCode,
      iconKey, price = StorefrontShippingCalculator.QuotePrice(rate, subtotal), leadDays, isFree,
      estimationLabel, optionCode)
  -> MethodCode/ServiceCode/OptionCode/Label/PriceAmount/LeadDays are authoritative. Currency is not
     carried per method; it comes from the cart's sole quoted currency
     (StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal).

Delivery promise:
  StorefrontShippingCalculator.MaxSellerPreparationDays(sellers, _shippingOptions)  (slowest seller)
  StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxPrep, method.LeadDays)
  StorefrontShippingCalculator.EnsureDeliveryNotEarlier / ValidTimeWindows
  -> preserved verbatim; for a single group this is byte-identical to the previous behaviour.

Persisted draft shape before this slice:
  "order".cart_shipping_drafts (PK cart_id) with ONE shipping_method_code/label/amount/min date/window
  Evidence: OrderDbContext.cs:131-147 and migration 20260910190000_StorefrontShippingDraftAndCheckoutFields.cs
  -> the cart-wide limitation TB-124 proved.

================================================================================
2. WHAT WAS IMPLEMENTED (this slice)
================================================================================
Domain (src/backend/Modules/Order/Tooba.Order.Domain/Checkout/):
  CartShippingDraftGroup.cs      group entity + line entity + CartShippingGroupFault codes
  CartShippingGroupPlanner.cs    pure planner (all membership/identity invariants)
  CartShippingDraft.cs           owns List<CartShippingDraftGroup> ShippingGroups;
                                 ClearShippingGroups (phase 1) / AddShippingGroups (phase 2)
  Navigation shape matches the certified house pattern:
    SellerOrder.cs:14  public List<OrderLine> Lines { get; } = [];
    (so EF maps List<T> { get; } = [] navigations cleanly; no backing-field guesswork)

Infrastructure:
  OrderDbContext.cs           mapping for cart_shipping_groups + cart_shipping_group_lines
  Migrations/20261016090000_AddCartShippingGroups.cs  (additive SQL, house raw-SQL style)
  Storefront/StorefrontOrderStores.cs  GetByCartIdAsync now Includes groups + lines

Application (additive contracts, no breaking change):
  StorefrontShippingGroupSelection  (request: method code + line ids + optional service/option/origin)
  StorefrontShippingGroupView       (projection: identity, seller, method triple, label, amount,
                                     currency, schedule, lead days, line count)
  StorefrontShippingSelectionRequest.ShippingGroups : optional trailing parameter
  StorefrontShippingDraftView.ShippingGroups        : optional trailing parameter
  StorefrontShippingService: BuildRequestedGroups / CartShippingGroupPlanner.Plan / MapGroup /
    MapGroupFault / EnsureLegacySingleGroup

================================================================================
3. WHY NO BLOCKED_AUTHORITY
================================================================================
- Seller identity: authoritative from Cart (CartLineView.SellerPartyId) -> grouping is grounded.
- Method identity/price: authoritative from the eligible-method projection + ShippingMethodsOptions.
- Fulfilment ORIGIN: no trusted authority exists at draft selection, so per decision (a) the field is
  typed+optional and any asserted value is REFUSED (shipping.group.origin.unverified) rather than
  forged. This is an explicit, recorded gap, not a silent merge.
- Per-seller METHOD eligibility does not exist (eligibility is cart-wide). Recorded as an open gap;
  it does not make grouping ungrounded because membership is derived from Cart ownership and the
  method set is the same authoritative catalog the storefront already trusts.
No shared process, no database, no DDL/DML, no fixture, no feature flag and no frontend file was touched.

================================================================================
4. NON-INTERFERENCE
================================================================================
- Shared :5088/:3000, tooba_alpha and all containers: untouched. No process launched/stopped.
- Pre-existing dirty files preserved and never staged:
    src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx
    src/frontend/app/admin/admin-order-detail-screen.tsx
- Bridge task receipt kept UNSTAGED: docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-126.task.md
- No frontend/CSS/theme/template edit (visual non-regression lock).
