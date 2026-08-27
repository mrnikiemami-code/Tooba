# 18 — Integration tests (TB-P06-T019-R1)

## `StoryFoundationTests` — 3/3 methods

| # | Test | Role |
|---|---|---|
| 1 | `Story_module_boundary_static_checks` | Schema + admin authz string + directory surface |
| 2 | `Public_visibility_status_cta_reorder_locale_and_admin_auth_behave` | Public seed, status windows, CTA reject, locale, admin auth (Docker) |
| 3 | `Seller_review_workflow_public_eligibility_and_authorization` | Full review E2E + isolation + SpiceDB seller access (Docker) |

SkippableFacts require Docker/Testcontainers PostgreSQL; when Docker available all three execute.

## Frontend (shared capabilities)

`app/stories/management/story-capabilities.test.ts` — Admin vs Seller flag matrix + `canSubmitStory` transitions.

Vendor nav includes `/vendor-panel/stories` in `panel-nav-integrity.test.ts`.
