# TB-279-R2 — RETAINED CUSTOMER INTENT PROOF (F)

The customer act stays the exact, viewer-scoped, durable submission introduced before this task; R2 closes the four
lifecycle holes the R1 review found.

## 1. Epoch invalidated on unmount / close / retarget

`return-request-modal.tsx:187-201`:

- a mount effect claims a fresh epoch and its cleanup invalidates it, so no in-flight answer can paint, notify or clear
  after the surface is gone;
- closing/retargeting the surface bumps the epoch as well;
- every later read, answer, toast, callback, `finally` and busy-lock release compares the captured epoch before acting.

Mounted fact: **"unmounting while the first of two children is on the wire stops the second send and never notifies"** —
the first child is answered after unmount; no second child is sent, no toast is emitted and the retained act remains.

## 2. Fence BEFORE every child send

The send loop captures `started = epoch.current` and re-checks it immediately before each child goes on the wire, in
addition to the post-answer checks. A retargeted/closed surface therefore cannot begin the next child even when the
previous answer arrived successfully.

## 3. Legacy (v1 / tb278) evidence is unresolved, never "nothing pending"

`return-submission.ts:34-37/333-343` keeps the previous prefix and finds BOTH legacy key shapes
(`${prefix}${checkoutId}` and `${prefix}${viewer}:${checkoutId}`). A v1 envelope has no authenticated viewer, so:

- it is never adopted as the current viewer's act;
- it is never cleared on this viewer's behalf;
- a v1 envelope on the checkout BLOCKS a new submission until it is safely attributable/reconciled.

Mounted fact: **"an older viewer-less witness blocks a NEW act instead of pretending nothing is pending"**.

## 4. Child outcome compares the exact originating envelope

`recordChildOutcome` recomputes the stored envelope for the child (`return-request-modal.tsx:390-410`) and compares the
expected exact bytes (submission key, child key, body, viewer, checkout and stable keys) before updating. A same-key/
same-child replacement cannot be marked recorded or cleared by the old callback.

Mounted fact: **"replacing the frozen bytes while the answer is in flight never applies the old outcome"** — the stored
intent is preserved and the tampered child is not recorded.

## 5. Retained behaviour (unchanged)

- write-then-READ-BACK before the first send; unavailable/lying storage refuses to send;
- unreadable evidence is an explicit state, not "nothing pending";
- viewer scoping: another customer's act is never read, adopted or cleared;
- a child's answer is recorded against the exact act; recorded is terminal, unknown keeps its bytes;
- only the exact current act can be cleared; a newer act survives a late answer;
- tampered child bytes are refused on read, never replayed;
- same-line multi-reason quantities stay under one ceiling; only body-dictated refusals are no-effect.

FE unit suite: **29/29** across `return-api`, `return-quantity`, `return-submission` and `return-submission-279`.
Mounted customer modal: **11/11**.

## 6. Limits

- jsdom proves lifecycle/behaviour, not pixels.
- No live customer session/checkout exists, so no served submission was performed.
