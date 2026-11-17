PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1
Parent-Task: TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 42e2df1f328e16b1bd855cc57edde168071a600f
Bridge-Task-Row: 14671841-a144-4d59-ad9e-350b3c693b23
Mode: OWNING_UNIT_PRECISION_AT_THE_WRITE_BOUNDARY_EXACT_SCALED_CLIENT_QUANTITIES_AND_DURABLE_UNKNOWN_INTENT_RETENTION
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ENVELOPE-PROVENANCE (declared)
This Bridge instance answered the claimed task row with an EMPTY content field (`contentLen=0` for every claimed row),
so the literal R1 envelope text was not retrievable from the Bridge. This record is therefore the REPAIR RECORD
reconstructed from the issued Architect review
`docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/ARCHITECT-REVIEW.md`
plus the Bridge task identity and Expected-Base-SHA above. It is not presented as verbatim envelope text.

ONE TASK / AUTHORITY
`docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/ARCHITECT-REVIEW.md`
reviewed TB-277 at `fac017f94d3d6af09939eb495cc34f6231eb5aac` and recorded PARTIAL with three bounded defects. TB-277's
modal work, decision-only approval and explicit first-refund route are RETAINED unchanged; no reimplementation, no new
lifecycle stage, no renumbering, no migration.

A — OWNING UNIT PRECISION IS ENFORCED BY THE RECEIPT WRITE
TB-277 published the ordering unit's decimal places as read enrichment only: `NormalizeReceiptLines` checked positivity and
`numeric(18,6)` representability, and the serialized write checked the cumulative ceiling, but nothing read the owning
Order line's precision. A direct `POST …/receipt` of `0.5` for a 0-decimal/pcs line could pass.
R1 must refuse a NEW inspected quantity that does not land exactly on the OWNING UNIT's quantum, read through the existing
Order Contracts return context of the case's own seller order. Missing/foreign/unanswered precision is a typed refusal,
never a guessed six-decimal default. Exact stored replay must keep answering before any NEW capacity/unit check. Zero
migration, zero live business write.

B — EXACT SCALED-DECIMAL CLIENT QUANTITIES
`collectReceiptItems` added JavaScript `Number`s for usable/damaged and the draft ceiling was `max(0, requested - received)`.
`0.1 + 0.2` is `0.30000000000000004`, so legitimate portions against a `0.3` ceiling were rejected. Clamping, epsilon
tolerance, `toFixed` rounding or a silent scale guess is NOT acceptable. The field must also refuse a fraction the owning
unit does not accept, keep Persian digit/separator normalization, bound the shared usable+damaged ceiling, and block a line
whose owning precision is unknown instead of assuming six decimals.

C — THE PROVEN-NO-EFFECT SET MAY ONLY CONTAIN BODY-DICTATED REFUSALS
`PROVEN_NO_EFFECT_REFUSAL_CODES` still listed `admin.authorization.denied`, `admin.actor.missing`,
`order.operation.denied` and generic conflict/state codes. A later denial/conflict does not prove an EARLIER send of the
same frozen bytes did not write; keeping such codes can discard a possibly-sent financial intent. Only refusals determined
by the frozen reviewed body itself (or by an immutable identity that could never have been true) may remain.

D — VERIFICATION / SCOPE LIMITS
Controlled ports only; no real PSP/Wallet/Inventory effect, no committed fixture. Focused affected suites, one typecheck,
one frontend build, one backend build, one Host publish. No whole-module/parity/stress rerun, no schema change, no new
return case, no live refund/retry/recovery/notification/provider/waybill/money call. The exercised case
`01a1e6b0-f33a-7000-80ee-beffb6344490` and its source checkout stay READ-ONLY. No screenshots.

E — HANDOFF
Evidence under `docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/`.
Update CURRENT AGENTS/START-HERE and the Returns plan with the actual behavior and remaining limits; the TB-277 report and
the Architect review stay unchanged. Commit + push, verify HEAD/origin parity, and report one real review URL.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
