# CQRS + MediatR Recovery Plan

## Current

- MediatR / ISender / IRequest: **absent** (zero PackageReference)
- Use-cases: `I*Directory` / Gateway Application Services in Infrastructure
- Authz seam: `I*UseCaseGuard` (explicitly without MediatR)

## Target

HTTP → `ISender` → Command/Query → Handler → Domain + Repository/Gates/Contracts

Approved MediatR: **12.5.0** (do not install in this planning task)

FluentValidation via MediatR pipeline behaviors.

### Pipeline responsibilities

validation · authorization · logging/telemetry · transaction (where appropriate) · idempotency · error normalization

### Strangler stages

| Stage | Pattern |
|---|---|
| A | Endpoint → Handler → existing Directory |
| B | Handler → Domain + Repository + Gates/Contracts |
| C | Directory narrows/removed when redundant |

No all-at-once Directory deletion.

## Directory classification (major)

| Directory | Owns today | Classification |
|---|---|---|
| CartDirectory | validation, quote orchestration, inventory reserve via gates, persistence | Wrap First |
| CheckoutDirectory | TransactionScope, multi-module orchestration, order write | Wrap First |
| PriceDirectory | AuthoredPrice CRUD + campaign prices | Keep (pricing BC) |
| CatalogDirectory | large catalog write/read + settings persistence | Split |
| MerchandisingCampaignDirectory | campaign membership CRUD | Keep |
| OfferDirectory / InventoryDirectory / PaymentDirectory / … | module write APIs | Keep / Wrap First |
| Host *Composer | read composition + some writes/decisions | Replace Later (Host waves) |
