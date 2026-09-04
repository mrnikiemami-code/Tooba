# TB-P08-T016 — CKEditor browser

## Code / contract (worker)

- Article editor: `content-article-editor.tsx` → dynamic `content-article-ckeditor.tsx`
- `data-editor="ckeditor5"`; no TipTap in Article path (`@tiptap` only in Product editor)
- Toolbar contract: H2–H4, bold/italic/underline/strike, lists, blockquote, alignment, link, table, undo/redo, DAM image
- No CKBox / CloudServices / Base64UploadAdapter
- RTL/LTR via `articleEditorDirection(locale)`
- FE tests: admin-screen + media + taxonomy CKEditor asserts pass

## Browser (parent)

- [ ] CKEditor 5 loads in Article EDIT (fa RTL / en LTR)
- [ ] Professional toolbar; comfortable canvas
- [ ] Image action opens Media Library; insert persists after Save/reload
- [ ] No TipTap Article editor; no base64 body images
