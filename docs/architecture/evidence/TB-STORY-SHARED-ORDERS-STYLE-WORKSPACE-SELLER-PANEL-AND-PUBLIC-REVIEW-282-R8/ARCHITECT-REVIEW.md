# ARCHITECT REVIEW — TB-282-R8 — 2026-11-19

Decision: ACCEPT_WITHIN_A_AND_READONLY_B_SCOPE; LIVE_OWNER_REVIEW_BLOCKED.
Reviewed code bd23deeec736746170ed6fe101cd880ba1b2e33d / final main dcd5e4e383f819924b182d421ffcb7dce32c1857.
Method: independent GitHub source/diff/evidence review. Architect did not run Windows tests/builds/live pages.

A accepted: normal create and successful replay both call adoptBoundCreatedDraft; pending retry scope checked; actual mounted admin/seller tests click blocked Save/Add, then same frozen key/body replay, adopt id, remount and reuse recovered id. Malformed ok-only response remains pending. Existing executor rejects unresolved different occurrence. Test fixture seller detail omits seller ownership, so controlled tests do not fully prove real owner hydration; no full scope/public-player certification claimed. Shared extraction currently targets camel-case storyId/data.storyId; no assertion of all wire aliases or crash-atomic recovery.

B accepted for bounded read: TryBindExistingSnapshotAsync only finds existing user via Identity Contracts, sets process snapshot, and Development endpoint awaits it. No registration/membership write in new method. Two Host tests are source-text guards, not behavioral dependency tests. Reported actual Host+proxy dev-context 200 corroborates serving but is not a permission grant.

Live blocker remains: worker reports valid login/actor but Story list 403 admin.authorization.denied. Source diagnosis:
- AdminPanelAccess checks tenant#view against IAuthorizationGuard.
- InMemoryAuthorizationAdapter holds process-only tuples; restart discards them.
- Normal AdminDevActorBootstrap.EnsureAsync writes tenant membership, but skip-bootstrap suppresses this side effect.
- AccessControlDirectory.GetEffectiveAccessAsync reads durable assignments; SyncUserCapabilityTuplesAsync writes derived capability tuples. Snapshot/identity lookup alone cannot restore an empty authorization engine.
Do not disable authorization or run full seed bootstrap to get a green page. R9 authorizes ONLY existing-authority-derived Development InMemory projection recovery, no durable new grants. Required membership must be proved from real authority, not inferred from fixed admin email.

R8 source scope complete; no further Story UI repair issued. Runtime readiness, authenticated active-form geometry and owner acceptance remain unclaimed. R6 startup effects UNKNOWN; preserve disclosure.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
STOP_FOR_ARCHITECT_AND_USER_REVIEW
