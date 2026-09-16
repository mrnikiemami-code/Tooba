# Focused validation — TB-P10-T022-R12A

| Check | Result |
|---|---|
| 3 StoreTemplate keys unique | PASS (`auto-parts`, `building-materials`, `tools-hardware`) |
| each exactly 8 root categories | PASS (Host tests + runtime API) |
| all roots 3-level | PASS (2 mid × 2 leaf) |
| each exactly 15 products | PASS |
| translation resolution | PASS (fa-IR TemplateLocalizedText / CategoryTranslation) |
| media isolation | PASS (`/images/template-{key}/`, no fashion-template) |
| seed idempotency | PASS (double ApplyAsync) |
| preview route shared renderer | PASS (`StorefrontLandingSections` + IndustryTemplatePreviewView) |
| Sample purity | PASS (`isPure=true`, origin `*-template-catalog-persisted`) |
| Store preview Template-hit=0 | PASS (runtime store origin + purity) |
| Template Apply full composition | PASS (Use Template → R10 editor) |
| critical-storefront | PASS |
| recovery-staleness | PASS (started at expected HEAD) |
| git diff --check | (run at commit) |

Host tests: `IndustryBatchATemplateCatalogT022R12ATests` — 8 passed.

FE: `admin-builder-ux-r12a.guard.test.ts` + r4 + industry-templates.guard — 18 passed.
