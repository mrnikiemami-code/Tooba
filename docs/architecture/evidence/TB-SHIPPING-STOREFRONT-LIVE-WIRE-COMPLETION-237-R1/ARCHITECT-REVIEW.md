# Architect review — TB237-R1 — 2026-10-28

Reviewed actual main e02c1a69529f581d346fa690748c1c022d0edf05, changed API/page/backend DTO/planner/revision
paths, complete shipping/payment handlers, and actual committed 390-shipping /360-payment /1440-payment PNGs.
Root did not execute Windows runtime/tests or independently connect to its localhost. Worker live receipts in
RESULT plus source and screenshots support a SCOPED SingleStore configured Manual/no-coupon practical checkpoint.
Verdict: ACCEPT THAT CHECKPOINT FOR USER TRIAL, NOT full owner/coupon/mobile/payment-success/release acceptance.

## Accepted progress
READ Confirmation* -> WRITE Expected* mapping now matches server DTO; captured top-level legacy shorthand empty;
all server groups and five echoes required. Save has a synchronous ref, no partial-group intent. Real persisted
root revision is exposed separately from legacy recovery variable, so recovered-null presentation does not
invent version0. Frontend uses server root token, correct expectedSelectionRevision text reaches actual
final-click API. Capability flags are server hints, not write authority. Original server CAS/owner/quote/calendar
checks preserved. No financial engine or schema authored here.
Recorded real normal buyer read/selection200/paymentpreview200/startpayment200 creates one real Store-owned group
with compact immutable evidence, one reservation commit and manual payment Pending. Goods9485771 + shipping125000
= payable9610771 IRR; accepted slot Oct31 09–17 Tehran. Pending manual payment != paid, PSP success or captured
money. channel Marketplace in the sales record is NOT Marketplace EDITION proof; owner remains Store/store-alpha.
An independent second cart remains Active with successful selections and zero checkout rows, useful for user trial.
Three pending231/232/236 Order migrations are now reported APPLIED on verified nonproduction tooba_alpha/store-alpha.
Do not preserve the obsolete sharedNOTAPPLIED top. No baseline/history rewrite. Original3000/5088/window preserved.

## Limits/corrections to carry into next LIVE task, no generic review loop
- Coupon live valid/invalid/clear was NOT run, neither actual mixed Marketplace/Seller configuration.
- Only save has the new synchronous guard; actual payment pay() still checks React paying and setPaying without
  synchronous ref. Stable server idempotency helps but is not measured same-tick UI suppression.
- Shipping validity/persist builder still reads raw projection rather than confirmed/current masked response;
  destination failed/pending may hide cards yet old raw context remains selectable. Coupon read from storage on
  render does not itself subscribe/refetch. Existing230 empty-key/new-key in-flight defects remain.
  Close narrowly while exercising live coupon/refresh, do not add a second coordinator.
- New method context fingerprint includes date values, not interval texts; don't claim every hour-change path
  measured. Server final revalidation remains authoritative.
- Mobile shipping390 PNG is useful and shows existing card; broken product image is pre-existing/outside task.
  Payment360 PNG has visibly clipped horizontal progress/heading and dev overlay. Report says390 screenshot
  innerWidth393 and360 screenshot innerWidth378: equality of scrollWidth to THAT expanded width is NOT proof
  of requested CSS viewport390/360. Require innerWidth == requested CSS width plus no document overflow. No full
  mobile visual acceptance. Fix only local payment layout, no shared Home/PDP redesign.
- Readonly capability banner now conditional, but final UI should be tied to actual coherent server capability
  and current response; no merely enabled-preview authorization. Missing root text never invented0.
- 337-test run with38 disposable-target-guard failures did not execute those relational facts; no all-green or
  "source unaffected" conclusion from absent env alone. Critical storefront baseline bg-white failure is retained,
  not certification; no broad rerun. Existing mandatory-goods fixture gaps remain to run only when relevant.
- Worker did NOT update SHIPPING-ARCHITECT-RECOVERY TOP in this commit (its old237-not-connected top still
  exists). Root corrects all3 now. Result fixed tail omitted automaticNextImplementationTask immediately after
  Evidence-SHA; repair next envelope, don't create another transport task/probe. Actual substantive report retained.
-91min vs40 and multiple corrections NONCOMPLIANT; useful path nevertheless real per scoped evidence. No time PASS.

## Current handoff and remaining required work
Review URL http://127.0.0.1:3100/fa/shipping, active DEV cart01a1807a-e7ef-7000-82d1-285f4bd5c982 paymentpreview.
This account/session must be handed off in a DEDICATED normal authenticated task browser; a URL alone is not a
buyer-session handoff in the user's other browser. Never expose password/cookies or inspect original user tabs.
Freeze237R1 worktree/runtime3100/5188 as user-review target once handed off. Continue new changes/trials in another
task-owned worktree/ports, preserving original3000/5088 too. User already permits necessary nonproduction setup.
Issue238: real coupon+mixed Marketplace/Seller owner trial on existing UI and minimal live-blocking UI fixes above;
proper viewport proof, actual duplicate/failed-refresh, original Order read after DEV publication change.
Checked-in gatesOFF; named tasklocal coherent ON. Final all-owner/coupon/practical requirement remains binding,
not waived by first SingleStore URL. No new history engine or new quote logic.
