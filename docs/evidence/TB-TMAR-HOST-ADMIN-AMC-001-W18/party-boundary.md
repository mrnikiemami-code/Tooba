# W18 — Party boundary

## Current Host debt

`Tooba.Host.Admin.ProductWorkspaceComposer` injects `Tooba.Party.Application.IPartyLookupGateway`.

## Canonical W19 target

| Item | Value |
|---|---|
| Target type | `IPartyLookup` |
| Namespace | `Tooba.Party.Contracts` |
| Path | `src/backend/Modules/Party/Tooba.Party.Contracts/IPartyLookup.cs` |
| Related result | `PartyLookupResult` (same file) |

## Registration (already exists)

`PartyModule` registers both:

- `IPartyLookupGateway` → `PartyDirectory`
- `IPartyLookup` → `PartyDirectory`

Certified modules (Order, Offer, Cart, Settlement, …) already consume `IPartyLookup`.

## W18 action

**Document only.** Do **not** change Host `ProductWorkspaceComposer` in W18 (default). Live adoption is W19 (mechanical boundary correction when composition moves).

## Constraint

`ProductWorkspace.Application` must never reference `Party.Application` — `Party.Contracts` only when needed.
