# TB-P08-T016 — Readiness / Preview / Publish

## Code / contract (worker)

- Backend-authoritative readiness (`EvaluateReadinessAsync`); Publish rejects with `content.publish.not_ready:*`
- Preview: admin-only route, noindex; Save-required UX toast
- Dialog kinds: publish / unpublish / republish via `openPublishDialog`
- BE ContentArticlePublicationWorkflowTests + FE publication tests pass
- Locale lock test updated to supply body before Publish (readiness)

## Browser (parent)

- [ ] Incomplete Draft shows actionable blockers
- [ ] Complete → Save → readiness publishable
- [ ] Preview reflects saved content; not publicly discoverable
- [ ] Publish → public visible; Unpublish → unavailable; Republish → visible
