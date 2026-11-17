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
The Bridge instance returned `contentLen=0` for the claimed row, so the literal envelope text was not retrievable. This
file is the REPAIR RECORD reconstructed from the issued Architect review
`…/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/ARCHITECT-REVIEW.md` plus the Bridge task identity
and Expected-Base-SHA, and is not presented as verbatim envelope text.

A — OWNING UNIT PRECISION ENFORCED AT THE RECEIPT WRITE
Refuse a NEW inspected quantity that does not land on the owning unit's quantum; typed refusal for missing/foreign/
unanswered precision; exact stored replay answered before NEW capacity/unit checks; zero migration, zero live write.
B — EXACT SCALED-DECIMAL CLIENT QUANTITIES
No floating-point usable+damaged addition, no `requested - received` drift, no clamp/epsilon/rounding; owning-unit
precision bounds each field; unknown precision blocks instead of defaulting to six decimals.
C — PROVEN-NO-EFFECT SET HOLDS ONLY BODY-DICTATED REFUSALS
A later denial/conflict never resolves an earlier possibly-sent intent.
D — Focused verification only; controlled money/stock ports; no schema change, no new case, no screenshots.
E — Evidence, AGENTS/START-HERE/plan update, commit+push, HEAD/origin parity, one real review URL.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
