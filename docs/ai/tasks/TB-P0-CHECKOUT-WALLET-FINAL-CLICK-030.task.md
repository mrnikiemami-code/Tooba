TB-P0-CHECKOUT-WALLET-FINAL-CLICK-030

Task-ID: TB-P0-CHECKOUT-WALLET-FINAL-CLICK-030
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-FRONTEND-FINAL-CLICK-029
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 0460533acfb8af2a70781f046ee7a086231e2bf3
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD
AutomaticNextImplementationTask: NONE

Mandatory preflight
Verify HEAD == origin/main == Expected-Base-SHA. If drift, STOP without rebase/stash. Read AGENTS.md, BRIDGE-WAKE-V1 and 028/029 evidence. Preserve pre-existing dirty/untracked files. Development Host :5088 is CLOSED: no edits/build/tests/stop/restart/deploy under src/backend/Host/**; no live DB writes, schema/config/migrations, real wallet debit or PSP calls. Do not publish or deploy frontend.

Mission
Close the remaining cart-mode wallet tender gap introduced/disclosed by 029 without reintroducing Shipping eager commit. A shopper with sufficient wallet funds must be able to choose wallet on the pre-commit Payment page and initiate it ONLY on the final Pay click through POST /v1/storefront/cart/{cartId}/start-payment (028), using canonical Payment/Wallet authority. Ineligible or insufficient wallet must fail closed and show a truthful UI state. Do not fabricate balances or quotes, assume wallet always eligible, or change reservation timeout/cycle limits. Preserve gateway/manual flows and existing checkoutId recovery.

Architectural constraints
A read-only pre-commit wallet eligibility/quote requires a trusted cart-scoped, actor/guest-verified backend owner contract; checkoutId-only wallet quote cannot be invoked against an Active cart. Prefer reuse of existing Wallet/Payment/Order Contracts-only seams and existing read-only cart quote. Do not expose balance details without owner-authorized verification. If a correct cart-scoped quote would require more than one coherent bounded slice, do NOT add optimistic client wallet enablement: report exact missing owner contract and the minimum next slice, with no unsafe public feature. A real wallet charge remains after the server-side 028 commit + 025 fresh-payability gate, never before user click. Wallet shortfall/mixed tender must retain existing Payment typed failure policy.

Implementation approach (only if safe and testable)

Find current Payment cart-mode method catalog, checkoutId wallet quote, and wallet/Order read-only owner seams; provide file:line authority map and whether there is already a secure cart-scoped eligibility endpoint. Do not redo 019-029 archaeology.
If a secure cart-scoped wallet quote already exists, consume it from cart mode and enable wallet only for canonical full-coverage eligibility. Keep zero commit and zero wallet debit during GET/preview.
Otherwise implement the smallest correctly owned, actor/guest-scoped read-only quote contract in Payment/Wallet/Order module (not Host). Test unauthorized guest/actor, insufficient funds, full coverage, stale cart version, and zero money movement. No browser-provided amount/balance authority. If not feasible in the hard box, BLOCK and leave wallet disabled with explicit follow-up; do not misrepresent as complete.
Verify chosen wallet final click sends useWallet=true with a stable key and no pre-click money calls; gateway/manual unchanged; recovery of existing checkout unchanged.
Focused tests only, TypeScript typecheck for any frontend edits, scoped module tests for any backend changes. Never build or run Host/Host.Tests.

Critical review gates
No Shipping POST /shipping/commit on Continue. No checkout creation/reservation on mere Payment page visit. Wallet must not become a bypass around the registered Payment fresh-money gate. No duplicate cart conversion; no false claim that a read-only point-in-time quote guarantees wallet balance at Pay time. Handle stale/rejected wallet attempt with recoverable checkout reference, no invisible second commit. Document any unrelated baseline failures, never repair them in this slice.

Worker result
Return BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT, exact SHA(s), paths, actual focused test results, wallet cart-mode eligibility and final-click evidence, security and residual races, whether the user can truly use wallet before checkout commit, and Host CLOSED proof. STOP_FOR_ARCHITECT; no auto-next task.
