# TB-P10-T005-R1 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| manual schema / ad-hoc CREATE TABLE | CLEAN — EF migration only |
| runtime SQL shortcut for final schema | CLEAN |
| second appearance endpoint | CLEAN — still `GET /v1/storefront/appearance` |
| hardcoded per-store branch | CLEAN |
| client-side-only theme patch | CLEAN — SSR root vars |
| forced browser reload as theme | CLEAN |
| polling | CLEAN |
| sleep/timing hacks | CLEAN |
| duplicate Store appearance state | CLEAN |
| hidden fallback masking broken API | CLEAN — live HTML `data-storefront-scope=tenant:store-alpha` |

CLEAN.
