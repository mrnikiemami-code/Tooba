TB-SHIPPING-OWNER-OPTION-COVERAGE-161

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-OWNER-OPTION-COVERAGE-161
Parent-Task: TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0ad0296ab0df0c2449417cf6438310b1cf3ca6ff
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_OWNER_OPTION_SCOPED_COVERAGE_AND_SAFE_DEFAULTS
Timebox: HARD 45 minutes, no extension, no repeated full suites
Status-on-claim: IN_PROGRESS

Objective

Close the next owner-side blocker identified by TB-159/160: coverage grants currently have only service+destination identity, not option/method identity. Each flat shipping method must be able to own its own opt-in geographic restriction with no borrowing from sibling option or provider-level legacy coverage when strict method pricing is requested. Keep current legacy callers byte-compatible. This is production implementation, not another wireframe/report-only task.

Required upfront inspection

Read canonical recovery and contracts: docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md; docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (including unrestricted-default addendum); docs/architecture/evidence/TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159/OWNER-AUTHORITY-BLOCKER-159.md; TB-160 result; ShippingCoverageGrant, write commands/contracts/EF mapping/migrations, ShippingRatePolicyEngine and IShippingRateQuoteLookup. Verify current origin/main SHA before touching code. Follow module ownership and migration conventions.

Locked semantics / safety contract
Absence of an explicit geographic restriction for one canonical method means unrestricted by Admin, not disabled, not missing price, and not inheriting sibling restrictions. This does NOT mean a shipping quote is free or financially ready; quote still requires an owned rate and every other authoritative eligibility constraint. Coverage/eligibility and pricing readiness must be distinct states.
If method A has explicit restrictions, evaluate them ONLY for A; option B with no restrictions stays unrestricted (subject to its own price and other requirements). Legacy service-level coverage cannot narrow or authorize an option-specific strict method by accident. No widening of a method's explicit restrictions by falling back to provider coverage.
No fake origin, parcel weight, carrier capacity, dates, monetary policy, settlement allocation or provider quote. Conditioned rates lacking trusted measurement must remain MeasurementRequired. StrictOwnedOption never inherits service-level/sibling rate (TB-160 must stay green).
The existing legacy coverage behaviour remains unchanged when strict mode is not requested. Do NOT activate a customer-facing authority gate or modify Order/storefront yet; do not equate enabled with quote-ready.
If restricted methods require additive OptionCode persistence/schema, do it ONLY in Shipping-owned entities, contracts, EF migration/model snapshot and authorized Shipping Admin write/read seams. Preserve old rows as legacy service-level rows; never migrate/assign legacy rows to every option. Check index uniqueness and idempotent migrations. If a safe migration cannot fit the timebox, STOP/PARTIAL with precise blocker rather than silently reducing the requirement.
Single bounded implementation
Extend Shipping-owned coverage representation/write API so a server-authoritative method identity (serviceCode + optional optionCode) can explicitly define a restrictive allowed geographic set while a missing method-specific set denotes unrestricted. There must be no browser methodCode splitting and no Host business authority.
Extend owner quote engine/adapter under StrictOwnedOption to enforce ONLY this method's explicit geographic restrictions. Missing explicit restriction is NOT a coverage.missing failure for strict mode; owned rate remains mandatory. Existing default LegacyFallback behaviour remains unchanged, including its currently required active service coverage grants.
Ensure clear owner contracts/read semantics distinguish unrestricted vs restricted, with tests for zero configured restrictions and multiple supported destinations. If the current coverage grant has no explicit restriction-intent representation, design a minimal additive representation that distinguishes an empty allowed set from unrestricted, without inventing a universal destination row.
Add focused owner tests: tipax:standard restricted to city X, tipax:express unrestricted; standard denied in city Y while express is not denied by standard's rule; standard allowed in X; provider-level legacy grants not inherited; zero configured restrictions unrestricted but not quoted without owned rate; conditioned rate with no measurement fail closed; optionless and default legacy behaviour unchanged; persisted option ownership and migration regression where applicable. Verify fail-closed 401/403 for new Admin write surface if altered.
Prefer the existing rate/coverage endpoints, CQRS/MediatR 12.5 ISender, Contracts-only cross-module; no overlapping API. Keep four one-level Admin tabs / RTL intact. Do not redesign UI in this task; minimal wiring only if necessary to avoid a misleading Admin restriction state.
Acceptance and evidence

Run necessary builds/focused Shipping tests and a bounded real-DB migration or persistence proof if schema changes. Record exactly what is proven. PASS only if independent strict coverage semantics, unrestricted defaults, legacy parity, and persistence/migration safety are proven. Otherwise PARTIAL/BLOCKED with specific failing obligation; do not report all-clear from compilation alone. Deposit docs/architecture/evidence/TB-SHIPPING-OWNER-OPTION-COVERAGE-161/RESULT.md and focused logs. Include code/evidence SHAs, actual timings, pushed main parity, changed files, and remaining prerequisites for Order customer quote cutover.

Forbidden

No writes/restarts to shared :3000/:5088/:5432, tooba_alpha or shared store-alpha, RabbitMQ/Redis; no migration on shared DB; no touching unrelated dirty Admin work, TB-075/079 logs or stashes; no rebase/reset/autostash/force-push. If scratch required, loopback only, own container/port/db, explicit fail-closed isolation guard, cleanup by exact PID/path. No real PSP/SMS. Do not launch next task.

STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
END_TOOBA_TASK
