# COMPLETION LEDGER — TB-282-R18

| Gate | Owner | Where | State |
| --- | --- | --- | --- |
| A1 same toast mechanism/theme | Story management UI | `story-toast.ts` wraps `react-toastify` on the existing container; no container/theme/CSS change | DONE |
| A2 app 3000ms duration that really closes | Story management UI | per-toast `autoClose:3000, pauseOnHover:false, pauseOnFocusLoss:false, closeOnClick:true` | DONE (served: 1 at 4149 ms → 0 at 4450 ms) |
| A3 cannot-run is error, not blue | Story management UI | zero-eligible path `toast.info` → `storyToast.error`; `formatAdminBatchToast` severity ladder | DONE (served class `--error`, info 0) |
| A4 success/uncertain severities | Story management UI | success → `success`; genuine uncertainty → `warning`; partial success + failure → `warning` | DONE |
| A5 no permanent message card | Story management UI | persistent amber `<section>` replaced by collapsed `<details data-testid="story-pending-recovery">` + count | DONE (served banner 0 when no command) |
| A6 storage and exact retry preserved | Story intent store | pending envelopes untouched; control replays the exact stored command; no auto-send on open | DONE (mount proof) |
| A7 single refusal reason | Story admin ops | `storyRefusalReasonFa` server-code map, «تأیید نشد: …», no bare «۱ مورد انجام نشد» | DONE (mount + served skip copy) |
| A8 unknown stays unknown | Story admin ops | uncertain outcomes keep `warning`; pre-send blocks are definite `failed` | DONE |
| B1 read-only diagnosis recorded | Host Story API | owner story snapshot + DB row + item media + occurrence count | DONE |
| B2 reproduce on real owning method | Story directory/domain | `StoryApprovalReadiness282R18Tests` (real `AdminApproveAsync`, real `SellerSubmitAsync`, real reject/repair path) | DONE |
| B3 repair the demonstrated cause | Story domain policy | `ProjectActionCodes` offers `approve*` only when `HasPlayableContent`; `reject` retained | DONE |
| B4 lawful repair path, no false approve | Story directory | reject (no readiness gate) → seller owned-asset update → resubmit → approve | DONE (test) |
| B5 exact occurrence/terminal-refusal handling | Story executor | loss/unverified retains; terminal 4xx clears only its envelope; 408/425/429 retryable | DONE |
| P1 affected tests | frontend/backend | R18 mount 3/3 (+R17 3/3), story unit/guard 15/15, backend R18 2/2, tsc 0 | DONE |
| P2 served FE | Host 5088 / Next 3000 | skip-bootstrap=1 recovery users=3; admin grid 200; seller grid 200; overflowX 0 at 390/1440 | DONE |
| S1 zero write | DB/Host | no live Story/Media POST; story version 3 / updated_at / occurrences 0 unchanged | DONE |
| S2 zero schema/config | repo/DB | no migration/DDL/seed/grant/appsettings/port/key change | DONE |
| S3 preserve prior work | repo | R13–R17 UI, VendorShell, i18n, Returns/payout/user data untouched | DONE |
| L1 known limits disclosed | RESULT | controlled-only served refusal; legacy record needs seller re-upload; pre-existing guard/test failures listed | DONE |

Verdict: PASS within A–B. Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.
