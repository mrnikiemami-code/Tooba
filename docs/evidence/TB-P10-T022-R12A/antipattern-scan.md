# Anti-pattern scan — TB-P10-T022-R12A

| Gate | Result |
|---|---|
| New schema family | PASS — reused Template* / StoreTemplate only |
| Operational Catalog changes | PASS — seed writes Template* only |
| Fashion asset reuse | PASS — `/images/template-{auto-parts,building-materials,tools-hardware}/` |
| Unrelated generic media | PASS — industry-tinted local SVGs→JPEG |
| Hardcoded template renderer | PASS — shared `StorefrontLandingSections` |
| Store/Sample mixing | PASS — sample = Template Catalog; store = operational + PreviewFake policy |
| PreviewFake in Sample mode | PASS — sample loaders never inject PreviewFake |
| Duplicate in-memory template source | PASS — Host preview + FE page builder from registry |
| Non-idempotent seed | PASS — key presence + parity early-return |
| Empty Page after Use Template | PASS — `buildTemplateSectionPayloads` key-driven (R10) |
| Second carousel library | PASS — unchanged |
| Polling/timeouts/workarounds | PASS — none added |
| Invented T023 / R12B | PASS — not started |
