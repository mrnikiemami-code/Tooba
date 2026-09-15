# Migration plan — TB-P10-T018

1. Keep current Home seed/fallback until Architect accepts migration cutover.
2. Keep published Landing configs readable; `LANDING_SECTION_TYPE_MAP` adapts deterministically.
3. Home snake_case mapped via `HOME_SECTION_TYPE_MAP` without breaking seeded demos.
4. Shared Appearance/ProductCard/surface roles unchanged (LOCK-SF-001…220 preserved).
5. Admin composer evolves later onto shared registry — no breaking rewrite in T018.
6. Ranking sources stay honest (no fake BestSelling on Landing).
7. Rollout: adapter → dual-read → dual-write → deprecate legacy keys when accepted.
