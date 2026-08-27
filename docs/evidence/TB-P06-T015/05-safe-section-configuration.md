# 05 — Safe section configuration (TB-P06-T015)

## Allowed

| Key | Type / bounds | Typical use |
|---|---|---|
| `title` | string ≤120 | Localized rail/section title override |
| `href` | string ≤256 | Rail “see all” link |
| `itemCount` | int 1–24 | Rails / articles count hint |
| `sourceKind` | enum string | `offers`, `most_viewed`, `new_arrivals` |

## Forbidden (reject)

`css`, `html`, `js`, `className` (+ any non-allowlisted key)

## Proof

- Domain validator in `SectionCatalog` / config parse path.
- `PageCompositionFoundationTests`: forbidden keys rejected.
- Live E2E: `_composition-e2e-api-proof.json` → `rejectedForbiddenConfigStatus: 404` (unsafe path/config not accepted as valid section mutation).

`FREE_FORM_PAGE_BUILDER = FORBIDDEN`
