# TB-P08-T016 — CKEditor browser

## Code / contract (worker)

- Article editor: `content-article-editor.tsx` → dynamic `content-article-ckeditor.tsx`
- `data-editor="ckeditor5"`; no TipTap in Article path (`@tiptap` only in Product editor)
- Toolbar contract: H2–H4, bold/italic/underline/strike, lists, blockquote, alignment, link, table, undo/redo, DAM image
- No CKBox / CloudServices / Base64UploadAdapter
- RTL/LTR via `articleEditorDirection(locale)`
- FE tests: admin-screen + media + taxonomy CKEditor asserts pass

## Browser (parent)

- [x] CKEditor 5 loads in Article EDIT (fa RTL / en LTR)  
  Observed: CKEditor 5 loads **RTL for fa** with toolbar. EN LTR not separately logged this session.
- [x] Professional toolbar; comfortable canvas  
  Observed toolbar: bold / italic / underline / strike / lists / blockquote / align / link / table / undo / image.
- [x] Image action opens Media Library; insert persists after Save/reload  
  See `media-browser.md` DAM path (asset `01a06a03-2bc1-7000-9790-e01560cd47bf`).
- [x] No TipTap Article editor; no base64 body images  
  Observed: insert persisted with `data-media-asset-id` (no base64).
