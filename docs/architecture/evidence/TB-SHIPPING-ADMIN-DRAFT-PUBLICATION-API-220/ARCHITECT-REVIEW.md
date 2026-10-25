# Architect review — TB220
Source69a3daa635b2386e6766a0d601fce1fe69d70145; base4fc9ccac044aa2a084a833172b645e6b0191f6f8.

## Bounded ACCEPT, next action is the USER-TESTABLE ADMIN PANEL
Reviewed exact changes for commands/query/workflow/DTOs/validators, real admin+seller delegates, error descriptors/resources, Detached guard and endpoint facts. Root did not execute build/tests.
Owner authorization precedes body read and ISender. Request tokens are nullable+JsonRequired and validated; zero absent selection token distinct from omitted/null. Owned draft is binding source. Known stale draft/pointer refused before capture. Existing214 capture then219 selection each once; pointer CAS can refuse while staging capture remains. No combined transaction/atomicity claim. Current selected read exposes older capture rather than latest. Real write result drives response; no post-select catalog/context/Geography enrichment. Normal Save never publishes.
219 missing-row post-Reload Detached guard now present; source hygiene only, no executed vanished-row claim.

Worker8/8 endpoint facts, run twice on migrated disposable PostgreSQL. Do not count this as16 independent cases. Real authorizer/body/MediatR/handlers/capture/pointer exercised with controlled platform grants; lost-pointer refusal is scripted after a REAL capture, not a genuine concurrent race. Scope of acceptance matches this evidence. Auth test checks zero dispatch/mutation; malformed body+403 supports denial-before-body source ordering.
No actual Host/FE refresh or browser/shared DB was performed, so API is not yet loaded into the user's runtime.

## One small consistency correction in221 (no R1 envelope)
Withdraw handler returns Cleared/version0 for an absent pointer, while Contracts requires zero only Absent and the next GET returns Absent0. Return state Absent if clear.Value.Exists is false; retain Cleared for an existing positive-version row. UI strict decoder must not encode contradictory response semantics.
SnapshotUnavailable preflight currently leaves version unknown and final scoped CAS may select a proven replacement; this is not a new readiness policy. UI must show unknown/unavailable read honestly, never fabricate token0 from failure.

## Delivery/process
Actual120minutes versus35, many correction rounds: noncompliant, not process PASS. Next221 must deliver visible testable list/editor/publication workflow with real local runtime refresh and browser evidence, not more helper architecture or broad re-tests.
Storefront still uses old path. Do not represent publication Selected as activation/customer eligibility or label old catalog activation as publication. User must receive this admin trial before further Storefront development.
User's existing explicit nonproduction authorization covers required two checked-in migrations/current Host refresh; no fresh permission round. Preserve draft fields/unsaved browser work; real publish/withdraw trial affects pointer metadata only and must be reported.
