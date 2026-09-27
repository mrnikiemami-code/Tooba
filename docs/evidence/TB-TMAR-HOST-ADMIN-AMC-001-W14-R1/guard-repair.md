# Guard repair — W14-R1

## Added

`HostAdminAmcW14R1GuardTests`:

1. `ProductSeoDirectory_has_zero_expected_InvalidOperationException_control_flow` — no catch IOE / catch blocks; uses TrySlugifyFromName + TryNormalizeSlug + WorkspaceProductSlugInvalid.
2. `Domain_owns_single_slug_normalization_core_with_Try_and_throwing_APIs` — Try* + throwing APIs + private core; `pendingHyphen` only in Domain normalizer; ProductSeoDirectory has no algorithm copy.
3. `W14_seo_route_ownership_and_workspace_codes_preserved` — three Catalog SEO routes exactly once; Host SEO routes absent; workspace SEO codes unchanged.
4. `Host_Admin_remains_52_and_W15_not_started` — Admin *.cs == 52; ProductSeo/ProductMedia present; W15 task/evidence absent.

## Focused Domain Try* tests

`CatalogCategorySlugNormalizerTryTests` — valid ASCII/Persian parity, separator collapse parity, invalid → false, throwing API compatibility.

## Preserved

Existing `HostAdminAmcW14GuardTests` remain authoritative for W14 migration surface.
