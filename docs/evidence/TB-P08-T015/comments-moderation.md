# Comments moderation rules (TB-P08-T015)

## Actions (backend-authoritative)

- `approve` → Approved
- `reject` → Rejected
- `hide` → Hidden
- `pending` → Pending (optional reopen)

## Transitions

- Pending → Approved | Rejected | Hidden
- Approved → Hidden | Rejected | Pending
- Rejected → Pending | Approved | Hidden
- Hidden → Pending | Approved | Rejected
- Same-status → `content.comment.invalid_transition`

## Auth

- List: `content.view`
- Create / moderate: `content.edit`
- Fail-closed via `ContentAdminAccess`

## Error codes

`content.comment.not_found`, `article_not_found`, `invalid_transition`, `invalid_payload`, `forbidden`
