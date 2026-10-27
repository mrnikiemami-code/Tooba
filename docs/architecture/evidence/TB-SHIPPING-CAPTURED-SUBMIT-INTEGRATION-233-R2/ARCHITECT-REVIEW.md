# 233-R2 Architect review — mechanical submit integration accepted, final acceptance NOT accepted

Reviewed main2ef0125cd191640bb05caf92f51e52d3d905b070 (source parent1d2106dd2) on2026-10-27. Root read actual acceptance/core/process/fixture/OrderModule registrations, existing PrepareShippingAsync and GeographyPlaceLookup.LegacyResolution. Worker-reported124/124/builds are NOT root execution.

## Accepted within narrow scope
Reader-free core removes the source DI cycle; actual registrations include it. Plan/facts group GUID keys and command-based facts validation corrected. Duplicate-offer unused dictionary removed. Acceptance now inside existing TransactionScope, Order root FOR UPDATE compares expected selection revision. Real public CheckoutDirectory.SubmitAsync fixture writes/reads two Order groups, post-write barrier rollback and ordinary idempotent replay exist. These prove mechanical integration with controlled Contracts, not a production-safe checkout or all stated scenarios.

## Corrections to evidence (source-confirmed)
- Positive seed A is post/standard, B is tipax/express; it asserts PointerCalls==2. Thus NOT the required same-binding/one-pointer case. Both fake pointers returning SAME SnapshotId for two different bindings is not a coherent real immutable Shipping capture.
- Both groups use SAME ReceivingDate, whole-minute09..17, no fractional ticks; constants show no100ns variation or independent weekday hours.
- Prices are registered99/198, Taxes.Rate=0; 99/198 outputs are not evidence of100-10+9 /200-20+18 promotion-and-tax calculation.
- Replay test never mutates _pointers.Revision before second submit. It proves ordinary replay, not replay AFTER publication change.
- DI test manually registers equivalent types; it does not call real OrderModule.AddServices. Source cycle repair accepted, full module-container certification is not executed.
- Refusal test asserts nonblank error.Code, not exact expected storefront code. Scenario"root" omits root bump, not a concurrent winner. All these claims need append-only correction.

Architect clarification: same owner+binding+destination must resolve the SAME area group. Earlier task wording about independent area/row must NOT be read as demanding different AreaGroupId at the same destination. Required independence is each group's own rate row/fee and selected date/hour on ONE shared week; never fabricate conflicting coverage.

## Remaining concrete blockers
1 Real Geography.ResolveLegacyAsync explicitly returns Unresolved on countryCode:null before any read. Current final resolver always passes null; permissive fake hides a real ALWAYS-REFUSE integration bug.
2 Saved address is not re-read/owned at final acceptance. Additionally existing PrepareShippingAsync converts SavedAddressId to null in its prepared Shipping snapshot, so232writer loses the source saved-address id before persisting draft. Preserve explicit provenance via a narrow typed server placement field, not guessed data/backfill.
3 Active service/option catalog check still absent; final gate only tests date count nonnull, not valid range. Quote SnapshotId/FormatVersion and pointer binding checks incomplete.
4 Row lock is source-confirmed but real contention/selection advance not tested. Point-in-time Shipping observation is not distributed serializability.

233-R3 finishes address/country + missing binding checks and corrects the existing integration proof, including bounded two-context contention. Coupon remains explicitly unsupported: do NOT expand into coupon parity now. Public Commit/PaymentDraft CLOSED, all new gatesOFF, shared migrations/runtime/user-window untouched. No fullcheckout/VisualACCEPT.100min vs45 NONCOMPLIANT; broad124 run not a new certification.
