PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177
Parent-Task: TB-SHIPPING-ADMIN-METHOD-PACKAGING-176
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8de5f34e0c14977c8456b7f3d42140c0678b03f9
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_PACKAGING_CHARGE_OWNER_CONTRACT_AND_CUSTOMER_QUOTE_SEAM
Hard-Timebox: 45 minutes HARD
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Resolve the actual TB-176 commercial gap in ONE bounded, owner-correct increment: determine whether the saved per-method packaging/freight amount is a customer-payable component, a carrier/operating cost, or only operational metadata according to existing authoritative contracts. Implement only the smallest unambiguously authorized customer-quote path, if one already exists and is contractually approved; otherwise ship precise executable/owner regression guards for the existing behaviour and record the blocking business decision as PARTIAL, NOT an invented price. This is not authorization to add packaging fees to customers.

MANDATORY NAMING / STRUCTURE RULES (HARD GATES)

Identity and artifacts MUST be exact and consistent: Task-ID TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177, file docs/ai/tasks/TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177.task.md, evidence folder docs/architecture/evidence/TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177/, result RESULT.bridge.txt. Preserve Parent-Task, Program, Channel and Worker-Id verbatim. Do not invent a second task ID, filename alias, suffix, new naming scheme or free-form task header.
For ANY new production type/file, derive the canonical filename, type, namespace and project ownership from neighbouring repository files BEFORE editing. Use PascalCase .cs file/type names and matching namespace rooted in the owning module's actual project and capability; use existing kebab-case TypeScript module naming alongside current frontend files. Folder path MUST agree with namespace and capability owner. Never create technical-axis root folders or duplicate a concept already exposed by owner Contracts.
Shipping owns packaging policy/evaluation/quotes; Order owns cart/draft/accepted customer charge; Payment/Settlement own their distinct financial authority; Host composition ONLY. Cross-module references Contracts-only. Use current CQRS/MediatR 12.5 ISender, Result/ApiResponseFactory, canonical API route conventions. No Host business logic or Results.Json. Do not rename any existing public method, route, DTO, migration, table, group identity, carrier code, or finance terminology for convenience.
Before committing, run the repository's ACTUAL naming/structure/import/architecture guards relevant to touched paths (not a guessed command). Record exact commands, failures and whether pre-existing; do not rebaseline, silence or delete guards. If an established naming rule conflicts with this task's proposed naming, obey the repo rule, record the conflict and STOP/PARTIAL instead of inventing an exception.

FIRST CHECKS / SOURCE-OF-TRUTH

Verify clean scoped working tree and HEAD == origin/main == Expected-Base-SHA before any write; if divergent STOP_FOR_ARCHITECT. Preserve unrelated dirty frontend admin-fulfillment file, TB-075/079 logs, stashes and all other worker artefacts untouched.
Read locked docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md, docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md and TB-155/160/163/164/172/176 actual evidence/source. Inspect existing ShippingPackagingFeeResolver and ShippingOperationalQuoteEvaluator; current strict Shipping-owned quote Contracts; StorefrontShippingOwnedRateResolver, SaveSelectionAsync, Storefront shipping group charges, payment-draft reconciliation. Trace exact fee ownership, payerKind (Prepaid vs CollectOnDelivery), included/excluded/tax semantics and method option scope. Do not guess that a packaging policy's FeeAmount equals an additional customer charge.

BOUNDED IMPLEMENTATION
A. Produce a source-backed single-page authority matrix: persisted packaging policy -> operational quote -> storefront customer quote -> cart group/draft -> payment preview -> payment allocation/Settlement. For each edge show YES/NO, exact symbol/contract and monetary semantics. Explicitly distinguish packaging service cost, customer shipping charge, amount payable on delivery and seller/platform/carrier settlement; no assumed equivalence.
B. Confirm if existing approved owner contract already specifies a separately customer-payable packaging charge with correct currency, taxes, payer, explicit precedence and method-level option identity. ONLY if ALL exist: implement the minimal gated (default OFF, absent-is-false) owner-correct quote/Order consumer path, fail closed on unavailable/ambiguous/currency mismatch; prohibit provider/sibling fallback, double counting, and silent changes to old accepted snapshots. Tests must show quote->draft->payment-draft exact per-group reconciliation and unchanged OFF parity. Any missing authority or undecided pricing responsibility => DO NOT implement or enable customer charging, stop at regression protection + blocked contract/evidence.
C. Leave TB-176 Admin create/edit/read/view-mode controls as proven. Preserve canonical method binding obtained from server, exact (serviceCode,optionCode) scope, and legacy shared row quarantine; method tipax:standard policy may NEVER charge tipax:express. Never interpret a stored zero as FREE and NEVER make CollectOnDelivery an advance-paid fee by default.
D. No new settlement allocation policy, carrier payable, fee share, tax assumption, checkout migration, provider quote, carrier booking, financial-mode invention or implicit payouts. If business sign-off is genuinely required, identify the single smallest owner decision in RESULT.md rather than disguising it as an implementation PASS.

ACCEPTANCE / EVIDENCE

Focused existing Shipping owner tests and relevant Order/payment-draft tests only when code changes; verify exact identity, denied/ambiguous quote, no sibling inheritance, payment sum charged once where contract expressly authorizes it, and OFF legacy parity. If no code is allowed, provide pinned source/provenance regression evidence without running redundant broad suites.
Prefer a bounded disposable loopback harness if a safe end-to-end consumer exists, using TB-157/166/172/176 recipes. Never write to the shared services. Any screenshot/HTTP assertion must be real; otherwise mark it UNVERIFIED.
Evidence under the task ID only: RESULT.md, focused test/build commands and statuses, RESULT.bridge.txt, scoped diff, exact SHA/parity, elapsed minutes, and isolation/cleanup if a scratch environment ran.
PASS only when a legally/contractually grounded customer-charge seam is implemented and proved end-to-end OR an already complete owner contract is proved correct with no source change, and all required checks pass. PARTIAL when authority is unresolved or acceptance is incomplete. BLOCKED if safe method ownership itself cannot be established. Do not assert that operational packaging policy automatically alters customer payable.

SAFETY / STOP
ONE task, 45 minutes HARD. Shared :3000/:5088/:5432, tooba_alpha/store-alpha, running processes, RabbitMQ/Redis untouched. No resets/rebases/stashes/force push, .next purge, real PSP/SMS/payout, unsupported carrier capability, broad suites or unrelated changes. Keep existing RTL/CSS, flat seven-or-more-method list, exactly three row actions and four non-nested tabs. Fast-forward push only, then STOP_FOR_ARCHITECT. Do not take another task automatically.

RESULT FORMAT (EXACT)
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177
Parent-Task: TB-SHIPPING-ADMIN-METHOD-PACKAGING-176
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Naming-And-Architecture-Guards: ...
Scope-Delivered: ...
Owner-Authority-Matrix: ...
Focused-Tests: ...
Customer-Charge-Proof-Or-Blocker: ...
Method-And-Sibling-Isolation: ...
Unverified: ...
Elapsed-Minutes: ...
Isolation-And-Cleanup: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
