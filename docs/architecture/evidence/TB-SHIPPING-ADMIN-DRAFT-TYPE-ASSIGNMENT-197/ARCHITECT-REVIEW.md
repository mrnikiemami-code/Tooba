# Architect review — TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197
Date: 2026-10-22
Reviewed main/code:962897207bdc8f21bb07251108973a8776275722
Verdict: PARTIAL / REPAIR_REQUIRED. Bounded ownership/schema/Contracts/API implementation accepted as reviewed surface, NOT final slice acceptance.
TB196 bounded catalog acceptance unchanged.

## Source review
Compared3f05917..9628972: root-only nullable id, own column/CHECK additive migration, Application port+queries/command, Contracts-only Infrastructure adapter, module DI, six own endpoints, validators/errors/classification and scoped tests. No foreign implementation/DbContext/FK/join/catalog copy or Host/frontend change.
HTTP uses real owner-authorizer seam before body/dispatch; JsonRequired distinguishes omission from explicitNULL over HTTP. Root expectedRevision checked before catalog and again at write; directory preflight, single SaveChanges, concurrency clear/canonical409, no-op preserves root time. Read-only lookup carries raw LanguageId texts.
Real PostgreSQL source asserts owner-scoped persistence, cross-capability graph preservation, empty-guid CHECK and two-original-token assignment/rate race with XORwinner/revision2/actual loser0save.
Worker reports626/626, six endpoint facts/four storage facts; architect source reviewed remotely, tests not independently rerun. Seven corrections exceed one-correction contract, including a production readback fix; not all corrections were harness-only. Worker PARTIAL/process history remains. No repeat just to relabel PASS.

## Concrete production issue
ReplaceShippingAuthoringDraftTypeAssignmentHandler validates non-null id via catalog, saves the own root, then calls ComposeAsync which executes a SECOND foreign GetByIdsAsync after successful commit. A second-read outage/cancellation returns failure to the caller even though id/revision were already committed; retry with old revision then conflicts. This is an avoidable post-commit dependency ambiguity, not the acknowledged cross-module read-instant race.
Fix response composition using the single prevalidated snapshot plus actual saved root/revision (clear uses no snapshot). No outbound dependency call after write/no-op. GET remains a fresh truthful read and can later show inactive/missing; no distributed transaction, cache or catalog activation guarantee.

GET composition currently turns any response not exactly one matching id into Missing, including duplicate/wrong-id facts. Empty returned set is real Missing; inconsistent nonempty response must fail closed with existing canonical configuration-unavailable semantics, not fabricated absence. No new schema/error system is needed.

## Proof gaps (source, not speculative)
- Unknown/inactive/local-invalid preflight rejected in SAME context then later SaveChanges0 is not separately proved; scalar request has no "last collection entry", so test actual scalar rejection, do not invent a new batch.
- Actual DB assignment write failure/clear tracker/unchanged full graph/valid retry not executed.
- Endpoint harness substitutes the Application catalog port directly; separate adapter facts instantiate it manually. Neither proves real ShippingModule registration resolves adapter across scopes.
- Current listener facts explicitly start with Activity.Current null and assert no parent. They prove root spans/counts, NOT required parent/correlation/TraceId restoration. Exception fact asserts message/type, not same sentinel exception instance.
- No-op timestamp pre/post observed without advanced clock does not independently detect a restamp if time is fixed; use a seeded earlier persisted timestamp/controlled non-vacuous boundary in focused proof, no sleeps.
JsonRequired is HTTP/deserialization presence proof, not proof about arbitrary trusted C# object construction; don't claim more than it proves.

## Sole repair task
TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1, parent197: own Application handler/query response correction plus focused same-context/provider-failure/DI+parent-trace proof. No migration/Domain/owner/auth/DTO wire/route changes, no catalog rewrite or whole626 confidence rerun.
PUT non-null lookup exactly once before save, no post-commit lookup; GET inconsistent facts fail canonical503 using existing code; no new catalog dependency fallback.
Real DB fault may be raised by a named TASK-ONLY BEFORE UPDATE trigger on the disposable Shipping target, then removed exactly; no production migration/trigger or global failure policy. One failing write cannot publish an assignment; catch clears context; retry after removing fault succeeds.
All other migration/Contracts/owner/race evidence reused by unchanged source identity. No next feature/cutoff/UI until this is reviewed. TB185paused/defaultOFF/PendingAllocation intact.
