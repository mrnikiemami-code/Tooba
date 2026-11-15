# ARCHITECT REVIEW — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3

Decision: **PARTIAL**, not Architect ACCEPT.

Reviewed actual `main` `1ff02ab8a18c20999b92791f2b51520726bfa2d1` / implementation
`96c5a8e19c10ba246b799202a804187ca1957a18`.

## Preserved (improved, not reopened)

- captured `AreaGroupId` addressing + exact binding comparison;
- Inventory hold read (`IFulfillmentReplacementReadinessReader` → live `Held` reservation);
- own-plan dispatch guard / queue projection / bulk preflight on one shared binding;
- selected-context review fingerprint;
- create-to-plan plumbing.

## Remaining source-confirmed defects (the whole of R4)

A. `admin-shipment-plan-modal.confirm` writes the body BEFORE the POST with `uncertain:false`; `load` freezes ONLY
   `rec.uncertain && rec.requestBody` and treats every other retained body as history. If navigation/reload/unmount
   happens while the first POST is in flight, no catch has set `uncertain:true`, so on remount the original body/key can
   be overwritten by a fresh intent. **A request is unresolved as soon as it may be sent**, not only after the UI catches
   an exception.
B. `PlanShipmentDispatchCoreAsync`, after stored-plan replay, calls the synchronous `RequirePlannable(locatedAny)`. It
   never calls `RequirePlannableAsync`, so a direct new confirmation POST bypasses the active-package membership gate;
   the earlier new-command `EnsureCancelledDoesNotBlock` was removed and `RequirePlannable` does not check
   `IsCheckoutCancelled` either. Preview denying a cancelled checkout is not a guard for the mutation. Exact authorized
   historical replay must remain possible; a NEW confirmation must enforce cancellation **and** package gates.
C. `features/order-operational-groups/created-parcel.ts` returned the only `Created` resend candidate even when a
   non-empty requested occurrence key did **not** match it (a fallback to another attempt, explicitly forbidden). The
   replacement fallback scanned every source and accepted any one link with a matching key without binding the requested
   source parcel. "Only one candidate" is not exact operation identity.
D. The modal's epoch effect returned immediately when `!open` and had no cleanup, so closing/unmounting alone did not
   invalidate an in-flight load/send callback; `send.finally` also called `setPending(false)` unconditionally. The
   per-component `inFlightRef` is not durable across remount, and an old answer could clear a newer same-scope retained
   attempt when clearing was scope-only.

R3's green tests did not cover the first-POST interruption (before any exception), the unmatched-key single-candidate
fallback, or a direct package-locked confirmation.

## Standing limits carried forward

- Relational evidence is sequential immutability/unique-index proof; the contradictory "two independent writers" claim
  must not be repeated.
- R2's missing **pre-apply** backup remains an irreversible audit deviation; no retroactive fix/reapply/restore.
- The Architect has not executed local runtime/tests; mounted/owning-application evidence is worker evidence, and there
  is no live/user acceptance.

## Issued response

Exactly one small repair, `TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4`:
`REPAIR_PRE_SEND_DURABILITY_NEW_CONFIRM_GATES_AND_EXACT_HANDOFF`. `271` remains NOT ISSUED;
`automaticNextImplementationTask: NONE`.
