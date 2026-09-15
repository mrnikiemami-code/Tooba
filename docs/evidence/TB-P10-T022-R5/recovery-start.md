# TB-P10-T022-R5 — Recovery Start

## Git

- branch: `main`
- HEAD (start): `6b9ecc12412826bb74ee9b2a03af214b7a343b9a`
- origin/main: same
- Expected previous HEAD (task hint): `32888135ebf386522495063fff2af84415603cf3` (R4 pin; tree advanced with UX polish commits; no tracked divergence)
- Working tree: unrelated untracked `.tmp-*` / host out folders preserved

## Runtime

- Host :5088 — restarted for migration/seed
- FE :3000 — available

## Catalog families inventoried before coding

- Product / Category / Brand / localized_texts / product_media_references / product_categories / variants
- Banner: no Banner entity; landing section ConfigurationJson (`BannerShowcase`)
- R4 Fashion source: in-memory `fashion-demo-preview.ts` (replaced by Template Catalog)

## Decision

Safe to proceed — no RECOVERY_CONFLICT.
