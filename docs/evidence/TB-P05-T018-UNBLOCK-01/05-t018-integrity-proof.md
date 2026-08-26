# 05 — T018 Integrity Proof

## Product / Home

- `StorefrontHomePage` still exposes `HomeCategories`, `BestSellerColumns`, `MostViewedProducts` (`StorefrontModels.cs` / `StorefrontComposer.cs`).
- Frontend maps `homeCategories` and renders Shopeiva-ordered Home sections (`storefront-home.tsx`, `page.tsx`).
- Home rail uses ≤20 categories via `SelectHomeCategories`; full Catalog is not dumped on Home (`23-category-home-vs-catalog-proof.md` retained).
- No Home redesign in this unblock; no PDP / Mega Menu product edits.

## Evidence on `origin/main`

All 23 T018 evidence paths present under `docs/evidence/TB-P05-T018/` on HEAD `8482124` (markdown + optimized PNGs 02–10, 14–20).

PNG sizes after optimization: ~191–517 KiB each (review-useful; not original 15–21 MiB pack).

## Commits carrying T018

| Commit | Role |
| --- | --- |
| `f77cc4a` | Implementation + markdown evidence |
| `6a7dc13` / `8482124` | Screenshot batches (transport only) |

Original monolithic local SHA `1497690` was rewritten **only while unpushed** to shrink blobs; product intent preserved.
