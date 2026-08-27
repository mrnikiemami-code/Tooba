# Evidence — TB-P06-T019-R1

**Shared Story Management — recover backend WIP + one Admin/Seller component system**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T019-R1` |
| Parent | `TB-P06-T019` (superseded by architect rescope) |
| Predecessor | `43585f8904bf31736ab15746aefcd43889c7a507` |
| Migration | `AddStoryReviewOwnership` |
| Commit message target | `feat share Story management across admin and seller [TB-P06-T019-R1]` |
| May report readiness | `SHARED_STORY_MANAGEMENT_LIVE` (NOT `PRODUCT_FULLY_READY`) |

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-wip-recovery-classification.md` | KEEP / REPAIR / DROP of interrupted WIP |
| 02 | `02-backend-wip-reconciliation.md` | Preserved review/ownership direction + repairs |
| 03 | `03-story-review-state-machine.md` | Draft→Submitted→Approved\|Rejected; publish rules |
| 04 | `04-story-scope-authorization.md` | Seller own-only; admin review; SpiceDB headers |
| 05 | `05-shared-story-component-architecture.md` | `StoryManagementScreen` shared |
| 06 | `06-admin-shared-story-management.md` | `AdminStoriesScreen` thin wrapper |
| 07 | `07-seller-shared-story-management.md` | `/vendor-panel/stories` |
| 08 | `08-story-capability-matrix.md` | Admin vs Seller matrix |
| 09 | `09-admin-review-in-existing-panel.md` | Approve/reject in same panel |
| 10 | `10-storefront-story-no-drift.md` | Storefront untouched |
| 11 | `11-seller-story-attribution.md` | No extra storefront chrome |
| 12 | `12-seller-story-cta-safety.md` | Existing CTA rules + seller path |
| 13 | `13-story-public-eligibility.md` | Public eligibility filter |
| 14 | `14-story-i18n-market.md` | Locale/market brief |
| 15 | `15-story-review-events.md` | Deferred push; status in UI |
| 16 | `16-authorization-proof.md` | Isolation / review tests |
| 17 | `17-boundary-proof.md` | Story module only |
| 18 | `18-integration-tests.md` | `StoryFoundationTests` 3/3 |
| 19 | `19-final-validation.md` | Validation placeholders |
| 20 | `20-final-runtime.md` | Runtime placeholders |
| 21 | `21-sot-update.md` | SoT / readiness notes |

## Shared UI entry points

- Shared: `src/frontend/app/stories/management/StoryManagementScreen.tsx`
- Admin: `/admin/stories`
- Seller: `/vendor-panel/stories`
