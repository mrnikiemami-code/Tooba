PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R17
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R16
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Use native application toast and compact bulk confirmations
Mode: OWNER_REJECTED_MODERATION_FEEDBACK_UX_REPAIR
Expected-Base-SHA: 6271b64a59ae63d4344a4837f56d5933dc439504
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Owner decision supersedes prior result-dialog requirement
Owner rejects “هیچ مورد مجازی در انتخاب فعلی نیست”, disabled impossible action dialogs, per-story name lists and automatic result Dialog. They explicitly want the application's existing toast. A thousand selected deletions must not produce a thousand-entry confirmation/result UI or toast storm.
R14/R15 instruction requiring a visible per-row result Dialog is SUPERSEDED by this owner direction. Preserve internal per-row outcomes and safe retry; change their presentation. R16 typing fix retained.
Current openAdminOp always opens classified plan even eligible=0; confirmAdminOp displays count toast AND result Dialog. Fix this Story integration only. Read AGENTS/applicable FE rules. One worker; no delegation/screenshots/polling.

## Required behavior
1. Classify before opening. If eligible=0, open NO operation/reason/result Dialog, send ZERO commands and keep selection. Use ONE existing app toast explaining why in plain Persian, e.g. all platform/main-storefront approve: “استوری‌های ویترین اصلی به تأیید نیاز ندارند.”; reject: “فقط استوری‌های در انتظار تأیید قابل رد هستند.” Adapt accurately to actual selected reasons. No “مورد مجاز”, “ردشده از این اقدام” or technical lifecycle jargon. Do not let platform origin alone override server actionCodes.
2. For eligible or mixed selection use ONE compact DS confirmation: operation + selected/processable/skipped counts; group skipped reasons with counts, never enumerate every title. Reject alone asks for reason; delete retains explicit destructive confirmation. No duplicate generic confirmation. Buttons واضح: تأیید / رد استوری‌ها / حذف استوری‌ها / انصراف as appropriate.
3. Completion uses ONE native existing application toast with accurate totals; NO automatic result Dialog, duplicate “بستن” controls, per-row toast or full name list. Example “۸ استوری حذف شد؛ ۲ مورد حذف نشد.” Uncertain transport must explicitly say result is unknown and recoverable, never call it definite failure or success. Skipped must not be labelled “ردشده” as though moderation rejection occurred. Severity reflects real outcome. Preserve internal result.items, scoped pending recovery, exact route/version/key/body retry and no replay of successes. After genuine deleted-detail completion, return to list without requiring a result-modal close; unresolved deletion cannot navigate as success.
4. Large selected sets: bounded summary DOM/toast size at 1000 controlled inputs. Keep existing sequential durable executor; no all-pages selection, new bulk backend, parallel requests, framework or hidden selection expansion. The count reflects the actual selected set, including existing page-selection limits.

## Verification / handoff
REAL mounted AppDataGrid: all-ineligible selected approve/reject -> one explanatory native toast, no dialog/no POST, selection unchanged. Mixed selected -> compact counts + reason input, per-character typing/cancel stable (R16), confirm sends eligible only, one toast/no result dialog. Cover delete success plus unknown/exact recovery and detail navigation. Adapt affected R14/R15/R16 presentation assertions to owner's new UI while keeping transport/epoch/key/selection assertions intact; no weakened safety tests.
A controlled 1000-row plan through actual confirmation/completion rendering proves no full title enumeration and bounded nodes/toasts; no 1000 live requests. Don't claim this is all-page live selection or live deletion.
Read-only/controlled HTTP on served FE3000: verify zero-eligible toast and compact confirmation/cancel, no live command. Record exact observed copy/counts; no screenshots. Affected tests + tsc only; update served FE. Html404 production build blocker unchanged.
ZERO backend/schema/auth/grant/seed/config/live Story-Media mutations. Host5088 skip-bootstrap=1; VendorShell/i18n authority/date layout/R16 callbacks/Returns/payout/user work/R6 effects UNKNOWN preserved.
Persist exact TASK.md twice + receipt, base ancestry (docs-only descendants allowed), normal commit/push/fetch parity; no force/rebase/amend/reset/stash, keys/binaries. Update five CURRENT pointers. Short result/code/final SHA/evidence/limits; Worker PASS != Architect ACCEPT != owner acceptance. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
