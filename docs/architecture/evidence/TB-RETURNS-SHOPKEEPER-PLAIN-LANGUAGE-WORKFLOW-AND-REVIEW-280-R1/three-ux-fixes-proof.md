# TB-280-R1 — three UX fixes mapped to source and M1–M7

## UX1 — reviewed money display

| Finding | Before | After | Proof |
| --- | --- | --- | --- |
| First-refund confirm shows requested money | dialog used `snapshot.refundAmount` | `firstRefundReviewMoney` → stored `approvedAmount` (or reviewed-plan frozen amount only when `fromReviewedPlan`); else unavailable + disabled confirm | M1: 3000/2000 shows ۲۰۰۰۰; confirm POSTs start-refund |
| Approved zero truthy-fallback | `approved \|\| refund` style risk | `!== null && !== undefined` keeps `0` | M1: approved 0 shows ۰, never 3000 |
| Missing approved | silent wrong amount | unavailable message, confirm disabled, no POST | M1: approved null → `admin-return-start-refund-unavailable` |
| Resume/retry | requested or first/latest guess | exact attempt matching `pendingRefundAttemptId` / `failedRefundAttemptId` | M2: pending 1500 shown; foreign failed id → unavailable, no POST |

Source: `admin-return-workspace.tsx` helpers `firstRefundReviewMoney` / `boundAttemptReviewMoney` and start/resume/retry dialogs.

Wire safety: bodies still carry only versions/attempt ids — no client-supplied money field added.

## UX2 — condition-bound review dialog

| Finding | Before | After | Proof |
| --- | --- | --- | --- |
| Implicit baseline prefills aggregate into usable | `acceptedUsable = line.accepted` | init from `acceptedUsable` / `acceptedDamaged` only when that condition's received ceiling > 0 | M3 damaged-only: no usable input; damaged=۱; body acceptedUsable=0 acceptedDamaged=1 |
| Zero-ceiling healthy field | usable field with max 0 | hide field when ceiling ≤ 0 | M3 |
| Mixed open with usable=3 on ceiling 2 | aggregate accepted | per-condition values; over-ceiling local refuse | M4: open clean; «۳» aria-invalid; Persian «۱٫۵»/«۱٫۲۵» lawful body |
| Zero-received sibling | fake quantity inputs | not-received notice + required reason; five-portion body | M5 |

Source: `beginResolution`, resolution dialog render (`showUsable`/`showDamaged`/`nothingReceived`), `describeAcceptedCeilingProblem`, `reviewResolution` empty→zero.

## UX3 — per-product unit summary

| Finding | Before | After | Proof |
| --- | --- | --- | --- |
| pcs 1 + g 500 → «501 of 501» | `sumUnitsOf` across items | `describeReceivedGoodsSummary` per product·unit; `hasAnyReceivedGoods` boolean only | M6: no 501; shows کالای اصلی + ادویه + عدد + گرم |
| Zero received headline | misleading aggregate | «هنوز کالایی دریافت نشده است» | M7 + live DOM |

Source: `hasAnyReceivedGoods`, `describeReceivedGoodsSummary`, `data-testid="admin-return-received-summary"`.

## M7 / retained safety

- Zero-received case: only `record_receipt`; cancel stores nothing (sessionStorage key absent).
- Settled case: no action buttons.
- Retained 276/277/279/280 mounts stay green; 277 fixture gained `approvedAmount` so the separate first-refund path remains exercisable under UX1; 279 notice assertion matches plain ceiling copy.
