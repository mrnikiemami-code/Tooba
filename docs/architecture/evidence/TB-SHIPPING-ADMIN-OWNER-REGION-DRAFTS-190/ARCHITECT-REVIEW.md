# Architect review — TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Date: 2026-10-22 (Asia/Tehran)
Verdict: REPAIR_REQUIRED — worker PASS is not Architect ACCEPT.
Reviewed head: b84877f4f633714989d6458f2be9a7aeda59f8f5
Code: 63b74264c552f5e5cddbe7dc2ae0ca2cacbcb1bd
Worker evidence: e6ca5cf6fe9b0fb7338b8c2ab0822baea3df85bf

## Accepted bounded foundations
The source has a Shipping-owned draft/group/area model, full owner predicates, independent read/write authorization, immutable Fulfillment binding, Geography Contracts ancestry traversal, module-call tracing, canonical API/error composition, and a prepared additive migration. No frontend or runtime cutover is present. Worker reports Shipping 526/526; Architect did not rerun .NET or a database here. This is source review, not relational acceptance.

## Blocking findings

### R190-01 — absent/null RegionGroups bypass the required validator
In ShippingAuthoringDraftValidators.cs both write validators place .When(x => x.Request?.RegionGroups is not null) at the end of the entire RegionGroups rule chain. FluentValidation applies it to the preceding NotNull as well. Therefore a missing/null collection is not rejected by the required rule. ToDomainGroups maps null to empty, but PrepareAsync calls groups.SelectMany on the original null collection. Non-nullable C# record annotations do not make JSON properties required. This is a reachable malformed-input failure, not a valid empty draft.

Require an unconditional stable-code missing-collection rejection; evaluate count/shape only after non-null, with safe parent Request guards. Prove omitted/null vs [] separately through real HTTP and the real validation pipeline. Do not accept null as [] or catch NullReferenceException.

### R190-02 — replacement mutates retained tracked groups before complete identity preflight
ShippingAuthoringDraft.ReplaceRegionGroups performs group.Apply inside its loop and discovers an unknown/repeated retained GroupId later in that loop. ValidateShape does not preflight ownership/membership or duplicate GroupIds. A valid changed first retained group followed by a foreign/unknown/repeated last group throws after the first group has already changed. The handler catches SemanticException; no SaveChanges occurs at that point, but the same scoped DbContext/aggregate can retain changed tracked entities. The requirement was validation before tracked-state changes, not merely fresh-context persistence staying unchanged.

Preflight the entire replacement and all node/group shape/membership invariants before any Apply/removal/Order/Revision/timestamp mutation. Test a retained first group followed by bad last identity; assert the same aggregate and same context ChangeTracker remain unchanged and a subsequent save cannot leak partial changes. Existing late-invalid test only covers geography overlap before loading tracked replacement and does not prove this case.

### R190-03 — ordered-group swaps are incompatible with immediate uniqueness
The prepared model creates ux_authoring_region_groups_draft_order as an immediate UNIQUE INDEX on (draft_id, group_order). Domain replacement directly assigns final zero-based positions to retained groups. A valid swap [A@0,B@1] -> [B@0,A@1] has no legal first ordinary UPDATE while both original rows exist under immediate uniqueness; EF may reject the dependency cycle or PostgreSQL may reject transient duplication. A transaction alone does not defer uniqueness. Shifting/deleting/adding ordered groups and selected-node moves across groups need the same review.

No executed PostgreSQL failure is claimed here: this is a source/schema conflict requiring a targeted regression and compatible persistence design before acceptance. Preserve stable group IDs and final uniqueness; do not remove constraints or discard/recreate retained groups merely to pass. Use the smallest module-owned strategy with a real optimistic-concurrency/atomic transaction boundary. If a staged save is necessary, explicitly document the one-transaction boundary and prove rollback; no partial commits. Any constraint change uses a new additive correction migration; do not edit TB-190 or older migrations.

## Next bounded task
TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1, parent TB-190, repairs these draft-save invariants only. No rate rows, taxonomy, frontend, new business modes, activation or certification. Only one task is issued. Exact task envelope is the downloadable artifact and, after worker receipt, its task receipt; this review is not a replay instruction.

Real PostgreSQL proof is required for the relational replacement claims. A disposable task-owned database/container on an explicitly unique loopback port may be used by that task, never shared :5432/tooba_alpha. If unavailable within bounds, report PARTIAL and preserve the open relational gate; neither InMemory nor script-string assertions substitute. Product functionality and deployment remain pending.

## Recovery / history
The task started on 676b7efc; the architect docs-only checkpoint bced3743 landed while it ran and was merged without rewriting history. This does not explain or excuse the defects above. Original result and task receipt remain historical, unchanged. TB-185 remains paused. Re-read fresh main and exact Expected-Base-SHA before any repair.
