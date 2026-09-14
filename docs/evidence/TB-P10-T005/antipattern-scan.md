# TB-P10-T005 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| arbitrary CSS from DB | CLEAN — tokens are RGB triples from registry |
| arbitrary HTML from DB | CLEAN |
| arbitrary JS from DB | CLEAN |
| per-store generated frontend build | CLEAN |
| per-store hard-coded theme branch | CLEAN |
| theme DB query per Product Card | CLEAN — card does not fetch appearance |
| runtime Tailwind class from DB | CLEAN |
| brand overriding danger/success/warning | CLEAN |
| duplicate theme providers | CLEAN — existing ThemeProvider only |
| client-only theme flash | CLEAN — `:root` equals default palette |
| global mutable appearance across tenants | CLEAN — scoped cache |
| placeholder Admin settings | CLEAN — none added |
| Page Builder in this task | CLEAN |
| Product Card Skin in this task | CLEAN |
