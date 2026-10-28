PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-PRIMARY-DATA-AND-DELIVERY-UI-FINISH-242
Parent-Task: TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — original data and polished configured delivery choice
Mode: RESTORE_ORIGINAL_SINGLESTORE_PRIMARY_AND_POLISH_EXISTING_DELIVERY_PICKER
Bridge-Task-Row: d65b5fa1-ed98-4ede-81ee-c6353689f7b4
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 39ba84c77037d524813e55b4b641c51b49904ac0
Base-SHA: 39ba84c77037d524813e55b4b641c51b49904ac0
Code-SHA: CODE_SHA_PLACEHOLDER
Evidence-SHA: EVIDENCE_SHA_PLACEHOLDER
automaticNextImplementationTask: NONE

PRIMARY http://127.0.0.1:3000/fa/shipping → Host 127.0.0.1:5088 PID45304 (D:\temp\tb242-primary, DeploymentId=store-alpha-tb242-primary).
/health/ready edition=SingleStore postgresql=configured. Tenant store-alpha / DB tooba_alpha (tenant-alpha). FE3000 PID41116 next start from D:\temp\tb242-fe. Marketplace Host/FE previous PIDs stopped; Marketplace DB/profiles retained. User Chrome CDP9350 NEVER automated/reloaded/closed (Marketplace session no longer maps after edition switch — disclosed). New visible Chrome profile D:\temp\tb242-chrome CDP9351 opened on /fa/shipping.

SKILL: .cursor/skills/tooba-frontend-feature/SKILL.md. Reused DateChipStrip + GroupSchedulePicker vocabulary for CapturedSlotPicker (rounded-2xl min-h-11 grid, Clock/Check, sr-only radio, «روز تحویل»/«ساعت تحویل»). Files: storefront-shipping-date-chips.tsx, storefront-shipping-group-cards.tsx, storefront-shipping-api.ts (selectedSlotStart/EndTicks now forwarded on PUT), continue-state tests, login-return-to prefer-const for production build.

MIGRATIONS (tooba_alpha only, private dump D:\temp\tooba_alpha_tb242_pre.bak): Shipping → 20261028201416_ReceivingSlotDuration; Order → 20261028200100_AcceptSlotDurationFormat (prior current CouponContext/SelectedCapturedRevision). No history rewrite.

AUTHORITY LEDGER (sanitized):
- User draft 01a16eeb… post/null option rev2 type 01a16f43… Asia/Tehran slot_duration NULL; weekday1 09–17 only; pointer snapshot 01a17389… format1. No Active offer carries that fulfillment_shipping_type_id → cannot silently price as user service (preserved).
- DEV230 draft 01a17d0d… post/standard rev2 type 01a1785e… Asia/Tehran slot_duration NULL; days 1–4+6–7 09–17, Fri closed; pointer snapshot 01a17d0e… format1. Sole Active assigned offer 0442a1fa… Platform → that type.
- LIVE projection 200: method «آزمایش ارسال۲۳۰», 7 dates, intervalText 09:00–17:00, ONE slot 09:00–17:00 ticks 324e9–612e9 (NULL duration = full window, not inferred 60).
- Selection PUT 200 revision TEXT "1", selectedDeliveryTimeWindow 09:00-17:00. Payment-draft 200 shipping 125000 payable 9610771 same window. Negative invented ticks → slot_unavailable 400.

CHECKS: tsc clean; continue-state+captured-selection 27/27; critical-storefront 19/20 PRE-EXISTING landing bg-white. Host+FE production rebuilt. Gates checked-in OFF; task-local captured Enabled. TB185 paused. No screenshots/PSP/VisualACCEPT/final submit of user review cart. Original deadline MISSED; focus over 45 box disclosed as overrun on cutover+migration+proof.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
