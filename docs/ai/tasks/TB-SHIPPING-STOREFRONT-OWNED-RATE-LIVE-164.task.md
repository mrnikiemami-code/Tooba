TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164
Parent-Task: TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 44d604fd9ca82557100f417acfaef2e65e125cdd
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_ISOLATED_AUTHENTICATED_STRICT_OWNED_RATE_ACCEPTANCE
Hard-Timebox: 45 minutes; STOP_FOR_ARCHITECT, no automatic next task.

Mission

Prove, NOT assume, that the TB-163 default-OFF strict owned-rate integration prices a real authenticated customer's two-seller Shipping draft and read-only payment draft from the Shipping owner's exact-option IRR policies when explicitly enabled ONLY in a disposable scratch Host. Preserve all shared runtimes and production defaults. If an actual seam defect is found, apply one narrow owner-correct fix with focused tests; otherwise evidence-only.

Mandatory baseline and guard
Verify HEAD == origin/main == Expected-Base-SHA; if diverged STOP and report. Read recovery docs and TB-159..163 RESULT evidence, especially TB-154 browser/customer harness and TB-157/158 Admin scratch authorization. Check current clean/dirty paths; never stage pre-existing unrelated changes.
Stand up unique loopback-only disposable PostgreSQL, one scratch tenant, migrated Shipping + Geography with repository MigrationRunner, private Host and (only if necessary) private frontend. Independently verify all connection strings, tenant mappings, ports, container names, gates and edition against a fail-closed isolation guard BEFORE writes. Never touch shared :3000/:5088/:5432, tooba_alpha, existing store-alpha data, RabbitMQ/Redis or other workers. Use real Development customer fixture auth, not admin dev actor as customer. No PSP, SMS, final payment or order commit.
The only enabled authority flag may be in the scratch-only Host overlay: Tooba:ShippingOwnedRateAuthority:Enabled=true. Keep all committed appsettings/defaults false. Do not turn on observe-only shadow gates.
Acceptance scenario
Seed EXACT method-owned Shipping rate policies for tipax:standard and tipax:express (e.g. 205000 and 480000 IRR), and applicable actual coverage declarations / grants per TB-161. Confirm owner strict quotes on the chosen destination. Seed a provider-only legacy tipax rate with a different amount to show it NEVER leaks to either strict method; no missing rate becomes free. Use real Geography destination IDs, seeded offer/category tax rule as needed (no fake 0-price). Ensure only proper, unconditioned rates are used unless trustworthy parcel measurements actually exist; conditioned rates must yield MeasurementRequired, not guessed weight.
Real authenticated two-seller cart with tipax:standard for both sellers, separate authorized schedules. Fetch Shipping projection: both method entries have owner-quoted 205000 IRR (not legacy configured amount). Save both groups, re-fetch projection and read-only payment draft: two groups, each 205000, shipping aggregate 410000 exactly, identical persisted group IDs across repeat/save and correct line memberships, schedules, tax/payable arithmetic; no checkout/payment/order rows created.
Negative test in isolated scratch: leave tipax:express without a method-owned rule or set it restricted without allowed destination; projection must exclude it and expose an honest unavailable reason, and attempted selection of it must fail CLOSED with no persisted corruption. Repeat provider-only fallback attack to prove no inheritance. No quote error may silently switch to legacy price or 0.
OFF parity: restart ONLY the disposable private Host with enabled=false, observe its original legacy configured price / semantics and no owner/geography quote calls where instrumentable. Verify unchanged shared production defaults by source check. Restore ON state only in scratch if further evidence needed.
If time permits, private authenticated browser view may show the quoted amount; DO NOT claim visual proof unless a real DOM/screenshot is captured. Primary deliverable is HTTP+DB provenance and fail-closed behavior.
Verification and evidence

Run only focused tests relevant to touched files, build if code changed, and one bounded private HTTP+DB journey. Save sanitized request/response summary, rate-source provenance, quote reason matrix, effective gate, schema/tenant isolation guard, DB readback, PASS/FAIL matrix, cleanup proof under docs/architecture/evidence/TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164/. Redact tokens, OTP, passwords/cookies. Include actual counts and disclose any failing step. Do not conceal unresolved downstream readiness or invent financial payout allocation.

PASS / PARTIAL / BLOCKED

PASS only if authenticated end-to-end ON proof (projection -> save -> re-read -> read-only payment draft) shows strictly method-owned values and correct per-group sums, rejects provider/sibling fallback, and OFF compatibility is demonstrated in private scratch, with all private resources cleaned. PARTIAL if implemented but any decisive live proof missing; BLOCKED when a specific upstream technical prerequisite prevents safe execution. No broad feature work, no premature storefront cutover, no unrestricted zero fallback.

Commit/push bounded code or evidence to main only after validating origin parity; cite exact SHA and retained pre-existing dirty paths. End with the standard BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT record and STOP_FOR_ARCHITECT; no next task.
END_TOOBA_TASK
