# Runtime smoke — TB-P09-T017

Raw: `runtime-raw.json`. `ok: true`.

- A Waiting manual: confirm/reject/cancel; no fulfillment on whole-order set
- B ReadyToFulfill: mark_processing + cancel
- C/D 1.25 pack 0.50 remain 0.75; Created+tracking still has one cancel
- E Delivered T015: cancel absent; POST forbidden FA
- F T016 cancelled paid: restore only; history cancel + inventory
- G Delivered: return actions present, no cancel
- H Multi-seller 3 sellers: whole-order collapse = one cancel
- Print invoice Tax/Duty/LineCount
- Grid list + ReturnRequested filter 200
