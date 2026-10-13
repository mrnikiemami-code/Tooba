TB-P0-PAYMENT-ENTRY-RESERVATION-GATE-022

Task-ID: TB-P0-PAYMENT-ENTRY-RESERVATION-GATE-022
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-FINISH-PAYMENT-021
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 8667e5bc6aa84ef02c95db26e4c3e5e41ba806ad
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD
automaticNextImplementationTask: NONE

MANDATORY PREFLIGHT
Confirm HEAD == origin/main == Expected-Base-SHA, and inspect tracked dirty paths without touching them. If drift, STOP (no auto-rebase). Read AGENTS.md, pipeline protocol, current recovery, and 017–021 results. If 021 result-carrier HEAD differs because of docs-only commit, report and STOP instead of guessing. This task must not touch the live Host, :5088, or tooba_alpha.

MISSION — SINGLE P0 DEFECT
Protect the EXISTING publicly reachable money-entry route POST /v1/storefront/checkout/{checkoutId}/payments (including wallet/manual/online) against initiating on an expired/released/unpayable reservation. Reuse canonical Order-owned checkout/actor/cycle/supply and existing unpaid-retry/reacquisition policy; reuse 018 IOrderPaymentHoldPreparationPort only if its preconditions and selected concrete method are met. Do not duplicate inventory checking or invent a new retry allowance. Keep payment-attempt retry distinct from reservation-cycle renewals. If the currently configured renewal limit is exhausted, fail closed with a meaningful existing owner-defined result; if renewable and stock exists, use the existing authoritative Order retry/reacquisition machinery rather than extending a Released row. If necessary, scope this task to an analysis-backed safe internal Payment-owned guard and leave endpoint behaviour unchanged until completeness is proved.

CRITICAL SAFETY
A newly persisted payment attempt/provider call/wallet debit MUST NOT occur before payability/stock proof. A previously persisted same-key payment result may replay according to existing idempotency semantics; never create a second charge or misreport changed checkout/method (021). Actor and guest access checks must precede stock extensions. Payment already has an external provider call before its DB SaveChanges: do not put PSP/wallet calls inside DB transactions, and do not introduce an ambient transaction spanning module DbContexts. Do not trust Order cycle status alone (014 showed Inventory can be Released while Order shows Active). Enforce effective real method/provider hold policy, not generic selector 'gateway' if a provider override exists. Do not add hardcoded 1/2/3 retry limits, deadlines, floors, or force equality of cycle/payment TTL. Preserve configured current store/platform policies.

DELIVERY CONSTRAINTS
Prefer <=3 production files, minimal focused Payment/Order module tests. No new public endpoint or frontend changes in this task. Do not alter Host production OR Host.Tests, Program.cs, gateway credentials, schema, migrations, config, existing 015/017 Inventory implementations, or unrelated files. Do not build Host or Host.Tests. NO live DB write, order mutation, actual PSP, wallet debit, deploy, restart, service stop. If an additional Payment error code or certified guard edit is strictly required, STOP and identify the exact minimal authorized file set; never repurpose an existing code or throw an unknown code that becomes 500. Record potential 021 historical SoT drift as follow-up without rewriting historical recovery records.

REQUIRED EVIDENCE
Identify all actual money-entry paths including old direct Payment endpoint and 019 composer. Show exact order of ownership, existing-key replay, method selection, cycle/stock verification or renewal, and provider initiation. Focused tests using fakes for expired hold, Released hold while cycle Active, valid hold, renewal allowed vs exhausted, wallet/manual/gateway, ownership failure, and changed-method same-key conflict, ONLY insofar as the available contracts make these tests truthful. Never fake a PostgreSQL locking or cross-module atomicity proof. If it cannot be safely implemented in 20 minutes, deliver a precise blocker with smallest needed follow-up and no unsafe partial endpoint.

FINAL RESULT
Report changed files + SHAs, exact tests/result counts, whether old direct endpoint is actually guarded, whether all three payment methods are covered, whether new endpoint/front-end still absent, residual races, any P0 blocker, and Host untouched. Commit/push only bounded validated work; STOP_FOR_ARCHITECT.
