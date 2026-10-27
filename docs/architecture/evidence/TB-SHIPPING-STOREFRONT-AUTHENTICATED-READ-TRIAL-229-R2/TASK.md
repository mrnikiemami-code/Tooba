PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2
Parent-Task: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: NORMAL_LOCAL_BUYER_SESSION_AND_REAL_CARD_OBSERVATION
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD20min / environment+login8 / mounted8 / recovery+commit4.
No new feature, calculator, architecture audit, test battery or parity implementation.
User already authorizes bounded local-development writes and verified Host refresh; do not request their password.

READ TOP + REVIEW
Read current recovery and229-R1 ARCHITECT-REVIEW. Four-defect repairs accepted FOR live verification only.
18backend+11frontend/typecheck already passed: do not rerun unless an actual source change needs a narrow check.
228 migration ALREADY applied; no MigrationRunner apply/reset/reseed. Preserve admin draft/rates/pointer.

1 REAL CONTEXT FIRST
Reverify actual Host/FE PID/path/start/environment/content root/health. Use retained R1 binary iff backend
source parity proves it is current; otherwise one isolated build, not rebuild loops.
Host environment exactly Development (no trailing whitespace), correct existing content root and settings.
Read the real local launcher safely without dumping credentials. Existing SingleStore store-alpha commerce
resolution is host-bound (Development tenant hosts include localhost and127.0.0.1); verify actual app origin,
BFF upstream and forwarded host/commerce context. Do not set tenant/owner from browser or alter edition.
Enable only Tooba__StorefrontCapturedPreview__Enabled=true plus explicit local AvailableDateCount
(e.g.7 AVAILABLE working dates, preview policy only). Checked-in Enabled stays false, TB185 unchanged.
R1 logged BOTH401 authentication_required AND "Commerce resolution failed". Correlate trace/request and
check each separately; no unsupported statement that planner caused401 or auth is the only blocker.
A launch/settings/host-context correction is authorized; do not weaken middleware/guards.

2 NORMAL TASK-OWNED BUYER, EXISTING ENDPOINTS
Root verified these real source paths:
- Modules/Identity/Tooba.Identity.Endpoints/Auth/IdentityAuthEndpoints.cs:
  POST/v1/auth/register and POST/v1/auth/login (normal handlers).
- src/frontend/app/api/auth/login/route.ts: existing CSRF validation, upstream normal login,
  buildAuthCookies(sessionId,refreshToken). No custom session cookies or invented tokens.
- src/frontend/app/api/auth/csrf and /api/auth/me existing BFF.
- storefront visible /login is OTP-only; that does NOT mean normal BFF password login is absent.
Create ONE unique local-only task test account through the existing normal registration API, with a
non-deliverable synthetic identifier appropriate to the actual supported IdentifierKind (e.g. unique
example.invalid email if supported) and random temporary password. Existing public registration, not SQL,
role/grant injection, dev actor impersonation, fake CustomerPartyId, auth reset or another user's account.
Then GET normal CSRF and POST existing /api/auth/login FROM the task browser/origin with proper CSRF.
Let that route set its OWN normal cookies; verify /api/auth/me succeeds and reports the same new account.
Do not persist passwords/session tokens/cookies in git evidence, screenshots or result bodies. Record
nonsecret account id, normal endpoint/status/correlation receipts only. No external SMS/email/OTP request,
no guessed real phone numbers, no reading/reusing user browser sessions or credentials.
If normal registration/account linking refuses, capture the exact stable code and stop promptly. Do not
invent a bypass or spend the timebox rewriting Identity. Retain fresh task-owned profile for user handoff
only if genuinely needed; no request for user secrets in report.
Fresh Chrome profile/freeCDPport/task-created target only. Never user tab enumeration/selection/navigation.

3 ACTUAL STORE PAGE
Use the task-owned account's normal cart flow: authorized create/cart-add APIs or UI, actual existing Offer
ids and actual pricing. Single-line then multi-line (same task cart may be modified normally; record versions).
No quantity/weight fabrication, no automatic all-offer assignment.
One bounded normal admin assignment on a DEV offer may be made through228 API only if needed, read and
record original before and exact resulting fields; no user draft/publication/rate alteration.
Navigate actual Storefront shipping route with real session and commerce context. Capture projection200
or exact blocking failure. No request interception/mocked payload/DOM text injection.
Inspect native desktop1440 and390 screenshots: actual captured cards visible, product lines present,
ONE authority/no duplicate legacy cards/fees, single-line included, per-group reason/unknownmoney honest,
scrollWidth==clientWidth. Known weight-gated capture can legitimately show measurement-required; this is a
valid unavailable LIVE state, not a positive quote. Positive two-group quote already fixture-proven byR1;
do not claim it live without actual real quote.
No Storefront selection/commit/order/reservation/payment; server not_ready guard stays.
No UI redesign. If an actual narrow mounting defect prevents rendering, one bounded correction permitted;
typecheck or single affected test only for that change. No broad refactoring.

LEAVE STATE / FINISH
Document live Host/FE identities, preview option/date count, fresh test account/cart ids (nonsecret), actual
trial URL, screenshots and GET receipts. Do not report login page as captured-card screenshot.
If preview renders correctly, may leave LOCAL preview ON for Architect/user visual test, clearly read-only
and blocked from checkout; document normal login steps without committing any secret. If blocked, restore
prior local configuration/runtime and report exact blocker. No invented recovery for lost local edits.
Evidence folder docs/architecture/evidence/TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2/.
Update ALL THREE recovery TOPs (AGENTS, START-HERE-IF-CHATGPT-IS-LOST, canonical shipping recovery).
No additional field/schema/gate design. Compact shared snapshot/per-order-group evidence and parity remain
next, unimplemented. Marketplace cross-owner proof not implied by SingleStore trial.
Exact-path commit/push/fetch parity, no add-A/history rewrite/Bridge/post-result script.
STOP_FOR_ARCHITECT. Automatic-Next-Task NONE. No user Visual ACCEPT claim.
