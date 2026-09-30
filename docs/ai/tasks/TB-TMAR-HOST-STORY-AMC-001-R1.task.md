PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-STORY-AMC-001-R1
Parent-Task: TB-TMAR-HOST-STORY-AMC-001
Channel: tooba-main
Claim-ID: 7f683356-7221-42aa-ae5d-99e0005c753d
NOTE: Bridge list API returned empty content after claim; Worker reconstructed scope from Architect residual + claim identity.
SCOPE:
- Do NOT reopen Host/Story (remain ABSENT / HOST_ZERO)
- Kill Endpoints -> Domain coupling (StoryReviewStatus parse leaves Endpoints)
- Kill message-based StoryHttpErrors classification; use SemanticException + ApiResponseFactory
- Reconcile SoT/recovery to Story R1 user-review stop
- Focused validate; commit/push on PASS; Bridge Result only
END_TOOBA_TASK
