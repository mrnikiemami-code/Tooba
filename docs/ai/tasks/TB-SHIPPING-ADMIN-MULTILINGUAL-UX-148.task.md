PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-MULTILINGUAL-UX-148
Parent-Task: TB-SHIPPING-ADMIN-DEV-AUTH-VISIBILITY-147
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: bcac6610ebd1edc7c7c5c246f1d2ac21b669c075
Program: TOOBA-SHIPPING-ADMIN-FLAT-SERVICE-CLOSURE
Mode: BOUNDED_REAL_ADMIN_GENERAL_UX_MULTILINGUAL_PERSISTENCE
Timebox: 45 MINUTES HARD
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

READ FIRST / LOCKED

docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md
docs/planning/MUST-FIX-BEFORE-RELEASE.md
TB-146/147 evidence and current source; do not reopen closed auth bug.

ONE TASK ONLY — ACTUAL CODE, NOT WIREFRAME
Improve the actual method-specific General tab inside /fa/admin/shipping-services, using the existing Admin design system. The user has rejected the current bulky repetitive view of «پیشتاز» (post:express): multiple redundant identity blocks, very large warning/info card, excessive scrolling, active-language-only naming and unclear enabled/financial readiness. Deliver a compact polished RTL responsive UI and TRUE multilingual method-name editing via the established backend method translation endpoint. Do not merely show fake language tabs.

MUST DELIVER

Maintain existing flat canonical list of methods, 3 row actions View/Edit/Delete, 4 non-nested detail tabs and authorized Development Admin read parity introduced in TB-147. Do NOT switch to provider/child editor; do NOT change other tabs' behavior, existing design tokens/theme or storefront.
In selected method dialog: concise header with localized name, canonical method code and status; avoid redundant giant identity panels. Provider identity/serviceCode/optionCode is explanatory shared metadata only; compact and optionally collapsible, never a method-specific policy editor.
Dynamic LANGUAGE editing: discover actual allowed languages/IDs via existing authoritative Admin languages endpoint/contract, or reuse the already-existing Admin language selector API. Render one language selector per actual supported language, no hardcoded fa/en/ru names/IDs or synthetic translations. Prefill existing translations for each language; distinguish missing translations clearly. Save changed name to the existing canonical /one-level/{methodCode}/translations/{languageId} seam and update only selected method+language. Confirm saved value by GET/reopen in a safely isolated authorized test environment. Handle stale/failure/loading, cancel, name limits and RTL/LTR safely; no data loss from switching languages before save. Do not change canonical methodCode as part of name edit.
Show clearly separate statuses for METHOD ACTIVE/INACTIVE vs financial quote readiness. Because a canonical per-method financial policy/readiness endpoint is not yet implemented, DO NOT invent a 'ready' status, compute fees, set financial modes, claim quote-ready, or make 0-price assumptions. Display grounded 'financial status not configured/not available from current API' with brief direct explanation and future policy action only if an authorized endpoint exists. No fake activation toggle when server binding forbids it.
Keep each method isolated (post:express name edit cannot touch post:standard; provider metadata never writes policies). Normal view mode must be read-only; edit mode must expose meaningful controls and explicit save/feedback.
Navigation and usability: compact spacing, clear labels, useful status, keyboard accessibility, RTL and smaller screens, independently scrollable modal content with visible close/actions. The resulting visual interface must be substantially improved, not only comments/tests.

SAFETY + TEST

Shared :3000/:5088 and store-alpha/tooba_alpha are READ-ONLY; don't restart, purge .next, or write to shared DB. Preserve unrelated local dirty admin-fulfillment file, TB-075/079 evidence logs and existing stashes. No rebase/reset/autostash/force push. If actual round-trip requires writes, use an isolated disposable authorized scratch system; if unavailable, report verification PARTIAL rather than claiming it passed.
Targeted unit/component tests covering real supported languages, missing translation, switch without data loss, per-method save identity, error/reopen behavior, view/edit permission, plus frontend typecheck. One real browser READ-ONLY visual screenshot of improved dialog with language selector under normal Development Admin; do not fabricate test proof.
Do not touch server financial policy, create-method endpoint, Checkout/Fulfillment/Settlement or PERF-CRIT-001. Those are next backlog items, NOT this task.
Commit/push source and concise sanitized screenshots/tests/evidence; verify HEAD=origin/main. If HEAD has moved or shared changes conflict, STOP and report. Keep task files/evidence compact.

PASS GATE
PASS requires functioning real multilingual UI backed by authoritative languages and the existing correct method-scoped save API, an actually improved browser-rendered General tab, focused tests/typecheck and source committed. If an isolated save/reopen cannot be proven, mark PARTIAL and say exactly what is unverified. No documentation-only PASS. STOP_FOR_ARCHITECT for user real-screen approval. No automatic next task.
END_TOOBA_TASK
