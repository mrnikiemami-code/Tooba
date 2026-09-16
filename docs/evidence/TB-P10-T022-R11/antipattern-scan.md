# Anti-Pattern Scan — TB-P10-T022-R11

| Anti-pattern | Status |
|---|---|
| Geometric previews in Variant Picker / Review | CLEAN — wizard uses `VariantLivePreview` only |
| Screenshot-only previews | CLEAN |
| Duplicate visual renderer | CLEAN — `renderSharedLandingSection` |
| Technical enum names in Admin primary UI | CLEAN — `طرح …` design names |
| Human name persisted as identity | CLEAN — only `variantKey` saved |
| Static fake carousel | CLEAN — production Swiper / rails |
| Second carousel library | CLEAN — Swiper only |
| Preview business mutations | CLEAN — click capture suppresses nav/ATC |
| Hardcoded counts outside Variant contract | CLEAN — `previewTargetItems` |
| Store/Template DB reads for picker | CLEAN — empty context + PreviewFake |
| Polling/timeouts in picker | CLEAN |
| Mounting all heavy previews at once | CLEAN — IntersectionObserver lazy mount |

Scan date: TB-P10-T022-R11 implementation.
