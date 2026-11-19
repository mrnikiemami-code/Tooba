# ARCHITECT REVIEW — TB-282-R9 — 2026-11-19

Decision: ACCEPT_FOR_OWNER_REVIEW_WITH_DECLARED_LOCAL_LIMITS.
R9 local startup recovery is accepted within Development/InMemory scope. No new implementation task issued. This is not owner UX acceptance, complete authorization certification or production readiness.

Reviewed code: 218a5e52d0a2bc6f518eb1e9d52b30f5cc6a4179.
Reviewed final implementation/evidence main: e5bcb134ad0c415fe547744b2f962dada187cb0d.
Method: independent GitHub source/diff/evidence review. Architect did not rerun the Windows runtime, builds or tests.

## Accepted local recovery
- Host only composes the owning AccessControl Contracts recovery inside the Development + skip-bootstrap branch, using existing trusted DevelopmentTenantCommerceContext/store-alpha scope.
- Recovery exits outside Development or configured InMemory mode before DB reads/projection. It reads non-archived tenant roles and their existing assignments via AsNoTracking; does not create roles/assignments/users, invoke EnsureBootstrap/Register or call SaveChanges.
- Existing AccessControl directory computes effective capabilities and projects them through the configured tuple writer; tenant/party memberships are reconstructed from existing platform/seller-scoped role assignments. This acceptance is for the canonical existing local assignments disclosed in RECEIPTS, not a new arbitrary email-to-admin policy.
- Three behavioral tests invoke actual recovery plus directory/adapter on controlled EF InMemory rows: empty engine -> expected tenant/party decisions; unassigned/cross-scope denied; non-Development/non-InMemory no-op. These were worker-run, not independently rerun.
- Source confirms no business bootstrap in the new skip branch. Observed assignment count 3 before/after is supporting evidence, not proof by itself that every durable table was unchanged.

## Live handoff evidence and honest limits
Worker reports admin session+actor Story list 200, assigned seller dev-header Story list 200, unassigned seller 403; dev-context 200. Admin and VendorShell /new active form DOM recorded at 390/1440 with no overflow and no review filter. This supersedes the R8 403 blocker for the tested assigned actors.
Host41552 DLL 54D3DE3744F226403B96BCB5AE0AD281DEA5805C68DA80EF14CD70557E6EEDC3, skip-bootstrap=1. Next34656 BUILD_ID 7nEINy82sBP3x0f0H6xdw (R8 FE retained).
Existing detail data was reported in list response; full served detail/dialog and live create/upload/submit/approve/public playback were not demonstrated. Owner now tests those existing business flows.
Seller API evidence used Development headers for an existing assigned actor. It does not certify all seller login/identity scenarios; Party-only identities without AccessControl assignment remain denied as disclosed.
Recovery is cold-start process projection, not a general permission-revocation/reconciliation service. Tests do not exhaust archived roles, malformed assignment/role scope mismatches, multiple scopes per actor, PostgreSQL translation or partial-recovery failure. No broader certification or new repair wave inferred from these limits.
Known unrelated critical-storefront bg-white failure stays disclosed. R6 bootstrap effects remain UNKNOWN; no cleanup/restore or new audit issued.

## Durable handoff
State: AWAITING_OWNER_REVIEW
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW

Do not issue R10 automatically or repeat the Story repair series without a concrete owner-reported failure. Preserve R5–R8 progress, current VendorShell, Returns, payout backlog and user work.

Owner review:
1. Admin /fa/admin/stories -> new: choose main storefront or shop, add image/video/product and check preview/save.
2. Existing assigned seller /fa/vendor-panel/stories -> new: own shop/product, save/reopen/edit, send for review.
3. Admin review/publish -> public /fa: playback and product/shop destination.
Owner saves/publishes create real business data; worker reports ZERO live Story/Media POST.
