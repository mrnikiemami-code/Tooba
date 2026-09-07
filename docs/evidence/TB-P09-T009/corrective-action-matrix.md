# Corrective Action Matrix

| Transition | Class | Action |
|---|---|---|
| Whole-order cancel (multi-seller) | DirectReverseAllowed while policy allows | one `cancel` |
| Manual reject deposit | CorrectiveActionRequired | `restore_deposit` |
| Packed → unpack | DirectReverseAllowed if not allocated/dispatched | `unpack` (T005 unchanged) |
| Shipment Created (+tracking) → cancel | CorrectiveActionRequired | `cancel_shipment` |
| Tracking typo pre-dispatch | CorrectiveActionRequired | `correct_tracking` |
| After dispatch | Irreversible (simple reverse) | no unpack/cancel-as-never-left |
| After delivered | Irreversible for fulfillment | Return only |
| Accidental cancel, no irreversible effect | CorrectiveActionRequired | `restore_cancelled_order` |
| Cancel after refund completed / dispatch / delivery | Irreversible | restore forbidden |
