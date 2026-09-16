# Anti-Pattern Scan — TB-P10-T022-R8-R2

| Pattern | Status |
|---|---|
| Reusing Fashion Template assets for Preview-Fake | CLEAN — `preview-fake-data.ts` has no fashion-demo-media import; media from `/images/preview-placeholder/` |
| Template entity IDs in Store fake fill | CLEAN — `preview-fake-*` ids only |
| Template names/copy in Store fake fill | CLEAN — dedicated locale strings (محصول نمونه / …) |
| Hidden Store → Template fallback | CLEAN — PreviewFillPolicy never queries Template Catalog |
| Generic placeholder boxes | CLEAN — real Variant renderers + fake fill |
| Fake DB inserts | CLEAN — in-memory only |
| Duplicate fake renderer | CLEAN — same production Section/Variant path |
| Hardcoded single-locale strings in components | CLEAN — `preview-fake-locale.ts` |
| Third-party hotlinks | CLEAN — local SVG assets |
| Published Preview-Fake leakage | CLEAN — runtime published proof |
| Other-industry expansion | CLEAN |
| Cloning | CLEAN |
| Polling/timeouts/workarounds | CLEAN |

Verdict: **CLEAN**
