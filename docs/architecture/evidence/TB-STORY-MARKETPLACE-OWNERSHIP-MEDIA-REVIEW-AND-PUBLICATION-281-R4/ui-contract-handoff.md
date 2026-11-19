# UI contract handoff (R4) — NOT issuance

Seller Story stays inside purchased-template **existing** VendorShell: `/vendor-panel/stories` with permission `story.view`. Shared feature; no parallel panel/menu/login.

## Wire samples (R4 transport)

### Remove item (admin/seller)

`DELETE …/items/{itemId}` body (or reviewed body shape used by FE):

```json
{ "expectedVersion": 2, "occurrenceKey": "rm-…" }
```

### Reorder items

```json
{ "itemIds": ["…"], "expectedVersion": 2, "occurrenceKey": "reo-…" }
```

### Reorder stories (admin)

```json
{ "storyIds": ["…"], "expectedVersions": [1, 2], "occurrenceKey": "reo-stories-…" }
```

### Approve-and-schedule / approve-and-publish / schedule / enable / disable / reject

Carry `expectedVersion` + `occurrenceKey` on the reviewed body. Schedule also requires stored content authorization server-side.

### FE

`story-api.ts` generates/reuses `storyOccurrenceKey` per reviewed act; does not mint a new key on blind retry of the same act. UI editor/list redesign remains **PLANNED_NOT_ISSUED**.
