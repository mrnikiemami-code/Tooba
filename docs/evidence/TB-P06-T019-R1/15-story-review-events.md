# 15 — Story review events (TB-P06-T019-R1)

## Deferred / no push

`StoryOutboxRegistration` exists (MassTransit Outbox table wired for Story schema), but this slice **does not** publish `story.submitted.v1` / `story.approved.v1` / `story.rejected.v1` to consumers. No notification delivery claimed.

## Audit fields (persisted)

On Story entity / admin-seller snapshots:

- `SubmittedByActorUserId`, `SubmittedAt`
- `ReviewedByActorUserId`, `ReviewedAt`
- `RejectionReason` (when rejected)
- `ReviewStatus` transition

## UI status

Review state shown via `StoryReviewBadge` / list columns and rejection reason block in `StoryManagementScreen` — status in UI is the operator-facing audit surface for this Task.
