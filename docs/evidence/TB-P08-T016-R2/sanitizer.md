# Sanitizer

- FE `sanitizeArticleRichHtml` allows hr/video/source, color/background-color styles, DAM-only img/video/file hrefs.
- Still forbids script/iframe/object/embed/base64/javascript:.
- Backend binary body rules unchanged (no embedded data:).
