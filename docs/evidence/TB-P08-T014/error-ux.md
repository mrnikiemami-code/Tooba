# TB-P08-T014 — Error UX

- Mapped in `admin-error-map.ts`: not_ready, invalid_schedule, publish.forbidden, unpublish.invalid_state, preview.unavailable, article.missing, already_archived, archive_not_allowed.
- Host lifecycle maps domain codes (not silent 404 for publish readiness failures).
- UI shows localized messages; no raw JSON/machine keys in toasts.
