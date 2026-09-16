# Anti-pattern scan — TB-P10-T022-R12B

| Gate | Result |
| --- | --- |
| New schema family | CLEAN — reused Template Catalog tables |
| Operational Catalog mutation | CLEAN — seed writes Template* only |
| Fashion / Batch A media reuse | CLEAN — `/images/template-tile-ceramic|interior-decor|home-appliances/` |
| Hardcoded per-template renderer | CLEAN — shared IndustryTemplatePreviewView |
| Store/Sample mixing | CLEAN — Sample catalog origin; Store PreviewFake only |
| PreviewFake in Sample | CLEAN |
| Duplicate in-memory template source | CLEAN — persisted StoreTemplate packs |
| Non-idempotent seed | CLEAN — Host tests re-apply |
| Empty Apply | CLEAN — registry sectionPresetList materializes via R10 |
| Second carousel library | CLEAN |
| Polling/timeouts workarounds | CLEAN |
| R12C started | CLEAN — not executed |

Verdict: CLEAN
