PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2
Parent-Task: TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: eaab66e86d7f0b70798d4f2b61c4122eef09d7ad
Bridge-Task-Row: afb1e8ac-f381-48f1-8cc6-d8c739470b77
Mode: ONE_TINY_REPAIR_REMOVE_THE_UNSAFE_NO_EFFECT_CLASSIFICATION_AND_PROVE_RETENTION
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ENVELOPE-PROVENANCE (declared)
The Bridge row for this task carries no content (`contentLen=0`, and `/api/tasks/next` answers 204 because the row was
already claimed before this wake), so the literal envelope text is not retrievable. This file is the repair record
reconstructed from the issued Architect review of the parent at
`docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/ARCHITECT-REVIEW.md`
plus the Bridge task identity and Expected-Base-SHA. It is not presented as verbatim envelope text.

ISSUED SCOPE (from that review, verbatim intent)
A and B are ACCEPTED and preserved unchanged: owning-unit precision at the receipt write boundary, exact scaled integer
client quantities, the 277 modal work and the approval/first-refund separation.
C is PARTIAL on exactly one point: `return.receipt.unit_quantity_invalid` is still classified as a proven no-effect
refusal, while the owning route emits the same code for an owning precision the lookup could not answer. Lookup
availability is not immutable body evidence, and a retried refusal cannot establish that an earlier unknown/in-flight
send will never commit — it may overtake an earlier request still before the case lock. Exact stored replay FIRST is
correct and must not be described as broken.

ONE TINY 277-R2
1. Remove that unsafe no-effect classification and correct its body-only comment.
2. Add a mounted unknown → unit-metadata refusal → exact-replay retention proof.
Constraints: existing owning refusal semantics and quantity rules remain; no backend change, no new production code, no
schema, no fixture, no case, no live POST; no screenshot, no delegation, no automatic continuation; the settled live
case stays read-only.

ACCEPTANCE SHAPE
The retained envelope survives a retry refused with that code, is not resolved by it, and is only resolved by the
authoritative replay of the same occurrence — proven on the real mounted workspace, not on a helper.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
