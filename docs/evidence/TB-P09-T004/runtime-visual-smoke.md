# Runtime visual smoke — TB-P09-T004

Target: Host `:5088`, FE `:3000`

Status:
- FE `http://127.0.0.1:3000/fa/admin/orders` → HTTP 200
- Host `http://127.0.0.1:5088/health` → HTTP 200 (restarted after build file-lock)

Checklist:
- [x] Orders list FE route opens (HTTP 200)
- [x] Host health OK after rebuild restart
- [x] Grid kebab trigger implemented (iconOnly + tooltip عملیات)
- [x] View action preserved
- [x] Whole-order ops scope filter covered by tests
- [x] Order Detail preserves finance/notes/history (source + tests)
- [x] اقلام و ارسال panel mounted above financial section
- [x] Line selection scoped per seller (tests)
- [x] Create Shipment uses Dialog, never prompt (tests)
- [ ] Full authenticated interactive click-through left for Architect visual review

USER_VISUAL_ACCEPTED=NO
