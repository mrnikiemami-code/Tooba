# Domain Ownership Map

Audited **322** domain types across **undefined** Domain projects (structural inventory of class/record/enum/interface/struct).

Principle: owner = invariant/lifecycle BC, not persistence convenience.

## Counts by current module

- Catalog: 93
- Fulfillment: 21
- Order: 19
- Payment: 17
- Promotion: 16
- Content: 14
- Settlement: 14
- Identity: 13
- Cart: 11
- Inventory: 11
- Tax: 11
- Party: 9
- Returns: 9
- Wallet: 8
- AccessControl: 7
- Offer: 7
- Pricing: 7
- Support: 7
- Story: 5
- ProductQnA: 4
- Localization: 3
- BulkInquiry: 2
- Media: 2
- Notification: 2
- PageComposition: 2
- Reviews: 2
- UserPreference: 2
- AddressBook: 1
- CustomerProfile: 1
- OperatorProfile: 1
- Wishlist: 1

## Explicit Move / Needs-design (Catalog convenience dumping — examples required + peers)

| Type | Current | Correct owner (proposed) | Action | Risk | When |
|---|---|---|---|---|---|
| ReservationPolicyAuditEvent | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearanceBrandTokens | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearanceTintTokens | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearancePaletteDefinition | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearanceProductCardSkinDefinition | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearanceThemeMode | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearanceBackgroundStyle | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreAppearanceSettings | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreCheckoutAbuseSettings | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreCheckoutIdentitySettings | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreHoldPolicySettings | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreLandingPageStatus | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreLandingPage | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreLandingPageSection | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreMenuLinkType | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreMenu | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |
| StoreMenuItem | Catalog | PageComposition / StorefrontSettings / TemplateCatalog (TBD) | Move / Needs-design | High | Defer until Host write removal + Contracts; move with dedicated ownership task |

## Keep (structural default)

- **AccessControl**: 7 types Keep (current BC = persistence owner; re-review on feature touch)
- **AddressBook**: 1 types Keep (current BC = persistence owner; re-review on feature touch)
- **BulkInquiry**: 2 types Keep (current BC = persistence owner; re-review on feature touch)
- **Cart**: 11 types Keep (current BC = persistence owner; re-review on feature touch)
- **Catalog**: 76 types Keep (current BC = persistence owner; re-review on feature touch)
- **Content**: 14 types Keep (current BC = persistence owner; re-review on feature touch)
- **CustomerProfile**: 1 types Keep (current BC = persistence owner; re-review on feature touch)
- **Fulfillment**: 21 types Keep (current BC = persistence owner; re-review on feature touch)
- **Identity**: 13 types Keep (current BC = persistence owner; re-review on feature touch)
- **Inventory**: 11 types Keep (current BC = persistence owner; re-review on feature touch)
- **Localization**: 3 types Keep (current BC = persistence owner; re-review on feature touch)
- **Media**: 2 types Keep (current BC = persistence owner; re-review on feature touch)
- **Notification**: 2 types Keep (current BC = persistence owner; re-review on feature touch)
- **Offer**: 7 types Keep (current BC = persistence owner; re-review on feature touch)
- **OperatorProfile**: 1 types Keep (current BC = persistence owner; re-review on feature touch)
- **Order**: 19 types Keep (current BC = persistence owner; re-review on feature touch)
- **PageComposition**: 2 types Keep (current BC = persistence owner; re-review on feature touch)
- **Party**: 9 types Keep (current BC = persistence owner; re-review on feature touch)
- **Payment**: 17 types Keep (current BC = persistence owner; re-review on feature touch)
- **Pricing**: 7 types Keep (current BC = persistence owner; re-review on feature touch)
- **ProductQnA**: 4 types Keep (current BC = persistence owner; re-review on feature touch)
- **Promotion**: 16 types Keep (current BC = persistence owner; re-review on feature touch)
- **Returns**: 9 types Keep (current BC = persistence owner; re-review on feature touch)
- **Reviews**: 2 types Keep (current BC = persistence owner; re-review on feature touch)
- **Settlement**: 14 types Keep (current BC = persistence owner; re-review on feature touch)
- **Story**: 5 types Keep (current BC = persistence owner; re-review on feature touch)
- **Support**: 7 types Keep (current BC = persistence owner; re-review on feature touch)
- **Tax**: 11 types Keep (current BC = persistence owner; re-review on feature touch)
- **UserPreference**: 2 types Keep (current BC = persistence owner; re-review on feature touch)
- **Wallet**: 8 types Keep (current BC = persistence owner; re-review on feature touch)
- **Wishlist**: 1 types Keep (current BC = persistence owner; re-review on feature touch)

Full machine inventory: `.tmp-tmar-domain-types.json` (type/namespace/file). Semantic review remains Architect-gated for borderline types.
