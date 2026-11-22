# TB-STORY-R22 — public player identity and delivery evidence

## Objective

Close the remaining reviewer-observed round after `c19e36ee5`: the Home rail must track the story that is actually playing, revalidation must survive a capped five-minute window and a failed refresh, covers must use the Media owner's explicit kind, and the seller query plus edit/save/reread path must be evidenced.

## Source-grounded findings

- `HomeStoriesSection.selectedStoryId` was set only when a rail card was clicked. `StoryModal` kept its own `currentStoryId`, so moving A → B inside the player left the parent still owning A. The removal effect then closed a healthy B player when A disappeared.
- The revalidation effect read `soonestEnd` and `revalidateTick`; after a revalidation the same deadline was not rescheduled, so a story ending more than five minutes away could stay stale indefinitely. The previous regression guard only checked the effect's dependency array.
- `PublicStoryCard.IsVideo` is derived from any slide being a video; it is not the cover's kind. The cover needs the Media-owned `StoryMediaAssetAuthorization.IsVideo`.
- The existing PostgreSQL Story relational class proved seller isolation, but not the seller grid's search/status/sort/count translation.
- The browser evidence requirement is a real form save followed by an independent reload, not a helper or HTTP-200 claim.

## Executed waves

| Wave | Change | Proof |
| --- | --- | --- |
| W1 | Parent owns the player's current story through `onCurrentStoryChange`; Home passes `setSelectedStoryId` to both modal layouts. | Mounted real `StoryModal` callback test and open-A → next-B → remove-A keeps the host open. |
| W2 | Revalidation moved to `useStoryRevalidation`; each tick advances an internal cycle so unchanged payloads reschedule; error refresh filters locally expired rows. | Real hook mount with a 20-minute `endAt`, capped test interval, ≥3 ticks; failed-refresh filtering fact. |
| W3 | Public card carries explicit `CoverIsVideo` from the Media owner when a cover asset exists; frontend never treats “unknown” as video. | Backend build 0 errors; `mapPublicStory` explicit/absent facts. |
| W4 | PostgreSQL seller grid proof added: search, status/review filters, title sort, page/count, foreign-shop exclusion, rollback-only. | Targeted Host test 1/1. |
| W5 | Real browser edit → save → reload → restore on `01a1f9d8-31af-7000-8e47-5da09230ff0d`. | CDP browser read changed title after reload and original after restore. |
| W6 | Recovery docs, this plan and the delivery receipt. | This folder + `AGENTS.md` CURRENT block. |

## Constraints retained

- No schema, migration, seed, grant or provider change.
- No Story/Media content mutation except the explicit browser save/restore required by this round; title restored and recorded.
- VendorShell, seller ownership, Contracts-only boundaries and existing publication/approval authorities unchanged.
