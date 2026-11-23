# ARCHITECTURE-PLAN — TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287

## Decision
A requestId identifies the immutable original normalized creation intent, independent of later Offer edits.
Replay of the identical original intent resolves the same OfferId (returning the CURRENT read model); a
changed intent on the same identity fails with `offer.create.intent_conflict` (409). Historical rows with no
recorded fingerprint fail closed.

## Root cause (source-grounded)
`CreateOfferHandler.MatchesCreationIntent` compared the replay to the CURRENT mutable `SellerOffer` fields
(SKU/status/return-policy/quantity). After a legitimate edit A→B, the original A replayed as 409 and changed
B replayed as 201 — an integrity defect (confirmed by TB-286 V4).

## Design (Offer-owned, additive)
- New nullable column `offer.offers.creation_intent_fingerprint varchar(64)` (additive migration source only;
  NOTHING backfilled; historical rows stay NULL).
- `CreationIntentFingerprint.Compute(...)` canonicalizes the create command (seller, variant, channel, trimmed
  SKU, effective status Draft/Active, effective return policy exactly as the domain stores it, custom window,
  min/max decimal canonicalized, trimmed requestId) with version tag `ci1`, then SHA-256 hex.
- `SellerOffer.SetCreationIntentFingerprint` records it once at create time; persisted atomically with the insert.
- Handler: replay branch compares the stored fingerprint (NULL ⇒ fail closed 409); the `PersistNewAsync` race
  branch compares the winner's stored fingerprint. The old mutable-field comparison is removed.

## Normalization
- SKU: trim, empty→null, case-sensitive (contract uniqueness is Ordinal).
- Status: transport validation is case-insensitive; effective value collapses to Draft/Active.
- Return policy: effective Default/Custom/NonReturnable and custom window exactly as `SetReturnPolicy` stores.
- Decimal: trailing zeros stripped (10 == 10.0 == 10.00). Create DTO exposes no decimal fields; these are
  included for completeness.
- requestId: trimmed; already encoded in the deterministic OfferId.

## Boundaries
Only Offer module files (`CreateOfferCommand.cs`, `SellerOffer.cs`, `SellerOfferConfiguration.cs`, migration,
snapshot) + new `CreationIntentFingerprint.cs`. No FE, Host, Cursor, root AGENTS, Pricing/Inventory code,
no live migration.

## Flow
HTTP (seller `/v1/seller/offers`, admin `/v1/admin/offers`) → auth → MediatR `CreateOfferCommand` → fingerprint
compare → `IOfferStore`/`OfferDbContext` → disposable PostgreSQL → independent SQL/GET reread.
