# TB-P10-T004-R14 — Manual guard

Succeeded → `SubmitManualEvidenceAsync` throws already-succeeded before domain mutate.

Historical evidence rows stay attached. No new tracking/reference.

Rejected/Failed manual still uses `RetryManualAfterRejectionAsync` only when no Succeeded Payment exists.
