# Residual cleanup — TB-P07-T034-R1

## Problem
After T034, Admin Product grid total was **287** while seed reported **283**.
The 4 extras were legacy **Published** rows titled `محصول فروش T021` (no `demo-prod-*` slug), so seam-only product reset left them behind.

## Fix
`CatalogDemoResetService.ResetAsync` now deletes **all** Catalog Products (and Catalog-owned dependents) in non-production demo reset, not only `IsDemoOrJunkProductSlug` matches.

Still fail-closed:

- Production blocked
- Development/Testing only
- `AllowResetAndSeed=true` required

Categories/brands/tags/attrs/media continue to use demo seam markers.

## Live proof
First R1 reset removed **287** products (283 demo + 4 T021), then reseeded **283**.
Second reset removed **283** and reseeded **283** (identical).
Junk title search (T021/R3/schema/…): **0** hits.
