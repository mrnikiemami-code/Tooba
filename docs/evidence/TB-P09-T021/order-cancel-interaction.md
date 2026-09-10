# Order cancel interaction

`AbortForCheckoutCancelAsync` now:
1. `VoidActivePackagesForCheckoutCancelAsync` — cancel Created packages for checkout
2. Existing abort of pre-dispatch shipments / warehouse reset

Post-central-dispatch: LOCK-OPS-002 still blocks whole-order cancel (dispatched quantity present). Package does not weaken that gate.
