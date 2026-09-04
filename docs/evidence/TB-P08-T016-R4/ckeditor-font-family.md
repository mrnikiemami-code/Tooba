# CKEditor font family

- Toolbar: `fontFamily` + `fontSize` enabled in `content-article-ckeditor.tsx`.
- Families include Times New Roman and B Nazanin (with stacks).
- Sanitizer `article-rich-html.ts` allowlists those families so applied fonts survive save/reload.
