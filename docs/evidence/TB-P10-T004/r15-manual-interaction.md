# R15 manual interaction

R5 promote/review/confirm/reject preserved.

Chosen model: **same logical cycle + explicit `ManualReviewTransitioned` event** while Active (ExpiresAt becomes review hold; CycleNumber unchanged).

If the initial/review cycle already ended, a later successful reacquire starts a **new** ManualReview cycle. Admin confirm / paid durable closes CommittedPaid. Reject / unpaid release closes ReleasedByPolicy. No Released row is resurrected.
