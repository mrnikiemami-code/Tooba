# Anti-Pattern Scan — TB-P10-T022-R9-R1

| Anti-pattern | Status |
|--------------|--------|
| Increasing timeout to hide stall | CLEAN |
| Spinner / UX hide of latency | CLEAN |
| Polling / magic retry loops | CLEAN |
| Disabling SEO | CLEAN — metadata/structured data retained |
| Cache without Store/locale isolation | CLEAN — tags include storeScope+locale+page |
| Global tenant cache | CLEAN |
| N+1 section HTTP / per-card FE queries | CLEAN — Host embeds section product cards |
| Duplicate metadata/page fetch chain | CLEAN — React `cache()` resolver |
| Unrelated refactor | CLEAN — scoped to Store Page SSR path |

**Verdict: CLEAN**
