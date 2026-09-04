# TB-P08-T014 — History model

- Table `content.article_history` append-only.
- Events: draft_created, updated, published, scheduled, unpublished, republished, archived.
- Fields: event type, actor, timestamp UTC, previous/new state, summary fa/en.
- No full HTML snapshots per keystroke.
