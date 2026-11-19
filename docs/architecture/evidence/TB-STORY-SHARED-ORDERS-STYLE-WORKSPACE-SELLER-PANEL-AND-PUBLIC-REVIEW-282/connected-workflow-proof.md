# Connected workflow proof — TB-282

## Shared workspace

- One feature: `StoryManagementScreen` for admin + seller capabilities
- Orders-style columns: thumbnail+title, shop label, display state, slides, start/end, last change, compact `AppGridRowActionsCell`
- Seller paging/search via `/v1/seller/stories/query` (`querySellerStories`) — no `createClientGridQueryAdapter` over a client list
- Admin retains `queryAdminStoriesGrid`
- Detail loads fresh `getAdminStory` / `getSellerStory` by id (not `rows.find` from an old list page)
- VendorShell page `app/vendor-panel/stories/page.tsx` mounts the same screen with `SELLER_STORY_CAPABILITIES`

## Reviewed lifecycle

- Seller: create draft Dialog → upload slides → submit with durable intent
- Admin: approve-and-publish, reject-with-reason Dialog, schedule Dialog
- Rejection reason shown when present
- Occurrence keys frozen per act; API `storyOccurrenceKey` throws if missing (no silent remint)

## Guard tests

- `story-workspace-282.guard.test.ts` asserts media upload paths, merchant labels, VendorShell wiring
- `admin-grid-migration.test.ts` asserts seller query + no client adapter
