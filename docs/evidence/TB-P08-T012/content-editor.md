# TB-P08-T012 — Content editor

## Delivered

- File: `src/frontend/app/admin/content-article-rich-text-editor.tsx`
- Wired from `content-article-admin-screen.tsx` Content tab (EDIT mode).
- Capabilities: paragraph/H2/H3/H4 selector, bold/italic/underline/strike, lists, blockquote, alignment, link, table, undo/redo, DAM image insert, grouped wrapping toolbar, large canvas (`min-h-[22rem]`), `transformPastedHTML` → `sanitizeArticleRichHtml`.
- RTL/LTR from Article Language via `dir` prop; toolbar labels switch with direction.
- No CKEditor.

## Workspace hierarchy

- Header card: title, status, language, dirty/save state, VIEW/EDIT badge, primary Save/Cancel.
- Archive/Delete separated under dashed danger border.
- Preview omitted (not functional).
