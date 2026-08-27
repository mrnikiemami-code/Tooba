# 08 — Page composition i18n (TB-P06-T015)

| Aspect | Behavior |
|---|---|
| `PageDefinition.Locale` | nullable; `null` = all / default scope |
| Admin/Public APIs | optional `locale` query |
| Config `title` | optional override string (safe); not `TitleFa`/`TitleEn` columns |
| Storefront chrome | Existing T014 cookie locale (`tooba_locale` fa\|en) + RTL/LTR |
| Section renderers | Reuse Shopeiva-locked Persian-first chrome; EN panel strings not required for composition MVP |

## Separation preserved

`Locale != Market != Currency` (T014 asserts remain valid). Composition does not encode market/currency into section types.
