# Frontend architecture locks — TB-TMAR-FE-BASELINE

Recorded in `docs/architecture/TMAR-architecture-locks.md`:

| Lock | Intent | Enforcement |
| --- | --- | --- |
| FE-ARCH-001 | Thin route files | review + future lint; characterization |
| FE-SIZE-001 | No new FE source >800 LOC | `frontend-source-size.guard.test.ts` + repo `TmarSourceSize*` |
| FE-SIZE-002 | Oversized shrink-only | same baselines |
| FE-SEO-001 | No client-only primary indexable content | lock + seo guard + critical-storefront |
| FE-BOUNDARY-001 | Shared must not depend on features (new) | import-boundary guard |
| FE-BOUNDARY-002 | Cross-feature via public boundary (new) | documented; deepen in FE-F2 |

