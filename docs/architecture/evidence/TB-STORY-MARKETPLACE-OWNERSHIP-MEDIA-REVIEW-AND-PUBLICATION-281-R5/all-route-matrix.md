# TB-281-R5 corrected occurrence/version matrix (issued repair paths)

R4 matrix retained historically. This table only re-states the R5-repaired owning paths with source-backed status.

| Surface | Occurrence | Replay-before-gates | Hash fields | Status |
| --- | --- | --- | --- | --- |
| SellerUpdateAsync | required | yes | title/locale/market/cover-id/cover-url/cta + version/story | CLOSED R5 |
| AdminUpdateAsync | required (prior) | yes | same length-prefixed update hash | CLOSED R5 hash |
| AdminUpdateItemAsync | required | yes | item media/caption/cta + ids/version | CLOSED R5 |
| SellerUpdateItemAsync | required | yes | same item hash | CLOSED R5 |
| AdminSetScheduleAsync | required | **before** version/content | start/end/version/story | CLOSED R5 |
| Create/Add SaveChanges | arbitration | exact pk_command_occurrences fold | n/a | CLOSED R5 |
| Media UploadForSeller | ambient+enlist+EF | refuse ValidationFailed | n/a | CLOSED R5 |

UI delivery still PLANNED_NOT_ISSUED. Seller surface remains existing VendorShell `/vendor-panel/stories`.
