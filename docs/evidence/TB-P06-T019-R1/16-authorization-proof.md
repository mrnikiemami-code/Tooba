# 16 — Authorization proof (TB-P06-T019-R1)

## Coverage in `StoryFoundationTests`

### Static / endpoint

- Admin routes require `AdminPanelAccess.RequireAuthorizedAsync`.
- Seller has **no** `/enable`, `/approve`, `/activate` mapped routes.

### Seller isolation (`Seller_review_workflow_public_eligibility_and_authorization`)

| Case | Expected |
|---|---|
| Seller A list excludes Seller B story | Pass |
| Seller A get foreign → null | Pass |
| Seller A submit foreign → throw | Pass |
| Missing seller actor header | 401 via `SellerPanelAccess` |
| Actor A vs SellerParty B (no membership) | 403 |

### Review / publish

| Case | Expected |
|---|---|
| Activate before Approved | Domain `InvalidOperationException` |
| Reject empty/whitespace reason | Throw |
| Approve idempotent | Safe second Approve |
| Draft/Submitted/Rejected not public | Pass |
| Approved+Active public | Pass |

Frontend capability tests: `story-capabilities.test.ts` — seller `canPublish`/`canReview` false; submit only None/Rejected.
