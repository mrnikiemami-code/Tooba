# TB-P08-T016 — Error UX

## Code / contract (worker)

- `mapAdminErrorMessage` / admin-error-map covers content publish readiness, category depth, tag language/duplicate, comment transitions
- Normal UI must not expose Bad Request / content.* / localization.* / raw JSON / stacks
- FE asserts: list grid error mapping; comment moderation human messages

## Browser / API (parent)

- [ ] Force known failure (e.g. publish not ready) → human FA/EN message  
  Not exercised this session.
- [ ] No raw HTTP/status jargon in toast/panel  
  Not exercised this session.

## Related API observation (not toast UX)

- Authors picker: **400** without required `activeOnly` query param (shell agent).
