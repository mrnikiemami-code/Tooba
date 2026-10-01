# Repair — TB-TMAR-HOST-REVIEWS-AMC-001-R1

Removed message-text failure classification from Reviews.

| Before | After |
| --- | --- |
| `ReviewsFailureMapper` inspected `exception.Message.Contains("قبلاً")` | File **ABSENT** |
| Composer caught `InvalidOperationException` and remapped all to known codes | Catch-all remap **REMOVED**; SemanticExceptions originate at owner |
| Directory threw Persian `InvalidOperationException` | Directory throws `SemanticException(ReviewsErrorCodes.*)` |
| Domain threw Persian `InvalidOperationException` | Domain throws `SemanticException` with Rejected / ModerationRejected |

Host/Reviews was not reopened (remains ABSENT).
