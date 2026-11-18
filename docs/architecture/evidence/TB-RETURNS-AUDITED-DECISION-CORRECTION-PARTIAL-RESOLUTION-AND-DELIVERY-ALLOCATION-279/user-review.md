# TB-279 — What to review (user review guide)

Result: **PASS within the issued consolidated scope** — NOT Architect ACCEPT and NOT user functional/visual acceptance.
This page says exactly what you can look at, what changed on screen, and what cannot be demonstrated live yet.

## 1. Review URL

```
http://127.0.0.1:3000/fa/admin/returns          (queue)
http://127.0.0.1:3000/fa/admin/returns/{id}     (case workspace)
```

Served identities: Host `5088` (PID `33892`, `/health` 200, `/health/ready` 200, tenant `store-alpha`) and Next `3000`
(`BUILD_ID 8fLqIGZTLx0CUcUquOuUG`).

## 2. What is new in the admin workspace

Three deliberate admin acts, each opening **one** existing design-system `Dialog` (no inline expanding forms, no
stacked dialogs, no second toast container — outcomes use the app's real `react-toastify`):

| Action (fa label) | What it does | What it refuses |
| --- | --- | --- |
| «بازگشایی پرونده برای بررسی دوباره» | appends a reviewed correction, returns the case to review, re-acquires its own released delivery capacity | a case with any refund attempt, custody fact, item disposition or settlement fact, a courier case, an undecided case, a stale review (`version_stale`), a sibling case that already took the capacity |
| «ثبت برنامهٔ تفکیک اقلام» | one reviewed plan per requested item (`accepted + rejected == requested`, `accepted <= received`), freezes the approved amount from accepted items × stored unit price before any money moves | a plan that accepts more than was received (refused, never clamped), a plan that leaves part of the claim unaccounted, a stale plan version |
| «ثبت تحویل کالای رد‌شده» | records the actual handback of rejected received goods with a required proof reference and a server-derived recipient | over-handback, a blank proof, a foreign item, an already-settled physical fact |

The case header now shows the **effective** decision (after the append-only revisions, falling back to the immutable
baseline) and renders «در انتظار بررسی دوباره» for a corrected case instead of inventing an approval.

Customer side is untouched from 278: nothing preselected, per-line product/unit/ceiling/step, per-line or shared reason,
exact integer-millionth quantities, unselected goods untouched.

## 3. What you will NOT see on the current data (and why)

All three stored cases are settled, read-only history:

- `01a1e6b0-f33a-7000-80ee-beffb6344490` (your first case)
- `01a1e8fc-c74e-7000-a155-fecfa1a3bf1a` (the 276 review case)
- `01a1e9c5-c729-7000-b614-79daf3d0e812` (your second case)

Each already owns a receipt, a decision and a succeeded refund attempt, so the correction gate correctly refuses to
reopen them. On those cases you will see the truthful state — `canReopenForReview=false`, blocker
`return.correction.state_invalid`, and **no** action offered — rather than a button that pretends to work. No case was
reset, cloned, reversed or re-created to make a demo, and no new business write was performed.

So the *served* proof for the three new dialogs is: the typed refusal on the real case, the served production chunk that
contains all three verbs and «بازگردانی», and the mounted real-component suites (`17/17`) that exercise open → fill →
refuse/replay → close. A live walkthrough of a successful reopen needs a lawful new case whose decision has no
financial or physical effect — that is a separate decision for you, not something this task fabricated.

## 4. Proof you can re-run

```
dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests -c Release
      -> 172/172   (includes ReturnsDecisionCorrection279DirectoryTests 11/11, ReturnsDecisionCorrection279Tests 13/13)
dotnet test src/backend/Host/Tooba.Host.Tests -c Release --filter "FullyQualifiedName~ReturnsModuleAmsc001"
      -> 34/34
npx tsc --noEmit ; npm run build
      -> 0 diagnostics ; BUILD_ID 8fLqIGZTLx0CUcUquOuUG
```

## 5. Honest limits (unchanged from the result)

- money/stock proven with controlled ports only — no real PSP, Wallet or Inventory effect;
- no committed two-writer race; serialization is proven structurally;
- the mounted FE suite is jsdom (behaviour, not geometry);
- the 276-era `admin-return-workspace-276.mount.test.ts` still fails on a fixture shape that has been stale since 277 and
  was not rewritten;
- no card-refund balance outside this Returns authority is modelled by the shared budget.

## 6. Verdict

`STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`. Nothing runs next unless you issue it.
