# Inline editor DAM

- `ProductRichTextEditor` optional `onPickDamImage` + TipTap Image extension
- Inserts `/v1/storefront/media/{guid}` with `data-media-asset-id`
- `sanitizeArticleRichHtml` rejects base64 and external src
