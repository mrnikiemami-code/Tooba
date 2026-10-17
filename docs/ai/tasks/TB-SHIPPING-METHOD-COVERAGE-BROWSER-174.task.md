PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-METHOD-COVERAGE-BROWSER-174
Parent-Task: TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: db80a0231ece17a6da3cf30ab4728d98509d67ba
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_PRIVATE_BROWSER_METHOD_COVERAGE_AND_CUSTOMER_QUOTE_ACCEPTANCE
Timebox: 45 MINUTES HARD; STOP_FOR_ARCHITECT

MISSION
Close the two honest TB-173 acceptance gaps, using real authorized isolated Admin browser actions and an authenticated storefront cart; prove method-specific coverage restriction affects only the selected canonical method, persists across reload, and is respected by StrictOwnedOption customer quote. Deliver a running, real interaction proof, not another wireframe or speculative implementation.

READ FIRST

docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (LOCKED addendum: absence of optional restriction = unrestricted, restricted-empty = deny; no sibling/provider inheritance).
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md and docs/architecture/evidence/TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173/RESULT.md.
Reusable disposable harness + fixtures from TB-166, TB-172, TB-173. Inspect exact current API, authorization, feature gate and existing UI handlers before proceeding.

WORK BOUNDARY

Before any mutation, verify HEAD == origin/main == Expected-Base-SHA. Do not silently proceed on drift: STOP and report. Preserve unrelated dirty files/stashes.
Start ONLY a task-owned loopback private PostgreSQL container/database, canonical migrations, isolated Development Host and isolated Next frontend. Verify and record isolation guard: NEVER operate on shared :3000/:5088/:5432, tooba_alpha, real store-alpha tenant data, or other worker services. Use sanctioned Development Admin actor via real AccessControl bootstrap and genuine Development customer OTP fixture; never bypass authorization. Use task-owned ports that are free. Secrets/OTP/tokens/cookies must be redacted and absent from committed evidence.
Seed minimal real scratch geography, two distinct owned shipping options tipax:standard and tipax:express, method-owned real active rate rules, one purchasable authenticated customer cart with legitimate offers/inventory/tax as required. Keep default-OFF production settings unchanged; enable ShippingOwnedRateAuthority only in private scratch overlay.
In the REAL Admin UI (/fa/admin/shipping-services), open the selected tipax:standard Edit dialog -> Rates & Coverage tab. Verify four top-level non-nested tabs, RTL, binding sourced from server, method coverage default Unrestricted, and View-mode controls read-only. Use actual UI controls to change to Restricted and add ONE chosen valid destination grant; save, full document reload and reopen, verify Restricted + exact destination from authoritative server; tipax:express remains Unrestricted/undeclared. Then use UI to return tipax:standard to Unrestricted and verify persisted read-back; historical grant remains auditable, never becomes sibling grant. If the UI fails, fix ONLY the smallest owner-correct frontend issue and test it, without redesign.
For the SAME actual authenticated customer cart, compare strict gate-ON server projection while standard is Restricted with an allowed destination vs an unsupported destination, and while method is restored Unrestricted; demonstrate standard inclusion/exclusion and express unchanged, using its own owned rate. Record response reason codes; invalid method-selection PUT must return 4xx and leave prior accepted cart shipping draft unchanged. No unsupported city/weight/slot/price invention, no zero/free fallbacks. If no trusted alternate destination can be exercised, explain exact gap and mark PARTIAL.
Capture genuine, unedited browser screenshots and DOM assertions of the method coverage UI before/after save/reload, the state in View mode, and customer-visible method selection or reason if possible; retain sanitized HTTP, authoritative SQL readback, and exact command/cleanup transcript. No mocks/fake screenshots.
Run ONLY focused relevant tests/typecheck if production source changes. No broad suite or evidence-only churn. Always cleanup by exact task-owned PID/executable and container identity; verify private ports released and shared services still intact.

ACCEPTANCE / VERDICT
PASS ONLY IF authorized real browser Admin write/reopen proves default -> Restricted with own grant -> Unrestricted, View mode read-only, sibling unchanged, AND authenticated customer strict quote inclusion/exclusion + invalid selection fail-closed are genuinely observed with SQL/HTTP/UI evidence. If any required leg is missing, mark PARTIAL (or BLOCKED with proven cause), not PASS. Do not modify release-wide pricing gate defaults, settlement/payout, checkout contracts, schema, other Admin screens, or shipping method creation. Do not assume absent coverage = prohibited in StrictOwnedOption; do not accidentally treat Restricted-empty as unrestricted. Respect 45-minute HARD box, stop before unsafe overrun.

RESULT CONTRACT
Report Base/Code/Evidence/HEAD/Origin SHA parity; exact files changed; concise screenshot and JSON paths; test counts; real verified vs unverified distinctions; safety/cleanup; status PASS/PARTIAL/BLOCKED and STOP_FOR_ARCHITECT. Commit only task-owned implementation/evidence and push fast-forward. No automatic next task. If production fix cannot safely fit timebox, stop with a minimal precise blocker; DO NOT manufacture a green result.
END_TOOBA_TASK