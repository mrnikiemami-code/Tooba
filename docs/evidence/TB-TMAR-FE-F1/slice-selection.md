# Slice selection — TB-TMAR-FE-F1

## Selected: admin-languages

| Criterion | Evidence |
| --- | --- |
| Size | `language-api.ts` + `language-list.tsx` + thin `languages/page.tsx` |
| Ownership | Canonical Language/Locale admin registry |
| Visual risk | Low (admin grid + edit modal) |
| Giants | Not required |
| SEO/storefront | Admin-only |
| API surface | Already separate from `admin-api.ts` (imports only shared result header) |
| Tests | `language-identity-lock.test.ts` + new characterization |

## Rejected

- category-admin / product-workspace — CRITICAL giants
- checkout/payment/builder — high risk
- campaigns — larger workspace already foldered; not the simplest first pattern proof

## Rollback

Git tip before migration commit; feature path removable with import revert.
