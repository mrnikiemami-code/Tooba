# Failure semantics — TB-TMAR-HOST-REVIEWS-AMC-001-R1

| Code | Origin | Preserved |
| --- | --- | --- |
| `reviews.duplicate` | `ReviewDirectory.SubmitAsync` (pre-check + DbUpdate unique race) | YES |
| `reviews.rejected` | Directory unpublished product; Domain `ProductReview.Create` validation | YES |
| `reviews.moderation.rejected` | Directory missing review; Domain `Publish`/`Reject`/`EnsurePending` | YES |

Unknown exceptions (including unexpected `DbUpdateException` after non-duplicate race) propagate without remapping to known Reviews codes.
