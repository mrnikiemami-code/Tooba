# Architect review — TB-231-R2 — 2026-10-27

Reviewed main21c225ff68c959511253172b303f8f18ff50e638 and diff sincea74fadd921bff4ddea2d3499429f82ef8cf76821.
Verdict: ACCEPTED for the corrected pending-migration basic shape checks and scoped provider proof.
This closes231 compact VALUE/TRANSFER/STORAGE within the recorded limits; no live writer, historical endpoint,
selection/commit or deployment acceptance.
Architect read actual migration and direct provider test changes. No local PostgreSQL execution by root;
RED4/10 and GREEN10/10 are worker execution, not independently rerun by Architect.

Confirmed: explicit ISNOTNULL for both optional day bounds, nonblank space-trimmed Store key, nonempty
required/optional-present UUIDs, uppercase three-letter currency regexp, existing22scalar columns/index/
all-or-none constraints preserved. SQLSTATE23514 and exact constraint checked by focused facts on both tables.
Tests query a subset of intact row fields; do not inflate this into independent per-column tamper proof.
PostgreSQL numeric coercion beforeCHECK and btrim(space-only) remain explicit limits. Application exact money
and owner validation remain required. Broader stale EF snapshot/future scaffolding remains uncertified.
Migration20261027150000 remains WRITTEN/sharedNOTAPPLIED. No old shared history changed. No runtime/user window
action was authorized by or performed for this review.

NEXT IS REAL TRUSTED SELECTION PERSISTENCE, NOT ANOTHER UNUSED COMPOSER.
Source inspection of current StorefrontShippingService shows ordinary selection replacement CLEARs groups,
SaveAsync, ADDs groups, SaveAsync: this is TWO commits without an enclosing atomic operation. Captured evidence
must not adopt that sequence as though already atomic.
Also the normal final-payment path is StorefrontStartPaymentComposer -> StorefrontShippingService.CommitAsync
-> StorefrontCheckoutService.SubmitAsync; updating only an obsolete frontend commit button would miss the
actual path.232 implements selected shipping DRAFT persistence only; final commit/payment staynot_ready.
Existing CartShippingDraft has no captured-selection CAS token; same cart version cannot protect simultaneous
changes to date/method.232 must guard accepted selection replacement without losing unrelated caller state.
AddressBook owned read currently has text Country/Province/City; do not trust an unrelated cheap destination
tuple from browser. Reuse existing Geography IGeographyLookup verified/unique address resolution and refuse
unknown/ambiguous/mismatched address identity, rather than silently quoting a different delivery address.

Current read-only UI, normal test account/cart and dedicated USER review window remain untouched.
Known empty-key retry/in-flight wait, cross-owner label map/decoder and Offer API proof gaps remain named.
Not_ready remains until later final-order evidence/revalidation/payment-payer proof. No authored JSON perorder.
