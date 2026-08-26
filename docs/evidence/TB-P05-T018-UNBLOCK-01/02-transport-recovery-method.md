# 02 — Transport Recovery Method

1. Confirmed T018 commit present locally and never successfully pushed.
2. Restored pre-bloat PNG blobs from reflog commit `ef4d9fe` (unpushed amend ancestor).
3. Lossless-format PNG retained; resized/cropped for review usefulness (max width 800, max height 1400) via WPF `PngBitmapEncoder`.
4. Evidence total after optimization: **~5.8 MiB** (was ~15–21 MiB).
5. Amended **local-only** unpushed commit (no force push; origin still at predecessor).
6. `git push origin main` with `http.version=HTTP/1.1` and enlarged `http.postBuffer`.
7. TLS not weakened; credentials not altered; no force push.
