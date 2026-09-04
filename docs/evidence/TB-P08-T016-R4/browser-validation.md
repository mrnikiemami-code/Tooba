# Browser validation (TB-P08-T016-R4)

Host: `http://127.0.0.1:5088` health ok  
FE: `http://127.0.0.1:3000`

Article: `01a06ac1-135f-7000-b45f-7089ee3d1add` (persisted `en-US`)

## Locale identity
- Badge: `زبان: English`
- Editor `data-dir=ltr`
- CKEditor English toolbar labels (Font Family, Insert video)
- Admin shell remains Persian; Article identity stays English

## History pager
- Tab History → English labels `Previous` / `Next` / `Page 1 of 1`
- Buttons disabled correctly when only one page (`totalCount=1`)

## Font Family / Size
- Font Family dropdown lists: Default, Arial, Tahoma, Verdana, Times New Roman, Georgia, Courier New, B Nazanin, Vazirmatn
- Font Size named + px sizes configured (`tiny`…`huge`, `12px`…`28px`)
- English LTR editor starts only when `translations` is not an empty array (fixed: `undefined` for EN)

## Video DAM
- Insert video → Media Library «انتخاب ویدیو از کتابخانه»
- Server-filtered video list shows uploaded `t016-r4-tiny.webm`
- Confirm inserts `<video … data-media-asset-id="01a06b5b-…">`
- Save persists video HTML (GET article body contains video + media id)
- Sanitizer hold tokens use HTML comments (NUL tokens leaked as `DAMVIDEO0` before fix)

## Image/File
- Insert image / Insert file toolbar buttons present (no regression smoke of full upload in this pass beyond prior green unit tests)
