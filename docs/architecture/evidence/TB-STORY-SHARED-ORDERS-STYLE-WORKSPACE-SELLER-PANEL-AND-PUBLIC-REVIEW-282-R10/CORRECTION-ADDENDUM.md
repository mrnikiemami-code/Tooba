# TB-282-R10 CORRECTION-ADDENDUM

## E — exact submit failure
Read-only Host evidence for seller actor `01a03628-…` / shop `01a030d1-…` / story `01a04246-…`:
items carried `mediaUrl` without `MediaAssetId`. `EnsureStoredContentAuthorizedAsync` throws `story.media.forbidden` (403) for seller URL-only rows, while prior `HasPlayableContent` treated URL as ready and published `submit` in `actionCodes`.

Aligned readiness with ownership: seller-scoped playable content requires MediaAssetId. Platform (null SellerPartyId) may still use URL. No grants/seeds; no clearing of owner rows.

## Next build
`next build` fails prerendering `/404` with `<Html> should not be imported outside of pages/_document` — pre-existing App Router issue, not Story-source. Served `next dev` for owner URLs; production BUILD_ID not claimed.
