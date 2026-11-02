# 248-R1 recovery synchronization — 2026-11-02

## Recorded state and authority

Source/evidence commit: `04abe1af87286afb8bf65c839e8a1732be9983f9`.
The repository RESULT/RECEIPTS and user-supplied worker envelope report software completion and canonical deployment. This checkpoint checked those documents against the current main commit. It did not run the user's Windows runtime, rerun tests, independently audit the implementation, or perform Tipax HTTP calls. Worker PASS is retained as a worker result, not converted into Architect ACCEPT.

No implementation task is active. No account/test is requested now. Live Tipax acceptance is deferred by the user's explicit instruction. A future independent code review is a review activity, not an automatic new implementation task; record concrete findings before declaring further work necessary.

## Canonical delivery

- Source: user's SarvNewVer checkout; FE3000 → Host5088; SingleStore / store-alpha / tooba_alpha.
- Worker reports accounts/secret references, reference sync, city mapping, method binding, captured API pricing and immutable accepted quote evidence, stale validation, create/track/cancel and admin/seller panels.
- CarrierApi is installed but unconfigured. Missing credentials must perform zero outbound calls and show the localized unconfigured state. Manual/Free remain available.
- Label/PDF is unsupported by the current adapter. Provider-wide absence was not independently verified here.
- Worker checks: Shipping 123/123, Order 21/21, frontend 11/11, typecheck/build/publish successful. Controlled provider proof is not live Tipax acceptance.

## Recovery safeguards

1. Inspect actual main, local edits, process/DB identity and build provenance before touching runtime. Recorded PIDs are historical.
2. Verify official migration history: Shipping CarrierConfiguration 20261102170000 and CarrierShipmentOperations 20261102180000; Order AddAcceptedCarrierQuoteEvidence 20261102180000. Apply only genuinely pending migrations through the official runner after backup.
3. Preserve App_Data/dataprotection-keys under the actual Host content root and application name Tooba. Moving/rebuilding publish output must not discard keys or orphan existing protected secrets. Verify actual key location and access permissions before a swap.
4. Preserve user accounts/cart/drafts/rates, tabs and unsaved work. No reset/reseed, guessed merchant data, alternate target handoff or real shipment/payment action.
5. Do not enable external carrier connectivity or request an account now. Future activation needs real owner credentials, provider references, authoritative origin/package facts and explicit live acceptance. Do not promise real shipment/provider correctness from controlled tests.
6. External create ambiguity must remain UNKNOWN until reconciled; never assume exactly-once or blindly retry based on local uniqueness alone. This is a review invariant, not a new verification claim.

## Documentation-only change

This synchronization changes recovery documentation only. No source behavior, configuration, migrations, runtime or business data changed. Original RESULT/RECEIPTS and history remain intact. This commit is a documentation successor to the source commit; it does not imply a fresh application deployment.
