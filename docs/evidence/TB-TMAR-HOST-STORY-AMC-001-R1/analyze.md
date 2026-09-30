# Analyze — Host/Story AMC-001-R1

## Trigger

Architect residual after TB-TMAR-HOST-STORY-AMC-001 HOST_ZERO:
- Endpoints referenced Domain (`StoryReviewStatus`) for query binding
- `StoryHttpErrors` classified failures by Persian/English exception message text
- Canonical path should be SemanticException + ApiResponseFactory

## Ownership

| Surface | Owner |
| --- | --- |
| Host/Story | ABSENT (must stay closed) |
| HTTP | Story.Endpoints |
| ReviewStatus parse | Story.Application `StoryFailureMapper` |
| Failure codes | Story.Contracts.Errors |
| Failure HTTP map | ApiResponseFactory via thin StoryHttpErrors.From |

## Non-goals

- Reopen Host/Story
- Schema / frontend change
- Full Offer-clone ARCH-COMPLETE-002 foldering
