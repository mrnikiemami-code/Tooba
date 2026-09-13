# TB-P10-T004-R24-R1-R1 — Recipient canonicalization

Single Host helper: `StorefrontRecipientNames`.

| Condition | Display / snapshot RecipientName |
| --- | --- |
| FirstName and LastName both non-empty | `FirstName + " " + LastName` |
| else legacy RecipientName non-empty | that RecipientName, unsplit |
| else | empty / `مشتری توبا` where that fallback already existed |

`ResolveExplicitOverLegacy`:

- request First+Last both present → those win (request RecipientName is only a compatibility mirror)
- otherwise → saved-address / historical names
- request RecipientName alone never overrides the book

Not done:

- parse / split RecipientName
- infer surname boundaries
- overwrite historical book rows
- frontend-only precedence for Payment / Order

New address (no SavedAddressId): FirstName and LastName remain required; RecipientName is the deterministic first+last mirror.
