# Public player proof — TB-282

## Honesty changes (`story-modal.tsx`)

- Removed fake like / comment / save / report local-only controls
- Share retains clipboard/native share with `?story={id}` URL
- Video: no `loop`; progress from `timeupdate` + advance on `ended`
- Image: persisted `durationMs` step when present
- Media error shows actionable «برو به اسلاید بعد»
- Shop attribution: `shopDisplayName`; link to `/seller-profile/{shopPublicId}` only when public id present
- CTA: refuse javascript/data/vbscript and bare Guid paths; external http(s) only for external

## Home rail (`home-stories.tsx`)

- Eligible stories only (`items.length > 0`)
- Opens modal when `?story=` matches an eligible id
- Rail label prefers `shopDisplayName`

## Guard

- `story-modal.mount.test.ts` source assertions for social removal, shop attrs, video events, share query
- `mapPublicStory` tests for shop fields / missing public id

## Distinction recorded

- Separate seller-profile story rail NOT required/built; canonical shop attribution/link IS required and landed
