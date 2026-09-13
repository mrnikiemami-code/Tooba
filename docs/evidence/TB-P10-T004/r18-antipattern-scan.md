# R18 anti-pattern scan

CLEAN

- Frontend does not resolve Offer > Category > Store precedence
- Effective preview comes from `PreviewAsync` / `PreviewManyAsync`
- No Product-level override
- Seller PUT denied; no seller save API
- Settings composer never writes cycle `ExpiresAt`
- Inherit checkbox sends null, does not copy parent as override
- No raw config keys in editor
- No first-line policy shortcut
- R17 history still uses stored cycle snapshot
- No settings polling
