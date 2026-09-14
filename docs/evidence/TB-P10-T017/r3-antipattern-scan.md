# Anti-pattern scan — TB-P10-T017-R3

| Pattern | Result |
| --- | --- |
| Page-specific theme token families | CLEAN — four global roles + allowed Card/Header/Footer |
| Page-by-page duplicate theme state | CLEAN — root `loadStorefrontAppearance` only |
| Route-specific palette definitions | CLEAN |
| Arbitrary hardcoded page/section wrappers | REPAIRED — shipping `w-full bg-white`, customer header/sidebar `bg-white`, login card, PDP shells, wallet-checkout `#F5F5F5`, blogs without shell |
| `!important` theme hacks | CLEAN for this repair |
| Screenshot-only CSS | CLEAN |
| Hydration suppression | CLEAN |
| Polling appearance | CLEAN |
| Per-section appearance fetch | CLEAN |
| Blanket replace of all white | CLEAN — cards/status/media kept; wrappers classified |
| CardSurface used as entire page background | CLEAN — page remains `bg-page` |
| Customer panel separate theme engine | CLEAN — same projection; `dark:bg-[#111]` notification override removed |

Customer panel notification inbox now uses `bg-surface` instead of a page-local dark hex.
