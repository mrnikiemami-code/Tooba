# 12 — Seller Story CTA safety (TB-P06-T019-R1)

## Existing domain rules (`StoryRules.ValidateCta`)

Shared by admin and seller create/update/item paths:

| Rule | Behavior |
|---|---|
| Allowed types | `none`, `product`, `category`, `article`, `internal`, `external` |
| Forbidden schemes | `javascript:`, `data:`, `vbscript:` → reject |
| Target required | Non-`none` types require target; length capped |
| Media types | `image` \| `video` only (`ValidateMediaType`) |

No arbitrary HTML/JS injection surface in Story CTA fields.

## Seller path

Seller draft create/update uses the same `ValidateCta` / media validators via `StoryDirectory` seller mutations. UI does not add a bypass. Admin retains the same CTA model (broader operational use, same unsafe-scheme rejection).

Covered in `StoryFoundationTests` public/admin CTA rejection cases (existing suite).
