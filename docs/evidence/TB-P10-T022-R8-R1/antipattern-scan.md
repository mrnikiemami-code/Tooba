# AntiPattern Scan

| Pattern | Status |
|---------|--------|
| generic placeholder boxes | CLEAN (dashed=0 on Store preview) |
| Template fallback in Store | CLEAN |
| fake DB inserts | CLEAN |
| replacement of real Store items | CLEAN (partial keeps real first) |
| separate fake renderer | CLEAN (same Variant) |
| magic per-component counts | CLEAN (variant cardinality) |
| published fake leakage | CLEAN |
| hardcoded one-language UI | CLEAN (fa/en/ar badge labels) |
| screenshot-only deception | CLEAN (runtime badges + DOM counts) |

Status: CLEAN
