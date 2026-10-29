# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R2 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker ≠ Architect / Visual ACCEPT.

## Defect

R1 filled `retryHint` on the five-second timeout but rendered it only in `sr-only` inside a `h-0 overflow-hidden` bar, so sighted users saw the progress vanish with no explanation.

## Repair

- Pending: keep top progress + localized sr-only pending live text.
- Timeout: clear pending/lock; show compact visible FA/EN banner (`data-testid="storefront-nav-timeout"`), `role="status"` / `aria-live`, `pointer-events-none`.
- FA: «باز شدن صفحه طول کشید؛ دوباره تلاش کنید.»
- EN: “The page is taking too long to open. Try again.”
- Auto-dismiss `NAVIGATION_RETRY_DISMISS_MS` = 6s; cleared on next ordinary `started` begin.
- No second provider; modified/native clicks unchanged.

## Proof

Focused navigation + guard tests 17/17; typecheck clean; production build; warm product click pending ~9ms on FE3000→Host5088. Cart `01a17b2d-e219…` Active v48 checkouts0 untouched. Parent R1 shipping/nav logic not reopened.

## SHAs

Expected-Base / Base: `6187bf7b71bf75ada518dd1ae9976757660f2c53`  
Code-SHA: `c9a33d4cee6adc05e610a7c5ccccd681c4e09d1e`
Evidence-SHA: `c9a33d4cee6adc05e610a7c5ccccd681c4e09d1e`
