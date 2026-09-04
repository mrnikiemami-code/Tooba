# TB-P08-T012-R1 — DAM integration

- Image toolbar action `damImage` calls `editor.config.damImagePicker` (React prop `onPickDamImage`).
- Parent `content-article-admin-screen` uses existing `pickDamImage` + `MediaLibraryDialog` + `damPickResolveRef` (no `window` globals).
- Insert HTML: `/v1/storefront/media/{guid}` + `data-media-asset-id` + optional alt/title inside `<figure class="image">`.
- No CKEditor upload adapter / cloud storage / base64 persistence.
- Article-use metadata stays in HTML; DAM asset catalog not overwritten by editor alt/caption.
