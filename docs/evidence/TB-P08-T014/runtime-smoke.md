# TB-P08-T014 — Runtime smoke

## Status

**PASS (API + pages)** after Host restart on T014 bits.

## Checks

| Check | Result |
|-------|--------|
| Incomplete Draft readiness | `canPublish=false`, requiredMissing>0 |
| Publish incomplete | rejected `content.publish.not_ready` |
| Admin preview API | 200 |
| History list | 200 |
| FE EDIT + Preview pages | 200 |
| Ready Draft (author+body) readiness | `canPublish=true` |
| Publish → Unpublish → Republish | all 200 |
| History event types | `article.published`, `article.unpublished`, `article.republished` (+ draft_created) |

## Notes

Interactive Jalali picker UI not browser-automated; API schedule/readiness covered by focused tests. `USER_VISUAL_ACCEPTED=NO`.
