# CORRECTION-ADDENDUM — TB-282-R6

Prior R5 “ready for owner test” did not mean merchant UX acceptance. OWNER_REJECTED_UX is the reason this task exists.

R4/R5 mounts that opened createMode against the title-only Dialog are superseded for creation UX: `/new` is now the full workspace. Placement/shop facts remain on the same fields (`story-seller-shop-fixed`, `story-admin-shop-picker`).

HTTP `next start` CSRF: Secure cookies must not be issued on `http://127.0.0.1`. That is request-scheme detection, not a NODE_ENV lie on HTTPS.

Host serving: task D mentioned `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`. That 404s `/v1/admin/dev-context` and blocks admin identity. Primary Host stayed Development **without** skip-bootstrap so owner login works. No Host binary change (same dll SHA as R5).
