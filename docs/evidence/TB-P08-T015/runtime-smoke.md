# TB-P08-T015 — Runtime smoke

## Status

**PASS (API + pages)** after Host restart on T015 bits.

## Checks

| Check | Result |
|-------|--------|
| Host health | 200 |
| Create Article + 2 comments | OK |
| Approve comment | 200 |
| Reject comment | 200 |
| List comments (statuses persisted) | 200 |
| FE `/admin/content/help` (:3000/:3002) | 200 |
| FE Article EDIT | 200 |
| Home feature wording in source | OK |
| Comments panel wired | OK |

`USER_VISUAL_ACCEPTED=NO`.
