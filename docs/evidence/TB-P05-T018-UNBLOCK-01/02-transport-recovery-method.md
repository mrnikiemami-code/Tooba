# 02 — Transport Recovery Method

1. Confirmed T018 commit present locally and never successfully pushed.
2. Restored pre-bloat PNG blobs from reflog commit `ef4d9fe` (unpushed amend ancestor).
3. Lossless-format PNG retained; resized/cropped for review usefulness (max width 800, max height 1400) via WPF `PngBitmapEncoder`.
4. Evidence total after optimization: **~5.8 MiB** (was ~15–21 MiB).
5. History rewrite **only while unpushed** (origin still at predecessor `11b7ee9`): soft-reset and rebuild commits — no force push.
6. Push with `git -c http.version=HTTP/1.1 push origin main` and enlarged `http.postBuffer`.
7. Single-commit push of all PNGs still risked 408; **split** into three fast-forwards: code/markdown first (`f77cc4a`), then PNG batch 1 (`6a7dc13`), then PNG batch 2 (`8482124`).
8. TLS not weakened; credentials not altered; no force push.
