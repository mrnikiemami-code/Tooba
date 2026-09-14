# TB-P10-T006 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| arbitrary color input | CLEAN — no `type="color"` |
| arbitrary CSS/HTML/JS from Admin/DB | CLEAN |
| hex persisted instead of PaletteKey | CLEAN — PUT `{ paletteKey }` only |
| frontend-only persistence | CLEAN — Host Catalog singleton |
| localStorage as appearance SoT | CLEAN — only admin actor id |
| global cache flush | CLEAN — `Invalidate` one scope |
| sleep/wait for TTL | CLEAN |
| full-page reload as save mechanism | CLEAN |
| duplicate appearance state | CLEAN — one registry, one projector |
| per-card appearance API | CLEAN |
| polling | CLEAN — `cache: "no-store"` SSR load only |
| runtime Tailwind class from palette values | CLEAN |
| brand redefines danger/success/warning | CLEAN |
| hard-coded Store ID on write | CLEAN |
| cross-store update path | CLEAN |
| dark mode painting | CLEAN — ThemeMode preserved, not exposed |
| Product Card skin | CLEAN |
| Landing/Page builder | CLEAN |
