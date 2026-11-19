# RECEIPTS — TB-282-R13

## Claim
- `/api/tasks/next` → Claimed `82b8164b-b25f-4043-b8ed-797458180d6e`
- Expected-Base `d6956b5f4363711aeba12ce7281f00fa42f46b62` is ancestor of HEAD at claim
- Issuance docs HEAD `42eab590d1a0d1a46cf93ea483295f9ae9896f56`

## Tests
```
npx tsx --test app/stories/management/story-management-282r13.mount.test.ts
→ 8/8 pass
npx tsx --test …282r12… …282r11… (with NODE_ENV=development)
→ R12 3/3; R11 A delete cancel/replay pass (admin-op dialog); unrelated R11 public-player path ENOENT pre-existing
npx tsc --noEmit -p tsconfig.json → 0 errors
```

## Interaction facts (controlled transport)
1. Detail approve → Dialog cancel → 0 POST; confirm → `POST …/approve` body `{expectedVersion, occurrenceKey}`
2. Reject empty reason blocked; cancel 0 POST; confirm posts shared `reason`
3. `classifyAdminOp` mixed Submitted+Approved → 1 eligible / 1 skipped with reason
4. Partial unknown B retains pending; recovery retry exact same body bytes; A success not re-sent
5. `executeAdminOpBatch` 2 eligible + 1 skip → 2 sequential POSTs distinct keys/versions
6. Seller detail: 0 approve/reject controls

## Geometry (CDP, create `/fa/admin/stories/new`, next-dev)
| Viewport | Fact |
| --- | --- |
| 1440×900 | dateTopDiff=0; clockTopDiff=0; startRightOfEnd=true; noEndInEndHeader; noEndInLegend=false; overflowX=false |
| 390×844 | startThenEnd=true; overflowX=false; noEndInEndHeader; endTime disabled under no-end |

## Served list (read-only)
- `/fa/admin/stories` grid present; AG selection checkboxes; select 1 row → toolbar «تأیید» «رد» «حذف استوری»
- Host `/health` 200; skip-bootstrap preserved; ZERO live Story/Media write

## Source touch
- `story-admin-ops.ts` (new)
- `StoryManagementScreen.tsx` (layout B + admin op Dialog + bulk/row)
- `story-intent.ts` / `story-response.ts` (`approve` action)
- `story-management-copy.ts` (fa/en bulk copy)
- mount tests R11/R12/R13
