# Architect review — 243 — 2026-10-29
Source7ea79556fa91fa8fe00b5fa45b349023d8ae7c88; result4f21dc069 and current main5e73e899cdb10f2812e57ec014e346a7b026c27c.
Root reviewed GitHub source/committed receipts, did not execute Windows/HTTP/DB/tests.
Decision: PARTIAL / NOT ACCEPTED AS COMPLETE. Keep demonstrated original SingleStore60-minute14–15 Order and
normal unsubmitted review cart as real progress; do not erase it or call all seven243 delivery rows complete.

Blockers from worker's own receipts:
1. Concurrent StartPayment second branch500 platform.unexpected. Owning exception/trace not provided.
   "First won" does not certify duplicate/replay row. No cause inferred from old238 unknown22001.
2.60->120 publication fresh projection0slots unexplained. Need actual candidate/reason/context; do not claim
   duration120 calendar verified.60restored8slots and old order unchanged are bounded facts only.
3. Existing post:standard was deactivated to force ResolveCatalogBindings into post/NULL, orphaning DEV230.
   This is a collateral configuration workaround, not completion of a legitimate full binding resolver.

New weight-source blockers confirmed in code:
- CatalogDirectory selects every Number definition with Unit g/kg, ignoring stable Code and IsActive.
  CatalogAttributeDefinition documents Unit as DISPLAY metadata; unit alone does not identify shipping weight.
- variantOverrides resolved separately, then grams==null triggers product fallback even for PRESENT invalid/
  conflicting variant. Violates the new port's own unknown/override guarantee; false known weight can price a tier.
- override semantics are not per definition: any valid variant values discard other product definitions.
- kg multiplication is not protected against decimal overflow; Order ResolveGroupWeightGrams total+= is unchecked.
- no new focused Catalog weight/coordinator facts committed; only a test-double method returning empty map.
Weight must be validated at owning semantic source, not inferred from display units or healed from invalid data.

UI source improvements retained: locale messages/useLocale, date formatting call, nontruncated range element.
No new screenshots or user Visual ACCEPT requested/inferred. No evidence of full DOM/keyboard/counter execution
in committed243 receipts; focused14/14+4/4/typecheck and Order.Infrastructure build are worker receipts only.
Order final read still uses compact stored shipping evidence, not repricing.

Issued correction243-R1 for SAME original-store delivery: fix actual weight authority/precedence/overflow,
restore post:standard via normal API and correct binding resolution, diagnose actual race500 and120read.
No new commissioning/DB/ports/screenshot campaign/automatic244. Preserve original user sessions/DB/orders and
unsubmitted review cart. All3TOPs must be current (START-HERE was left ISSUED in243).
Original deadlineMISSED; no replacement time promise, no PSP/paid/provider exactly-once or full-release claim.
