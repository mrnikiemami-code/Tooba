# TB-277-R2 — result

**Task** `TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2` (Bridge row
`afb1e8ac-f381-48f1-8cc6-d8c739470b77`), parent `…-277-R1`.
**Base** `eaab66e86d7f0b70798d4f2b61c4122eef09d7ad` (the Architect's review commit of 277-R1).
**Status** PASS within the issued "one tiny 277-R2" scope. **Not** Architect acceptance, **not** user
functional/visual acceptance. 277-R1's A and B acceptances and all 277 modal/approval/refund work are preserved unchanged.

Decision:
`ONE_UNSAFE_NO_EFFECT_CLASSIFICATION_REMOVED_SO_A_TEMPORARY_OWNING_UNIT_METADATA_LOOKUP_REFUSAL_CAN_NEVER_RESOLVE_A_POSSIBLY_SENT_RECEIPT_AND_THE_MOUNTED_WORKSPACE_PROVES_UNKNOWN_THEN_REFUSAL_THEN_EXACT_REPLAY_RETENTION_ON_THE_EXISTING_RETURNS_SURFACE_WITH_ZERO_BACKEND_CHANGE_ZERO_SCHEMA_AND_ZERO_LIVE_POST`

## What the Architect left open, and what was done

The review accepted A (owning-unit quantum at the receipt write) and B (exact scaled client quantities) and marked C
PARTIAL on exactly one token. The owning receipt route answers `return.receipt.unit_quantity_invalid` for two different
situations:

| Emission | Meaning | Is it about the frozen reviewed bytes? |
| --- | --- | --- |
| `OwnUnitQuantity.IsAlignedTo` fails | the reviewed quantity genuinely misses the owning unit's quantum | yes |
| `ReadOwningUnitDecimalPlacesAsync` cannot answer | the owning order context/precision was not answered (null, foreign, unreadable) | **no — it is lookup availability of a separate request** |

A retry answered with the second form therefore says nothing about an earlier send of the same bytes that may still be
before the case lock. It was removed from `PROVEN_NO_EFFECT_REFUSAL_CODES`, and the set's documentation now states that
removal and why. No backend code, contract, route, error catalog or schema was touched: the owning refusal semantics are
exactly as accepted in 277-R1.

## Preserved unchanged

- **A** owning-unit quantum enforced before any row, `return.receipt.unit_quantity_invalid` typed refusal, exact stored
  occurrence replay answered FIRST (`ReturnDirectory.Receipts.cs` untouched).
- **B** integer-millionth client arithmetic, one shared usable+damaged ceiling, unknown precision blocks, Persian digit
  and separator normalization (`return-quantity.ts`, `return-receipt-quantities.ts` untouched).
- **277** one design-system Dialog per reviewed action, approval as a decision only, and the first refund as its own
  reviewed financial command (unchanged).
- The already-committed occurrence replay path: the review explicitly notes it is **not** broken and it is not
  re-described here.

## The mounted proof

`app/admin/admin-return-workspace-277.mount.test.ts` gains one fact,
*“an unknown receipt send survives a unit-metadata refusal and only the exact replay resolves it”*, mounted on the real
`AdminReturnWorkspace` with a scripted owning route:

1. the first `POST …/receipt` loses its answer → the exact bytes are **kept** in session storage and the one uncertainty
   modal is shown;
2. the retry is answered `400 return.receipt.unit_quantity_invalid` → the stored envelope is **byte-identical** to the
   reviewed one, the retained card stays, and the notice is still the uncertainty message, **not** the resolved-refusal
   message;
3. the next retry is answered `200` with the stored snapshot → only now is the envelope cleared and the card removed.

All three sends carry byte-identical bodies, so one reviewed occurrence produced one command and never a second one.

### The fact is a real guard, not a restatement

Re-adding the removed token locally makes the same fact fail at the retention assertion (the stored envelope reads
`null` because a lookup refusal erased it). The token was restored immediately; nothing of that experiment was
committed. This is recorded as a declared deviation.

## Verification (run once, after the final edit)

| Suite | Result |
| --- | --- |
| mounted `admin-return-workspace-277.mount.test.ts` | **9/9** |
| `return-quantity` + `admin-returns-work-queue` + `admin-error-map` + `return-api` | **48/48** (queue **26/26**) |
| `tsc --noEmit` | **0** |
| `next build` | **0** (`BUILD_ID GptasB03cazPtYoJp7aiE`) |

Served: Next `3000` PID `23760`, `/fa` and the case page `200`; the workspace chunk
`/_next/static/chunks/7952-1ecaa58d415c5e34.js` contains the **9-token** set and **no** occurrence of the removed token,
while `/_next/static/chunks/1323-979bf525b6515616.js` still carries the plain Persian/English explanation for that code
(the operator keeps a readable message even though it no longer resolves the envelope).

Host is untouched by this task — no backend change, so `5088` PID `21060`, dll SHA256
`8AA71DD82E6D737F9698A93D0F99EE7A77EB07E02916E4185DE9C271890E7948`, `/health` 200, `/health/ready` 200.

## Boundaries kept

- **No** backend edit, **no** new production code, **no** schema/migration, **no** new error code, **no** fixture.
- **No** live POST, receipt, decision, refund, stock, notification or provider call; the settled case
  `01a1e6b0-f33a-7000-80ee-beffb6344490` stays read-only completed history.
- **No** screenshot, delegation, historical agent resume or automatic continuation.

## Honest limits

The mounted suite is jsdom with a scripted owning route: it proves classification and retention, not browser geometry.
No live unknown-outcome walkthrough exists — the only live case is settled and no live POST is authorized. The
unit-metadata lookup failure itself remains a backend fact proven by the 277-R1 receipt suite; this task proves only
that the client no longer treats it as proof of "no write".

`STOP_FOR_ARCHITECT`; `automaticNextImplementationTask: NONE`.
