Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `e7953947-9598-48fc-877a-c188ae722ba5` (createdAtUtc 2026-10-16T20:29:14.0929323Z).


TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158
Parent-Task: TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 16f8d26ad82ae6f0b54afdf779b94ee6a8f0a6d6
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_PRIVATE_ADMIN_BROWSER_AND_STOREFRONT_QUOTE_ACCEPTANCE
Timebox: HARD 45 minutes; stop rather than extend or loop.

Mission

Close the two explicitly missing acceptance legs of TB-155/156/157 without adding another architecture proposal: (1) genuine authorized private browser proof that the existing flat Admin method General and Rates & Coverage views render method-owned financial status and persist an IRR rate edit through reload; (2) trace a genuine shipping customer quote for that same canonical method to determine whether the updated owner rate is actually used. The prior TB-157 HTTP+DB acceptance is already proved; reuse its runnable harness, do not spend the task rewriting it.

Mandatory setup and safety
First confirm HEAD == origin/main == Expected-Base-SHA, clean scoped files. If different, STOP with mismatch; never reset/rebase/autostash/force-push.
Read docs/architecture/evidence/TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157/README.md and its scripts; also TB-155, TB-156, TB-157 RESULT/evidence and locked Admin flat-method contract. Preserve provenance and existing financial semantic distinctions.
Create fresh disposable localhost-only Postgres, Host and private Next frontend using TB-157 harness (choose new collision-free task-owned ports/container/PIDs/paths, update isolation guards and cleanup appropriately). Product migrations, real authorization bootstrap and scratch-only geography fixture are mandatory. Do not use/restart/write shared :3000/:5088, :5432/tooba_alpha, RabbitMQ/Redis, store-alpha real data or another worker's runtime. Seed only private scratch data. Never collect credentials/tokens/cookies in evidence.
Follow authorized Development Admin actor and the real product AccessControl bootstrap; no bypass/mock allow-all, forged role, invented price or fake screen. Use server binding GET, never split methodCode client-side.
Deliverable A — real browser Admin acceptance
Launch private Next against private Host (not just commit launcher). Open /fa/admin/shipping-services with a real authorized Admin browser session. Check flat seven-method list and exactly 3 actions per row; inspect tipax:standard General and Rates & Coverage, four non-nested tabs, RTL, proper method binding.
Prove tipax:standard initial financial state, create its own FIXED IRR amount through the actual UI and read it back after full document reload. The General badge/amount must reflect authoritative GET, not a stale local value. Verify tipax:express sibling does not inherit price, and provider-level legacy row never becomes standard's money.
Change standard's existing owned amount through the actual UI, re-read on the server and reload browser; verify persisted changed amount, no additional ambiguous duplicate and stable sibling. Record actual DOM assertions, status codes, sanitized JSON, screenshots. If UX prevents the operation, identify exact code/path and apply a minimal owner-correct fix with targeted tests, then rerun acceptance. Do not mark PASS based only on HTTP or source guards.
Include fail-closed visual behavior for unconfigured/legacy shared/conflicting or unverified status where realistically achievable; never show missing tariff as free, zero as explicit FREE mode, or activation as quote readiness.
Deliverable B — genuine customer quote dependency
Using a private authenticated customer/cart and seeded merchant method, read the real storefront shipping projection or payment-draft price before and after editing tipax:standard owned rate. Identify which rate authority is used by the current storefront: Shipping owner rate rows, a catalog fixture, or another seam. Capture server response, group fee, and identity for standard and its sibling. Never infer quote propagation from Admin read-model alone.
If new rate propagates, record exact independent group fee and aggregate in real response, and compare express unchanged. If it does NOT propagate, do not invent a fix or claim ready: record owner-specific call chain and smallest necessary seam in the evidence; implement a narrow safe integration only if feasible within 45-minute timebox, authorization and existing contracts, with regression tests. Do not alter settlement payout, charge tax, or fabricate FREE/COD/provider-quote modes.
Do not call final payment, PSP, real SMS, real carrier API, or create order; read-only payment-draft allowed when legitimate scratch tax rules support it. A 409 tax fixture is not an excuse to fabricate rates; report it precisely if encountered.
Verification and stop rule

PASS only if both real private-browser save/edit/reload with sibling isolation AND real authenticated customer quote provenance/propagation are verified, with no shared mutations and complete cleanup. If only one leg succeeds, PARTIAL and clearly distinguish proven from unverified; if irreducible blocker, BLOCKED with code-level root cause, not another plan-only document. Focused tests/typecheck only for touched code, no broad suites or repeated rebuild loops.

Report: Base/Code/Evidence/Head SHA and origin parity; exact HTTP/DOM assertions; browser screenshot file paths; pre/post method-specific rates, sibling snapshot and customer quote; tests; real elapsed time; resources/ports and evidence of task-owned cleanup. Commit source changes only if necessary; save sanitized evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158/ and the task artifact under docs/ai/tasks/. Push fast-forward to main; STOP_FOR_ARCHITECT; Automatic-Next-Task: NONE.
END_TOOBA_TASK
