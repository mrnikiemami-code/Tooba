Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `c92e8603-0d5f-4a03-9a48-9a00bf88eebb` (createdAtUtc 2026-10-16T20:04:37.5993269Z).


PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-METHOD-FINANCIAL-EDIT-156
Parent-Task: TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: c752e77bb8471b28ea4d3951b2a77d276bd8412d
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_REAL_METHOD_SCOPED_FIXED_IRR_EDITOR_AND_ISOLATED_READ_WRITE_PROOF
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; reserve last 10 min for focused verification, commit/push and result. If unsafe/not possible, STOP with evidence; no automatic task.

READ FIRST — LOCKED AUTHORITIES

docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/planning/MUST-FIX-BEFORE-RELEASE.md
docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (financial-mode and isolated-constraints addendum)
docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/architecture/evidence/TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155/ (especially evaluator/modes table/result)

WHY NOW
TB-155 implemented an authorized READ-ONLY method-specific financial status endpoint and honest Admin display. This is NOT a usable monetary editor. The existing owner supports tariff amounts/currency through ShippingRatePolicy, but has NO approved persisted FREE, COLLECT_ON_DELIVERY or PROVIDER_QUOTE mode. Do not pretend those modes exist, and do not make an unconfigured enabled method appear free. The next useful bounded capability is explicitly configuring an OWNED fixed IRR charge for exactly one canonical method, with trustworthy read/write confirmation. This task also closes the missing isolated HTTP+browser proof of TB-155 if safely achievable.

MISSION 1 — SHIP THE MINIMUM REAL METHOD-SCOPED FIXED-AMOUNT SETTING

Inspect the EXISTING Shipping-owned Admin rates/coverage command endpoints, policy records, authorization, validation, translation/binding seams and current one-level Rates & Coverage tab. REUSE the existing validated write API if it already stores a method+option-specific fixed rule; do not create an overlapping endpoint. If the existing API cannot express a method-scoped rule safely, add only the narrow owner-correct command/contract required, with authoritative server binding and proper permission. Do not infer serviceCode/optionCode by splitting the method code in the browser.
Allow Admin to set/edit a genuine fixed non-negative IRR charge for ONLY the selected method (e.g. tipax:standard) in its Rates & Coverage panel; optionally surface a compact link/summary in General. Server-side scope must exactly match the canonical method binding (serviceCode+optionCode) and tenant/context, never provider-level null-option rules or sibling options. Preserve existing rules; if there are competing owned rules or shared unresolved legacy policy, FAIL CLOSED with an actionable Persian explanation instead of overwriting/choosing one silently.
Save/reload display of amount and verified financial state from TB-155. An absent rule remains NEEDS_CONFIGURATION (not zero). A legitimate stored zero is a stated data amount, NOT an invented FREE_TO_CUSTOMER mode. Do not introduce speculative free/collect/provider modes or commercial settlement shares.
Keep the flat Admin list, exactly three per-row actions and four non-nested tabs; dynamic multilingual naming, RTL and existing CSS/design system unchanged. Do not add another provider/subservice level or global pricing form.

MISSION 2 — ISOLATED ACTUAL ACCEPTANCE, NOT PURE UI CLAIMS

Use a task-owned disposable Host/DB/frontend ONLY if quickly available under established TB-152/154 isolation recipe. Preserve shared :3000/:5088/:5432, store-alpha/tooba_alpha, running processes and unrelated dirty admin/log files; no shared DB write. Do not invent money or use production credentials.
First validate GET /v1/admin/shipping-services/{serviceCode}/financial-status?optionCode=... over authenticated HTTP (TB-155 seam), with status, reason and actual returned fields; no unverified screenshot claim.
In the scratch Admin browser, save a fixed IRR rule for ONE method, reload/reopen, verify exact amount and financial state, check sibling tipax:express is unchanged, verify server rate authority and any storefront quote dependency where possible. If user auth/dev setup or authoritative write capability is blocked, STOP/PARTIAL with precise blocker; no unauthorized shortcut.
Explicit regression: method active but no owned tariff is not free; shared provider-level legacy tariff does not leak to a method; multiple/conflicting owned rules remain not silently normalized; unauthorized writes reject. No migration or data rewrite unless clearly indispensable and approved by locked architecture; if necessary and exceeds timebox, report BLOCKED with a narrow next step.

TEST / QUALITY / STOP

Focused Shipping owner policy/endpoint tests + relevant Admin UI tests + frontend typecheck; no unnecessary full suites or 10-minute loops. Report baseline failures separately.
PASS only when real method-owned fixed IRR can be saved, read back and shown in a private authorized runtime, with sibling isolation and safe failure proven; otherwise PARTIAL/BLOCKED, not a cosmetic PASS.
Document minimal evidence (no tokens/secrets), actual route/write semantics, changed files, remaining modes not supported, exact SHA. Commit/push fast-forward when appropriate; verify HEAD==origin/main, never stash/reset/rebase/force-push unrelated user work. Stop for Architect.
Do NOT modify Checkout group identity/schedule, PSP, Settlement policy, payout, Admin create method, carrier calendars, EF logging or other backlog items in this task.
END_TOOBA_TASK
