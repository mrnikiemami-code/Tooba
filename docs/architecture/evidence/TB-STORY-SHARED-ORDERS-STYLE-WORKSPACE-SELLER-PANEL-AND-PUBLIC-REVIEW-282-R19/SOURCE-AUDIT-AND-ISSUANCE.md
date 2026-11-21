# SOURCE AUDIT / R19 ISSUANCE — 2026-11-21

Pinned source: d3de64c2c6692b551c8193e842bdd04fe99ceded. Independent architect source inspection only, no live business mutation or test rerun. Owner now authorizes ONE consolidated implementation task, not direct acceptance or another audit-only phase.

## Confirmed defects / task mapping
| Defect | Owning source | R19 |
| --- | --- | --- |
| Admin stop builds /disable but binder accepts seller /stop only | StoryManagementScreen.tsx, story-response.ts | A1 |
| Approved unpublished mislabelled Draft; seller Approved+Draft lacks return action | displayState, StoryPublicationPolicy, Story.ReturnToDraftForEdit | A2 |
| Matching storyId alone accepted for mutation completion; publish test returns Draft | story-response.verifyStoryTarget, 282r19-cta-edit.mount | A3/E |
| Arbitrary 4xx retires uncertain stored intent; transient status classification inconsistent | story-executor, story-admin-ops | A4 |
| Menu story.view vs generic panel access; silent demo shop fallback | Story authorizers, panel access, seller-api | A5 |
| UI shop/date filters/updated sort absent from server normalize allowlists | StoryLiveGrid, StoryAdminGridPolicies/Adapter/QueryEngine | B6 |
| Action refresh reloads default query | StoryLiveGrid queryAdapter, AppDataGrid load effect | B7 |
| Locale prefix dropped; grid default fa/rtl; residual hardcoded messages | basePath, locale resolver, grid bridge/copy/player | B8 |
| Detail error dead end; incomplete leave/context handling | detail load and scope/leave effects | B9 |
| Product responses not fenced/debounced | loadStoryProductOptions effect | B10 |
| Preview not actual Story playback; edit has no media replacement input | authoring JSX/player | C11-12 |
| Pending count/details remain appended, owner explicitly means in-page messages | pendingActs rendering | C13 |
| Public captions never rendered; any-video story treats JPEG cover as video | StoryModal, HomeStoriesSection, MapPublic | D14 |
| Image dwell starts before load; hidden/loading/accessibility incomplete | StoryModal timers/handlers | D15 |
| Open page does not revalidate expiry/stop; public query loads all candidates/items | HomeStoriesSection, GetPublicStoriesAsync/EnrichPublicAsync | D16 |

## Avoid stale claims
Paginated/debounced shop picker and admin shop submit exist in d3de64c2. Publish-now and preserved media exist in 16da85143. Do not revert them or call them still absent. VendorShell does directly emit the scope event; earlier “never emits” report was incorrect. Actual Storefront share query is consumed; do not invent a missing share link feature.
R17 owner direction overrides historical per-row result Dialog requirement. Latest user clarification: toast already closes; lingering in-page messages are the issue. Keep transient native feedback and safe on-demand recovery without appended warning prose.

## Data/proof truth
R18 old snapshot/no-live-write evidence was time-scoped. Later worker REPORTED deleting old stories, leaving four CTA test rows and real UI creation/deletion; actual historical data mutation is not independently verified by this audit and cannot be renamed ZERO. No further cleanup/restore/seed/live action authorized. R6 bootstrap effects UNKNOWN retained.
No automatic implementation beyond R19. No current Architect ACCEPT or owner/release acceptance. Exact task and receipt are committed; file artifact is supplied to owner. Bridge was not enqueued by architect here.
