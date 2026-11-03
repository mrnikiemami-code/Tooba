PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R1
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — finish accepted-group execution without carrier bypass
Mode: CLOSE_REVIEWED_TRANSPORT_ROUTING_AND_REAL_MODAL_INTERACTION_GAPS
Expected-Base-SHA: 063e100c8f3f60b8d08b08f55b25714d3e551ef6
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

GOAL
Finish the SAME agreed seller -> accepted shipping group -> physical parcel UI on the canonical user environment. Preserve 252 layout improvements. No new shipping feature, no new order-change workflow, no screenshots.

ARCHITECT REVIEW: 252 PARTIAL, NOT FULL ACCEPT

AcceptedGroupShipmentCreateBinder validates membership/payment/remaining quantity, but never branches on committed transport mode. CreateShipmentCoreAsync always calls _fulfillment.CreateShipmentAsync directly, passing request.ProviderMetadataJson. This leaves a generic internal-allocation path for CarrierApi groups outside the existing 249 account/readiness/occurrence/claim/provider protocol. A read-only accepted method label alone does not close that bypass.
ResolveFulfillmentMethodCode strips canonical options to a generic prefix. Client post:other can normalize equal to accepted post:standard. Preserve exact accepted service/option identity at the boundary; do not treat prefix equality as proof of exact commitment. Any needed Fulfillment registry mapping is a separate explicit adapter step.
AdminCreateShipmentModal.submit uses async React pending state only, with no synchronous in-flight guard or try/finally. Same-tick submits can both enter; thrown transport failure leaves pending stuck. Tests supplied are source contracts 4/4, NOT required mounted interaction tests.
Shared create command now requires groupId. Audit actual seller/other existing callers so this does not break legitimate reachable execution or leave an alternate override.

A — ONE LAWFUL ACTION PER TRANSPORT
Derive transport mode from persisted accepted Order facts, never from a client field or method-name heuristic.
Manual/Free: normal group-bound Fulfillment parcel path, preserve accepted method/time/fees and quantity protection; no carrier account required.
CarrierApi: ONLY the existing group-scoped Shipping execution workflow with 249 completion/readiness/account binding/occurrence claim/provider dispatch and no-resend recovery. Generic Order create_shipment MUST refuse before allocation/provider when called for CarrierApi, even if the attacker submits correct groupId+method. Do not add a second carrier execution implementation.
Use owning Contracts/ports and typed localized refusal. Do not duplicate provider logic in Order/Host. UI should render ONE relevant create action inside the group; no simultaneous generic modal plus separate API-create path for the same group.
Do not accept arbitrary ProviderMetadataJson as an alternate carrier/service/identity override. Derive safe execution inputs from accepted facts/authoritative completions.
Check canonical accepted method identity exactly when supplied; unknown/unsupported mapping fails closed. Keep accepted historical method fields unchanged. Optional registry mapping must not authorize a different catalogue option.
Audit shared seller command/UI callers; wire group identity/read-only method to reachable seller flows, or present a clear lawful blocked state. No silent seller regression.

B — SMALL FORM RELIABILITY FIX
Synchronous ref gate BEFORE first await; release via finally for success/refusal/exception. Show localized error on thrown failure; form remains retryable as appropriate. Preserve group and quantity edits on failure. No auto retry.
Server remaining-quantity and replay/allocation protection remains authoritative. Reject duplicate line selections or aggregate and validate totals before allocation, rather than allowing per-entry quantity checks to overstate a line.
Do not add fake tracking/carrier state. Keep package physical facts separate; no summed product dimensions.

C — EXECUTABLE PROOF, NOT SOURCE TOKENS
Mount ACTUAL panel/modal in existing jsdom harness:

same seller, two accepted groups, same method label but distinct owner/date: exact group summary+line membership and group-scoped parcel list;
Manual create emits correct group+selected qty, NO method catalogue; same-tick click -> one request; thrown request -> visible error, pending released and edits preserved;
CarrierApi group exposes existing controlled API path and no generic modal action; tampered generic server call refuses with zero Fulfillment/provider calls;
foreign/cross-group/duplicate line/excess/method-option mismatch refuse before side effects;
seller equivalent includes required group identity; legacy missing accepted facts stays readable and blocked.
Use a focused orchestrator/adapter integration test for transport routing (not binder-only). Reuse controlled provider and isolated fixtures; relational test only if allocation behavior changed. Do not replay every prior carrier suite.
Run typecheck/build and focused affected tests only.

CANONICAL HANDOFF
Same D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088 SingleStore/store-alpha/tooba_alpha.
Normal authorized read-only task session opens order:
http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9
Show group read-only method/date/time and open eligible modal only. No user create/pack/cancel/dispatch/payment mutation. Isolated same-seller dual-group fixture proves missing live shape; do not change user's order to create it.
No screenshots/new final ports/alternate DB/real Tipax credentials or calls. Preserve DataProtection/user data. Read back effective CapturedPreview local Enabled/SelectionEnabled/CheckoutEnabled after restart; no recurrence of lost flags. Checked-in defaults unchanged.
No migration expected; no seed/reset/business SQL. Validate actual main/base and preserve intervening work; no force/reset/stash/rebase.

RECOVERY / DELIVERY
Update AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md with 252 Architect PARTIAL and exact R1 closure. Retain prior history.
Preserve TASK verbatim under docs/ai/tasks/TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R1/TASK.md and evidence TASK/RESULT/RECEIPTS.
Return exact transport routing, method/option validation behavior, mounted+server evidence, deployment/gate parity, authorized review URL and any unmet scope honestly.
Priority is a usable, correctly bound existing form; avoid unrelated UI redesign or speculative feature additions.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.
END_TOOBA_TASK