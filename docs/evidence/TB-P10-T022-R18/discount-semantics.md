# TB-P10-T022-R18 — Discount Semantics

- promotional selling = campaign AuthoredPrice amount when applicable
- compare-at = Base amount only when Base > promo
- discount % = derived in UI from (1 - promo/compareAt); never stored
- promo >= Base → selling = Base, CompareAt = null (no fake strike)
