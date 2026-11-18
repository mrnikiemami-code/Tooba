# TB-279-R3 — user review

## Open these

- Operator Returns surface (unchanged frontend, refreshed backend): `http://127.0.0.1:3000/fa/admin/returns`
- Existing settled case history: `http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490`
  and `…/01a1e9c5-c729-7000-b614-79daf3d0e812`

## What changed in behaviour (backend only)

1. A NEW approve or reject over an undecided/reopened case now REQUIRES the decision revision the operator reviewed. A
   caller that sends nothing (or an older revision) is refused with the existing
   «نسخهٔ تصمیم بازبینی‌شده قدیمی است» refusal and NOTHING is written — no decision, no status change, no frozen amount,
   no allocation movement. Repeating an already-recorded decision still answers the stored fact with no new effect.
2. Goods physically received by a REJECTED (or cancelled) case stay occupied until a real, evidenced handback — even for
   old cases that have no per-delivery allocation rows. Contradictory receiving evidence can no longer be read as
   "nothing arrived": the rejection is refused with the existing evidence-conflict code and no row moves.
3. A partial acceptance still frees only what was never received: accepted 2, rejected-received 1, handback 0.5 keeps
   **2.5** occupied and leaves **0.5** returnable.

## What you will NOT see here

- No frontend visual change was made or claimed; the Next build is the same one you already have.
- There is no active return case, and this task was not allowed to create one or send any live POST, so you cannot
  exercise approve/reject/refund on a live case yet. The proof for those paths is the focused owner suite described in
  `closure-matrix.md`.
- No money, stock, provider, notification or database content was changed. The two cases above are history; they were
  only read.
- The historical key exposure remains contained in Git only; retirement is still a separate, explicitly unauthorised
  decision.

## Honest limits

The money/Inventory scenarios (accepted-portion restock `2` not `3`, cross-case `60 + 60 > 100` budget refusal) run
through the real owning Directory with scripted ports that succeed and record their arguments — not a real payment
gateway or inventory service. No concurrent two-writer race was committed. Worker PASS is not Architect or user
acceptance.
