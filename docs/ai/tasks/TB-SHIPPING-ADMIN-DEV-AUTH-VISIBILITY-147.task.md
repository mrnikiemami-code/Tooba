PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-DEV-AUTH-VISIBILITY-147
Parent-Task: TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 07a808797a7d8d3d3ee1982fde378c907c48f73c
Program: TOOBA-SHIPPING-ADMIN-FLAT-SERVICE-CLOSURE
Mode: BOUNDED_AUTH_PARITY_AND_NO_SILENT_LEGACY_FALLBACK
Timebox: 45 MINUTES HARD
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

ROOT CAUSE (ALREADY REPRODUCED)
The user's /fa/admin/shipping-services shows the old provider editor with «انواع سرویس (سطح ۲)». Server-side page.tsx calls buildUpstreamAuthHeaders which cannot read the Development admin actor in browser localStorage, leading to 403 admin.authorization.denied for GET /v1/admin/shipping-services/shipping-methods/one-level. Gate fail-closed silently renders ShippingServicesScreen. With a valid admin session the flat screen renders. This is NOT a Next cache or build issue.

OBJECTIVE — FIX REAL UI NOW; NO MOCKUP
Make the new independent-method Admin screen visible via the same authorized Development Admin workflow as existing admin pages, without requiring the user to perform a second manual login or type secrets in DevTools. Maintain secure authentication and server-authoritative gate decisions. This is a narrow continuation of TB-146, not a new general auth refactor.

REQUIRED IMPLEMENTATION

Read and reuse existing Development-only browser Admin identity/transport from admin-api.ts, shipping-services-api.ts, flat-method-catalog-api.ts. Inspect page.tsx, flat-screen.tsx, shipping-one-level-gate.ts and host-client.ts. No new auth secrets or new identity header names; do not hardcode or impersonate admin.
Eliminate mismatch safely. Preferred if compatible with existing implementation: small client bootstrap reads canonical GET through the same authorized browser Admin API transport and mounts ShippingServicesFlatScreen ONLY if the actual Host returns gate=Enabled AND ready=true. An already established secure server-side identity seam is an acceptable alternative. Do not activate the gate from localStorage, a query parameter, a method list, a build flag or a hardcoded fallback.
On 401/403/unknown/unready/offline: explicit localized denied/error/retry state, never silent fallback to legacy provider-level ShippingServicesScreen. No old «انواع سرویس (سطح ۲)» editor accessible from this route. Avoid flash of legacy page.
Preserve flat canonical method rows, only View/Edit/Delete, four non-nested detail tabs, authoritative method binding, permissions and RTL. Do NOT implement create-method endpoint in this task; leave honest blocked state. No Checkout, Order, Fulfillment, Settlement, migration, CSS redesign, shared Host code or policy changes.
Preserve SSR logged-in Admin and production unauthorized fail-closed. Never enable arbitrary identity injection in Production. No shared running server restart, cache deletion or DB writes. Do not use DevTools password/OTP scripts. No stashes, rebase or reset; preserve unrelated dirty files.

MANDATORY VERIFICATION

Under the user's ordinary authorized Development Admin actor flow, without extra auth-session login: actual browser /fa/admin/shipping-services must display 7 canonical methods and 21 row actions; no legacy provider modal. Read returns 200, gate=Enabled, ready=true.
With a real authenticated Admin session: flat page still works.
Without an authorized identity / under 403: clear denied state and no legacy admin editor.
Focused tests of auth/gate paths + frontend typecheck; only relevant tests, note existing unrelated FE-SIZE guard baseline.
Commit + push on origin/main only if safe; verify HEAD==origin/main. Report screenshots or DOM evidence, precise changed files, test results and remaining TB-146 CREATE-METHOD and write-roundtrip blockers. If the existing Development admin actor is unauthorized, STOP with evidence; never spoof a privileged ID to claim PASS.
PASS only on actual browser proof of normal authorized Development workflow; otherwise PARTIAL/BLOCKED. STOP_FOR_ARCHITECT; no automatic next task.
END_TOOBA_TASK
