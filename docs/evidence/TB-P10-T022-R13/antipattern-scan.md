# Anti-pattern scan — TB-P10-T022-R13

| Gate | Result |
| --- | --- |
| geometric preview remnants | CLEAN — removed unused `VariantPreviewCanvas`; picker uses production live previews; selector uses live iframe for all 10 packs |
| duplicate preview renderer | CLEAN — shared `IndustryTemplatePreviewView` / Fashion shared view |
| duplicate Home/Landing editor | CLEAN — one composer |
| Store/Template data mixing | CLEAN — Sample/Store/Published isolation guards PASS |
| Template asset reuse in PreviewFake | CLEAN — `/images/preview-placeholder/` independent |
| operational Catalog mutation from Template Apply | CLEAN |
| raw JSON SEO editor | CLEAN |
| per-section permanent SEO step | CLEAN |
| client-only reorder persistence | CLEAN — canonical server order |
| divergent drag/arrow ordering | CLEAN |
| second carousel library | CLEAN |
| N+1 / request waterfalls | CLEAN — R9-R1 path retained |
| magic timeout/retry/polling | CLEAN |
| hardcoded languages | CLEAN — language registry |
| exposed enum/GUID/internal identifiers in ordinary UX | CLEAN |
| first-item/first-seller shortcuts | CLEAN |
| suppressing errors to pass tests | CLEAN |
| template-specific renderer forks | CLEAN |
| accidental Admin theming from Storefront palette | CLEAN — Admin chrome tokens; Storefront theme only in embedded preview iframes |
| invent TB-P10-T023 / cloning | CLEAN — not started |

Verdict: **CLEAN**
