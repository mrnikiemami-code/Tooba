# Source Purity

- Sample origin: `fashion-template-catalog-persisted`
- Store origin: `operational-store-catalog`
- Store fake fills: in-memory `preview-fake-*` only; Template Product/Category/Brand/Banner hit = 0 for fill path
- Sample: PreviewFillPolicy disabled
- Partial fillers fixture injects non-Template store-like items only when `fixture=partial-fillers`
