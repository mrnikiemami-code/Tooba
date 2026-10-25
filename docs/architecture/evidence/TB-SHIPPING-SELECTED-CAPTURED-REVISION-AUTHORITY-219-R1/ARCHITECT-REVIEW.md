# Architect review — TB219-R1
Source4fa863c9150c79759d43a5b1624e4eaed2e856cc; basee4e3a1cf7b6fcc0e9ba7e84c77eb1526a49bb101.

## Bounded ACCEPT
Exact production/test diff inspected. SelectAsync/ClearAsync now reload the one clean scoped pointer before expected-token/no-op checks. RequireCleanTracker still precedes this work, and ordinary write CAS/unique mapping remains unchanged.
Domain Select/Clear compute NextVersion before any field assignment; overflow refusal therefore cannot leak target changes through later SaveChanges. Adapter maps SemanticException as a typed refusal.
The three executed additions use ACTUAL stale tracked A objects, independent B commits and current-token controls; max-version test checks same object/Unchanged/laterSave0/independentDB after each Select and Clear. This closes the two concrete reviewed defects. Retain219 architecture and provider evidence; no R2 loop.

Worker: unchanged-code red3failed/9passed; repaired12/12 real disposable PostgreSQL; build0 errors. Root did not rerun. Time26>20, implementation stop exceeded; no process PASS.

## Precise limit carried into220, not a new repair envelope
LoadPersistedPointerAsync returns tracked immediately after ReloadAsync and handles disappearance only in a DbUpdateConcurrencyException catch. A reload of a missing row can instead leave Entry.State Detached without throwing; returning that reference is not a verified absent-row result.
220 must add a post-reload Detached-state check and return null. No application path deletes pointer rows (clear retains version), so this is narrowly carried source hygiene, not a reason for another foundation test loop or schema rewrite. Do not claim the vanished-row branch was executed in219-R1.
Other provider/cancellation branches retain existing plain propagation and own-entry cleanup.

## Next concrete capability
220 adds authorized publication read/capture-select/withdraw API for existing drafts using existing auth/body/ISender/validators/resources. Capture and pointer update have distinct atomic boundaries: a pointer CAS refusal may leave a non-selected staging capture; no combined atomicity claim.
No customer readiness is inferred, no implicit auto-publish on ordinary save. FE list/publication controls and actual Storefront ownership/type/membership + projection/selection/commit remain next.
214/219 tables still absent from shared DB; no user-draft/runtime mutation in this review.
