# Video DAM upload / insert

## Root cause (empty library)
Client filtered a page of unfiltered assets → image-heavy pages produced empty video grids.

## Repair
- BE: `MediaDirectory.QueryAsync(..., contentTypePrefix)`
- Host: `?contentTypePrefix=` / `?kind=video|image|file`
- FE: `queryAdminMediaLibrary` + dialog pass prefix per `assetKind`

## Sanitizer
- Video hold tokens changed from NUL-delimited `DAMVIDEO` to HTML comments `<!--TOOBA_DAM_VIDEO_n-->` so `DOMParser` style walk does not strip holders.

## Browser proof
- Library lists uploaded `t016-r4-tiny.webm` under video kind
- Insert + Save: article body retains `<video … data-media-asset-id="01a06b5b-…">`
