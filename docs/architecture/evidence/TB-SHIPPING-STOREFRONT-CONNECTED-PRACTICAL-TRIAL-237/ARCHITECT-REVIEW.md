# Architect review — TB237 — 2026-10-28

Actual reviewed commit affef402945dbb80772b33137a14e465d07f149a. Root read patches, complete shipping page/API,
captured intent builder, actual backend input/preview DTOs and real payment handoff/final-click adapter.
Root did not execute Windows builds/tests. Verdict: PARTIAL / NOT CONNECTED; main user outcome rejected.
No runtime/URL/browser/migration/stock/payment proof exists. Source wiring is not a delivered practical flow.

## Retained
The mandatory goods echo no longer permits omission/blank in the backend. Whole-cart coupon-backed Applied
evidence guard is present; partial applicability remains possible without matching/discount policy in Order.
Strict string decoding for capture confirmation tokens and page-controlled choices are useful scaffolding.
Backend guard behavior was built only, not actually exercised by the worker; do not mark its unrun negatives green.
The claim "before any quote" for goods echo is inaccurate: AssertGoodsEcho is called after ResolveAsync/composition.
Do not reopen pricing/owner/calendar core. Retain compact236 storage design.

## Concrete blockers to real connection
1. saveShippingSelection serializes confirmationSnapshotId/confirmationSourceRevision/confirmationSelectionVersion/
   confirmationAreaGroupId/confirmationRateRowId under capturedGroups. Actual backend StorefrontCapturedGroupSelection
   binds ExpectedSnapshotId/ExpectedSourceRevision/ExpectedSelectionVersion/ExpectedAreaGroupId/ExpectedRateRowId.
   Confirmation* are READ names, expected* are WRITE names; the five expected values currently arrive absent.
2. persistAndContinue sets top-level shippingMethodCode to capturedPrimary.shippingMethodCode. Captured writer
   explicitly rejects ANY nonblank top-level legacy ShippingMethodCode/date/window. API forwards those fields.
   All captured saves therefore hit intent_invalid before useful selection; do not weaken the server.
3. capturedSelectionComplete filters groups with no methods; buildCapturedSelectionIntent skips those groups.
   Server requires ALL cart groups selected. UI must keep unavailable groups visible and block Continue with their
   reason; no partial-cart shipping intent. It also does not actually check all five confirmation tokens before
   building, despite its comments/report.
4. canContinue/build uses raw projection while cards use pending/failed masked view. Context key sets snapshotId:null
   and only root revision (preview DTO has NO SelectionRevision property); per-candidate capture/goods/calendar
   changes can retain old choices. Use the CURRENT confirmed response + full per-group provenance/goods and coupon/
   destination/cart generation. Root revision comes from actual draft/accepted payment receipt, not fabricated
   preview.selectionRevision; absence vs malformed must be distinct. No blind "invalid token ->0" fallback.
5. No synchronous save in-flight guard was added; React setBusy alone does not block two calls same tick.
   Coupon storage read on render does not itself subscribe/refetch a new coupon context.
6. Actual payment path is app/payment/storefront-payment-handoff.tsx -> app/storefront/storefront-final-click-api.ts.
   pay() passes only cartId/cartVersion/method. Final-click input/body has NO expectedSelectionRevision.
   Captured final submit therefore cannot succeed through this UI. Adapter also uses coupon?.trim() || stored,
   which resurrects a cleared coupon. Correct existing path; don't create another payment page/commit-on-Continue.
7. The readonly banner is still unconditional for captured previews, no gate hint UI implemented, and recorded230
   empty-key/in-flight behavior was not touched. Local runtime was never even started, so these regressions were
   hidden by parser-only fixtures rather than caught by actual server bodies.

## Process/evidence
Worker reports6/6 intent units/typecheck/Infrastructure build; no required critical-storefront check or native
screenshots. Existing full-owner relational fixture now omits mandatory goods echo; it is expected red and must
receive real server facts, never fake zero or relaxed validator. Broad suites must not substitute for running app.
95min vs45 without starting runtime is noncompliant. Original watched checkout/window preservation is retained.
The worker again sent literal probe to result transport despite explicit canonical prohibition; record this as
a transport/process violation, NOT a real result/permission to send more diagnostic posts. Current complete
message+actual source support review; no root Bridge calls or transport-repair task is authorized/needed here.

## Next action
Issue237-R1 ONLY: reuse existing D:\\temp\\tb237 worktree, BOOT local Host/FE FIRST, fix the exact wire names/
legacy mixing/all-group confirmation/payment token on same surfaces, exercise real browser PUT->payment preview
and actual final endpoint. User already permits normal nonproduction setup/data/migrations. No new audit/engine/
schema or production activation. End with a usable connected URL and explicit stage receipts; Marketplace remains
a final requirement, not a reason to delay the first configured SingleStore URL. All-owner backend accepted scope
remains; no physicalDB/PSP/visual/fullrelease claim. Preserve USER unsaved original window via isolated worktree.
