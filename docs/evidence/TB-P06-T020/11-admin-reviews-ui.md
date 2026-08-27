# 11 — Admin reviews UI (TB-P06-T020)

Date: 2026-08-27

## Existing surface (verified LIVE)

| Piece | Path / behavior |
|---|---|
| Route | `/admin/reviews` → `AdminReviewsScreen` |
| Shell nav | `admin-shell.tsx` — نظرات `live: true` |
| API | `loadAdminReviews` → `GET /v1/admin/reviews?status=Pending` |
| Actions | `moderateAdminReview` → `POST …/publish` and `…/reject` |
| UI | DataGrid pending queue with publish/reject; count from Host `TotalCount` |

## Host

Admin endpoints unchanged and remain registered:

- `GET /v1/admin/reviews`
- `POST /v1/admin/reviews/{reviewId}/publish`
- `POST /v1/admin/reviews/{reviewId}/reject`

Queue is Pending-only (moderation queue). Status query on admin list remains Pending semantics — no fake multi-status admin matrix invented.

## Extension this wave

No admin UI rewrite required. Seller list does not replace admin moderation. Audit actor/reason remains server-side on Publish/Reject domain methods (`ModeratedByUserId`, `ModerationReason`).

## Verdict

**ADMIN_REVIEWS = LIVE**
