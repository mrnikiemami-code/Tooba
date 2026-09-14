# TB-P10-T008 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| duplicate ThemeProvider | CLEAN |
| page-local dark registries | CLEAN |
| per-page dark hex branches | CLEAN — one globals remap |
| client-only visible flip | CLEAN — SSR + blocking script |
| polling | CLEAN |
| toggle writes Store DB | CLEAN |
| localStorage as Store SoT | CLEAN — device preference only |
| competing theme keys | CLEAN |
| desktop/mobile split state | CLEAN |
| status bound to brand | CLEAN |
| CSS/JS from DB | CLEAN |
| layout change | CLEAN |
| skins / page builder | CLEAN |
