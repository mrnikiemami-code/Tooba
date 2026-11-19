# UI contract handoff (R3) — NOT issuance

Seller panel remains existing VendorShell `/vendor-panel/stories` (`story.view`). No new panel.

## Wire samples (current)

### Seller submit (R3)

`POST /v1/seller/stories/{id}/submit`

```json
{ "expectedVersion": 2, "occurrenceKey": "submit-…" }
```

### Seller stop / return (retained)

```json
{ "expectedVersion": 3, "occurrenceKey": "stop-1" }
```

### Seller add item

```json
{
  "mediaType": "image",
  "mediaAssetId": "…",
  "expectedVersion": 1,
  "occurrenceKey": "add-…"
}
```

### Seller media upload

`POST /v1/seller/media/story/upload` (+ admin `/upload-for-shop`) with occurrence key; Media CQRS.

### Public card

`shopPublicId` from Catalog storefront public id when seller is public; otherwise null (no Party Guid fabrication).

UI delivery remains PLANNED_NOT_ISSUED pending Architect review of this result.
