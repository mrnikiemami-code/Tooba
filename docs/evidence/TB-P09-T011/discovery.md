# Discovery — TB-P09-T011

- Orders grid kebab (`AdminOrderOperationsMenu`) had no `onCompleted`; `ServerGridPage` did not remount/requery after cancel/confirm. Status stayed stale until manual reload.
- `ProjectPaymentActions` still projected `confirm_deposit` / `reject_deposit` / `restore_deposit` after checkout cancel. `ProjectActions` still projected fulfillment forward codes when a fulfillment row existed for a cancelled seller.
- Direct POST of those codes was not fail-closed with a cancelled-specific human FA.
- Bulk toolbar treated empty intersection as mixed even for a single selected line (`bulkCodes.size === 0` ⇒ hide Pack Selected + show mixed warning).
- Row kebab used only `rowActionsForLine` (requires `orderLineId`). Seller-level `pack_selected`/`unpack` left the kebab empty.
- `BuildReturnDeadlineUi` set identical `DeadlineDisplay` and `RemainingDisplay` for `before_delivery`; FE concatenated both → duplicated «۷ روز پس از تحویل».
