# Capability map (skill §5)

Capabilities are business responsibility axes taken from the module's **existing** vocabulary
(Contracts/Operations, Domain aggregates, endpoint audiences, W0 ownership map). No name was invented
mechanically from Command type names.

## Capability inventory

| Capability | Business responsibility | Owner | Physical home |
|---|---|---|---|
| **Payouts** (single module capability) | Seller payout request lifecycle, admin payout processing/retry, seller settlement balance/entries/statements read models, admin settlement balances + payout queue + DB-native admin grid | Settlement | `Application/Payouts/{Commands,Queries,Models,Ports}`, `Infrastructure/{Directories,Queries,Gateways}`, `Endpoints/{Seller,Admin}` |
| Composition (shared, not a capability) | typed-fault → `Result` seam used by every command handler | Settlement | `Application/Composition` |
| Validation (shared, not a capability) | transport-shape validation of the 4 endpoint-reachable client-shaped requests | Settlement | `Application/Validation` |
| Boundary vocabulary (shared, not a capability) | stable error codes, bilingual resources, published integration events, cross-module ports | Settlement | `Contracts/{Errors,Resources,Events,History,Operations}` |
| Persistence / integration | `settlement` schema, outbox, inbox, snapshot adapters/bridges to Order/Payment/Returns/Party | Settlement | `Infrastructure/{Persistence,Messaging,Handlers,Adapters,Bridges}` |
| Observability | 3 counters on the single `ToobaTelemetry.Meter` | Settlement | `Infrastructure/Observability` |

## Audience axis (Endpoints) versus capability axis (Application)

The endpoint surface is audience-shaped because the HTTP contract is audience-shaped:

| Audience | Route group | Routes | Capability slice |
|---|---|---|---|
| Seller | `/v1/seller/settlements` | `GET /balance`, `GET /entries`, `GET /statements`, `GET /payout-requests`, `POST /payout-requests` | seller read models + payout request |
| Admin | `/v1/admin/settlements` | `GET /balances`, `GET /payout-queue`, `POST /payout-queue/query`, `POST /payout-requests/{payoutRequestId:guid}/process`, `POST /payout-requests/{payoutRequestId:guid}/retry` | admin read models + payout processing |

`Audience` is an Endpoints concern only. It is deliberately **not** mirrored into Application
(that is what produced the retired `Validators/{Admin,Seller}` split) and it is not a second
capability: both audiences operate the same Payouts capability.

## Why `Payouts` is the only capability folder

- `Contracts/Operations` + `Contracts/History` expose one bounded context: payout/settlement
  bookkeeping for sellers and admin operators.
- Domain aggregates (`SettlementEntry`, `PayoutRequest`, `PayoutAttempt`, `SettlementAccount`,
  `SellerPayoutProfile`, `SettlementStatement`, `CommissionPolicy`) are all facets of that one
  context — there is no second aggregate cluster with an independent lifecycle.
- All 10 endpoint-reachable requests read/write that one context.

Forcing additional capability folders (e.g. splitting `Balances` / `Statements` / `PayoutRequests`)
would be mechanical invention (skill §5 hard rule 5) and would fragment a single cohesive use-case
family across folders.

## Cross-capability shared leaves

| Leaf | Shared by | Justification |
|---|---|---|
| `Application/Composition` | all 3 command handlers | one fault seam; duplicating it per handler would be `OVER_SPLIT` |
| `Application/Validation` | 2 audiences, 4 requests | one transport validation surface; the repository pattern uses a shared `Validation/` leaf (Returns / Promotion / AddressBook precedent) |
| `Contracts/Errors` + `Contracts/Resources` | every layer + other modules consuming codes | single canonical code home + bilingual resources |
