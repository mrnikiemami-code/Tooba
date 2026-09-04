# TB-P08-T016 — Error UX

## Code / contract (worker)

- `mapAdminErrorMessage` / admin-error-map covers content publish readiness, category depth, tag language/duplicate, comment transitions
- Normal UI must not expose Bad Request / content.* / localization.* / raw JSON / stacks
- FE asserts: list grid error mapping; comment moderation human messages

## Browser (parent)

- [ ] Force known failure (e.g. publish not ready) → human FA/EN message
- [ ] No raw HTTP/status jargon in toast/panel
