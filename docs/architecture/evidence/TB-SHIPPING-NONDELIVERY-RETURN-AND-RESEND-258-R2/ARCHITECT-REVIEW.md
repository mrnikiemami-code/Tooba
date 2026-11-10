# Architect review 258-R2 — PARTIAL; two bounded safety corrections
2026-11-10; independently reviewed source a56814cb7ea97126bcab08da45b1886449fd3bab and worker result13112af098c976982c7b7b4897cfca7ccfd8b014. No independent Windows runtime/test execution claimed.
Accepted within review: narrow public-command reconcile before fresh projection, current actor and checkout/fulfillment/parcel checks, actual retained-entry wiring, mixed usable/damaged receipt UI. Preserve them.

Remaining:
1. FulfillmentUnit.ResolveRecordedResendIntent returns ResolvedCancelled immediately when recorded.Status==Cancelled, BEFORE NormalizeSelections and RecordedResendAnswersIntent. A changed source/quantity using the key of a cancelled parcel receives authoritative completion for the wrong intent. Cancelled history is a result state, not an exemption from exact identity validation.
2. post-dispatch-attempt-store.writePostDispatchAttempt catches failed sessionStorage, stores only memoryFallback and returns a normal successful attempt. The dialog proceeds to POST. A full page reload loses the Map; a partial resend with remaining capacity may then mint/allocate again after a lost response. The required storage-unavailable refusal/durable recovery is still missing. In-memory persistence is not reload durability.

Issue3 remains PARTIAL; issues1/2 accepted257. Only258-R3 issued;259/260 NOT ISSUED. No new DB, one canonical tooba_alpha. Do not redo accepted lifecycle/domain/UI.
