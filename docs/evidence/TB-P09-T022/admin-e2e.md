# Admin E2E — TB-P09-T022

From T021 + final-gate expectations (runtime A/B fills live proof):

- Single-seller: `create_consolidated_package` capability absent; `consolidatedPackages` empty (section would stay hidden).
- Multi-seller (≥2): eligible Created shipments selectable; create package with 2+ sellers.
- Package card: human `MP-…` number, status, provider/tracking.
- Member shipment cards remain visible; conflicting ops locked with FA explanation.
- No broad visual redesign; backend capabilities authoritative.
