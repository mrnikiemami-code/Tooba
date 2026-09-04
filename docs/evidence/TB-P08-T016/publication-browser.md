# TB-P08-T016 — Readiness / Preview / Publish

## Code / contract (worker)

- Backend-authoritative readiness (`EvaluateReadinessAsync`); Publish rejects with `content.publish.not_ready:*`
- Preview: admin-only route, noindex; Save-required UX toast
- Dialog kinds: publish / unpublish / republish via `openPublishDialog`
- BE ContentArticlePublicationWorkflowTests + FE publication tests pass
- Locale lock test updated to supply body before Publish (readiness)

## Browser / API smoke (parent)

- [ ] Incomplete Draft shows actionable blockers  
  Not exercised this session (API smoke used ready articles).
- [x] Complete → Save → readiness publishable  
  API smoke: readiness **100** on FA article `01a06ac1-1338-7000-bacf-fb0861585ff2`. Browser EDIT showed readiness badge.
- [ ] Preview reflects saved content; not publicly discoverable  
  Preview control present in header hierarchy; full preview content / noindex path not separately logged.
- [x] Publish → public visible; Unpublish → unavailable; Republish → visible  
  API smoke: publish / unpublish / republish **PASS**. Public `http://127.0.0.1:3000/fa/blogs` + detail slug **200**.

## API smoke IDs

- FA: `01a06ac1-1338-7000-bacf-fb0861585ff2` slug `t016-gate-fa-20260904082033`
- EN: `01a06ac1-135f-7000-b45f-7089ee3d1add`
