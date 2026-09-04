# TB-P08-T012 — Media root cause

## Root cause

`content-article-media-api.ts` called shared `adminHeaders(body !== undefined)` with a **boolean**. Shared `adminHeaders` expects `Record<string, string>`; spreading `true` adds **no** `Content-Type: application/json`. Featured/SEO/gallery JSON bodies often failed binding → silent or generic mutation failures.

## Secondary

- Parent `savePayload.coverMediaAssetId` could overwrite Featured after Media-tab assign (stale parent workspace).
- SEO assign paths swallowed errors (no toast).
- Media panel `onConfirm` fire-and-forget (dialog busy ended early).

## Repair

- Media writes use `adminHeaders({ "Content-Type": "application/json" })`.
- Media panel `onWorkspaceChange` syncs parent; Save uses synced featured id.
- Awaited confirm + error toasts for Featured/Gallery/SEO; unassign never deletes DAM asset (BE unchanged; UI copy clarifies).
