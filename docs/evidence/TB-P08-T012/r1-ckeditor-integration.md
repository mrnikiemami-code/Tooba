# TB-P08-T012-R1 — CKEditor integration

- Packages: `ckeditor5@^48.5.0`, `@ckeditor/ckeditor5-react@^11.2.0` (self-hosted npm; no CKBox/cloud upload).
- Canonical: `ContentArticleEditor` → dynamic `ssr:false` → `content-article-ckeditor.tsx` (`ClassicEditor`).
- Wired in `content-article-admin-screen.tsx` Content tab EDIT mode only; VIEW stays sanitized HTML.
- TipTap `ContentArticleRichTextEditor` removed; Product TipTap unchanged.
- `licenseKey: "GPL"` for self-hosted open-source plugins (no licensing gate/TODO/warning UI).
- Next: `transpilePackages` includes `ckeditor5` + `@ckeditor/ckeditor5-react`.
