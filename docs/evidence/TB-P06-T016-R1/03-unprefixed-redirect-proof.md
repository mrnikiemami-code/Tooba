# 03 — Unprefixed redirect proof (TB-P06-T016-R1)

## Purpose

Prove unprefixed public URLs do not remain indexable duplicates: they **308** permanently to the default locale (`fa`) with path + query preserved. Invalid locale prefixes must **404**, not soft-redirect.

Machine source: `_acceptance-proof.json` → `redirects.*`

## Redirect matrix

| Request | Status | `Location` | Notes |
|---|---|---|---|
| `GET /` | **308** | `/fa` | Root → default locale Home |
| `GET /blogs` | **308** | `/fa/blogs` | Blog listing |
| `GET /products` | **308** | `/fa/products` | Product listing |
| `GET /products/demo-game-3` | **308** | `/fa/products/demo-game-3` | PDP slug preserved |
| `GET /products?q=demo&page=1` | **308** | `/fa/products?q=demo&page=1` | **Query string preserved** |
| `GET /fr/products` | **404** | — | Invalid locale prefix; not redirected to `/fa` |

## Policy confirmation

| Rule | Observed |
|---|---|
| Status code | **308** Permanent Redirect (not 307/302) |
| Default locale | `fa` |
| Query preservation | Proven via `q=demo&page=1` |
| Prefixed paths | Do **not** redirect (see `02` — direct 200) |
| Invalid locale | **404** (honest failure) |
| SEO intent | Unprefixed public URLs cannot compete as alternate indexables |

## Body / Location evidence

Redirect responses carried Location path in the body snippet field of the probe dump (e.g. root bodySnippet `/fa`), matching the `Location` header.

## Panels (not redirected into locale prefix)

Account/ops surfaces remain intentionally unprefixed (`/admin`, `/customer-panel`, `/vendor-panel`) per T016 route map. Redirect policy applies to **SEO-public** storefront paths only.

## Verdict

Unprefixed → locale-prefixed **308** strategy is proven, including query preservation and invalid-locale 404.
