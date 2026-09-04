# Video DAM (TB-P08-T016-R5)

- `insertDamVideoHtml` inserts bare `<video class="article-dam-video" …>` (no figure wrapper).
- GHS allowlist keeps dedicated `video` + `figure` entries.
- Sanitizer unwraps accidental `figure>p>video` / `p>video` after reload so controls/src/`data-media-asset-id` remain.
- Unit coverage: wrapped figure>p>video sanitizes to working video without paragraph wrap.
