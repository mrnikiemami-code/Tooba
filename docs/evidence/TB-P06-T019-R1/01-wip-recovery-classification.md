# 01 — WIP recovery classification (TB-P06-T019-R1)

Predecessor: `43585f8904bf31736ab15746aefcd43889c7a507` (`HEAD == origin/main`).

Uncommitted Backend Story review WIP from interrupted TB-P06-T019 classified:

| Path | Decision | Notes |
|---|---|---|
| `StoryEntities.cs` (Origin, ReviewStatus, ownership, Submit/Approve/Reject, `IsPublicationEligible` / `IsPubliclyVisible`) | **KEEP** | Matches shared management + auth scope |
| `StoryContracts.cs` | **KEEP** | Seller + admin review contracts |
| `StoryDirectory.cs` | **KEEP** + **REPAIR** | Seller list/create/submit; admin approve/reject; public query hardened for eligibility |
| `StoryDbContext.cs` | **KEEP** | Column/index mapping; migration added |
| `StoryDevelopmentSeed.cs` | **KEEP** | Admin active + seller draft/submitted |
| `StoryEndpoints.cs` / `StoryPanelComposer.cs` | **KEEP** | `/v1/seller/stories` + admin review routes; no seller activate/approve |
| `StoryFoundationTests.cs` | **REPAIR** | Expanded review workflow coverage |
| Frontend Seller Story UI | **N/A** | None existed at interrupt — out of this backend slice |

## Decisions summary

- **KEEP**: all compatible review/ownership WIP (domain, contracts, directory seller/admin APIs, endpoints, seed, DbContext mapping).
- **REPAIR**: `GetPublicStoriesAsync` eligibility filter; Host.Tests review cases; EF migration `AddStoryReviewOwnership`.
- **DROP**: none of the review ownership direction.

Storefront Story UI: untouched (frontend not modified in this slice).
