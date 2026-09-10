# TB-P09-T022-R1 — visual smoke (real browser)

`USER_VISUAL_ACCEPTED=NO` — Worker does not claim human visual acceptance.

Tool: cursor browser against live FE `http://127.0.0.1:3000` with Next **Ready**.

## Fixtures

### Single-seller — `01a08973-d831-7000-ae48-d6f8a6bc3fcf`

- Admin Detail / اقلام و ارسال
- **No** `بسته‌بندی مرکزی` heading

### Multi-seller delivered — `01a08973-dd8c-7000-b205-cc7f15358dfb`

- `بسته‌بندی مرکزی` visible
- MP packages present (historical + delivered lineage from T022 matrix)

### Fresh multi-seller — `01a0898a-0d7d-7000-b5ed-7f5d85a29241`

| Step | Observed |
| --- | --- |
| Before create | Create dialog (`ایجاد بسته تجمیعی`); eligible member shipments selectable |
| After create | Package **MP-01A0898C6B73**; member lock explanatory text visible |
| Actions | `ابطال` / `ارسال بسته تجمیعی` buttons render |
| History | Package events present in operational history |

## UI quality (smoke)

- RTL intact; no redesign of Orders layout
- No endless spinner / no new uncaught console crash on tested paths
- Prefer human tracking labels over raw GUID slices (FE polish in working tree — see `r1-fe500-fix.md`)

No TB-P09-T023.
