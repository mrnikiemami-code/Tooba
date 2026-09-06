# TB-P09-T002-R1 — Runtime smoke

Host: `http://127.0.0.1:5088`  
Checkout: `01a0451c-fabf-7000-99f0-30d410a58638`  
Actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db`

## Observed

- System history entry: `actorKind=system`, `actorDisplayFa=توسط سیستم`
- Operator note / history note: `actorKind=user`, `actorDisplayFa=توسط اپراتور آلفا` (after OperatorProfile upsert with usable display name)
- No truncated GUID pattern (`توسط اپراتور {8hex}`) in actor labels
- `GUID_LEAK` check on history page actor labels: false
- Invoice/receipt endpoints unchanged (not re-exercised beyond T002 baseline; no composer semantic change)

## Frontend

`admin-order-detail-screen.tsx` still renders `actorDisplayFa` only; maps new `actorKind` / `actorDisplayName` from API without layout change.
