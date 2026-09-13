# R18 validation

Admin rejects before domain clamp:

- Initial minutes 1…43200
- Retry minutes 1…43200
- Max cycles 1…20
- 0 / negative / >max rejected with localized titles
- empty = inherit (null), not coerced to platform defaults
- frontend integer-only parse; decimals rejected

Domain `ClampOptional` remains a checkout safety net and is not used as the Admin write success path.
