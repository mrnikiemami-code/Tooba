# 13 — Composition runtime safety (TB-P06-T015)

| Scenario | Behavior |
|---|---|
| Unknown `sectionType` | Skipped / not rendered (switch default null) |
| Hidden section | Omitted from public composition GET |
| Empty product rail data | Section returns `null` (no fake products) |
| Invalid config JSON | Rejected on write; read path uses safe parse defaults |
| Missing composition API | Frontend falls back to default order |
| Forbidden builder keys | Rejected at domain validation |

No arbitrary HTML injection path from admin config into storefront DOM.
