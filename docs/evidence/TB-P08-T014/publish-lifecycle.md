# TB-P08-T014 — Publish lifecycle

- Draft → Publish (or Scheduled if PublishDate future).
- Published → Unpublish → Draft (history preserved).
- Draft after prior publish → Republish (distinct history event).
- Archive remains separated under destructive actions; not collapsed with Unpublish.
- Publish dialog shows blockers and does not mutate when mandatory gaps known; backend still authoritative.
