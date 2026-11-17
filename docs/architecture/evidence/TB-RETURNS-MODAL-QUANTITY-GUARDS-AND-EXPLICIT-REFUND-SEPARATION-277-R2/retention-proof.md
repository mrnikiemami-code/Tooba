# TB-277-R2 — the retention proof in detail

The Architect's remaining defect was a classification, not a behaviour of the owning route:

> `ReadOwningUnitDecimalPlacesAsync` emits that SAME code for null Order context/unanswered precision as for invalid
> quantity. Lookup availability is not immutable body evidence. A retry refusal cannot establish that an earlier
> unknown/in-flight request will never commit: it may overtake an earlier request still before the case lock.

So the repair is a negative one — stop treating an answer as proof — and the only thing that can prove it is a walk
through the three states in order, on the real mounted surface.

## The states and what must hold

| # | Owning answer | What the surface must do | Why |
| --- | --- | --- | --- |
| 1 | transport loss (`host-unreachable`, no code) | keep the exact frozen bytes, show ONE uncertainty modal | a lost answer is never "no write" |
| 2 | `400 return.receipt.unit_quantity_invalid` | **keep** the exact frozen bytes, keep the retained card, keep saying "uncertain" | the same code also means "this separate lookup could not answer", which is not evidence about the earlier send |
| 3 | `200` with the stored snapshot for the SAME occurrence | clear the envelope, remove the card | only the authoritative reconciliation of that occurrence resolves it |

A committed occurrence replays, so nothing is lost by waiting — which is exactly why state 2 must not resolve.

## The fact as written

`app/admin/admin-return-workspace-277.mount.test.ts`
*“an unknown receipt send survives a unit-metadata refusal and only the exact replay resolves it”*

- mounts the real `AdminReturnWorkspace` for a `pcs` line (`unitDecimalPlaces: 0`) whose published action is
  `record_receipt`;
- enters a **lawful** quantity (`2` of `2`) through the real receipt dialog, so the case is about retention and never
  about the local quantity guard (which the other facts already cover);
- a `stubScripted` owning route returns, in order: transport throw → `400 { errorCode }` → `200` snapshot;
- assertions:
  - after state 1: `storedIntent()` is non-null, `action === "receipt"`, exactly one open dialog, notice matches
    `قطعی نیست`;
  - after acknowledging and retrying into state 2: `assert.deepEqual(storedIntent(), frozen)` — byte-identical bytes —
    the retained card is still on screen, the notice still says the outcome is uncertain, and it does **not** contain
    `اقدام انجام نشد` (the resolved-refusal notice);
  - after state 3: the envelope is `null` and the card is gone;
  - finally: `new Set(posts.map(JSON.stringify(body))).size === 1` over all three sends — one occurrence, one command,
    and the URL is `/v1/admin/returns/{case}/receipt`.

## The negative control

With the token temporarily re-added to `PROVEN_NO_EFFECT_REFUSAL_CODES`, the same fact fails at the retention
assertion with `actual: null` — the retry's lookup refusal had erased a possibly-sent intent. That is the defect the
Architect described, reproduced and then closed. The token was restored from a pre-experiment copy before any commit;
`git diff` for that file shows only the intended hunk.

## What this proof does not claim

- It is jsdom with a scripted route: it proves classification, storage bytes and the retained surface, not browser
  geometry or a real network.
- It does not re-prove the backend lookup: `ReturnDirectory.Receipts.cs` is unchanged, and the fact that the same code is
  emitted for unanswered precision is the 277-R1 owning-route evidence.
- It does not claim the committed-occurrence replay path was broken; the Architect is explicit that it is correct, and it
  is the path state 3 exercises.
