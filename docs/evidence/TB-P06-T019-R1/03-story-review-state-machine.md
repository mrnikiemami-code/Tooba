# 03 — Story review state machine (TB-P06-T019-R1)

Domain enums: `StoryReviewStatus` + `StoryStatus` (`StoryEntities.cs`).

## Review lifecycle (seller-origin)

```text
Draft (ReviewStatus.None, Status.Draft)
  → SubmitForReview → Submitted
  → Admin Approve → Approved
  → Admin Reject  → Rejected (+ Status back to Draft, reason required)
Rejected → edit → SubmitForReview → Submitted (resubmit)
Approved → Admin Schedule / Activate → Scheduled | Active
         → Expired | Disabled (admin lifecycle)
```

Admin-origin stories use `ReviewStatus.None` and skip review; publication is direct.

## Transition rules (adopted)

| Actor | Allowed | Forbidden |
|---|---|---|
| Seller | Create Draft; edit when `IsSellerContentEditable` (Draft + None/Rejected); Submit; resubmit after Reject | Approve, Reject, Schedule, Activate/Enable, change `SellerPartyId` |
| Admin | Approve (idempotent if already Approved); Reject with reason (max 500); Schedule/Activate only after `IsPublicationEligible`; Disable | — |

## Domain guards

- `SubmitForReview`: seller origin only; Status must be Draft; ReviewStatus None or Rejected.
- `Approve`: seller origin; Submitted → Approved (repeat safe).
- `Reject`: seller origin; Submitted only; non-empty reason; Status → Draft.
- `SetSchedule` / `Activate`: call `EnsurePublicationEligible` — seller story must be Approved first.
- No seller `/enable`, `/approve`, or `/activate` routes in `StoryEndpoints.cs`.
