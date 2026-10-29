PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORIGINAL-STORE-COMPLETE-HANDOFF-243
Parent-Task: TB-SHIPPING-PRIMARY-DATA-AND-DELIVERY-UI-FINISH-242
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — complete original-store handoff
Mode: CLOSE_REAL_SERVICE_CONFIGURATION_UI_AND_BUYER_FLOW_ON_ORIGINAL_PRIMARY
Bridge-Task-Row: b81b8416-9bb8-4431-9019-5469670443a7
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: c6c5ebd48a220b22d8e2874e9857e9ed076c052d
Base-SHA: c6c5ebd48a220b22d8e2874e9857e9ed076c052d
Code-SHA: 7ea79556fa91fa8fe00b5fa45b349023d8ae7c88
Evidence-SHA: 4f21dc069e09d4b94676ca959151b047c25ddcb8
automaticNextImplementationTask: NONE

PRIMARY http://127.0.0.1:3000/fa/shipping → Host 127.0.0.1:5088
FE PID46476 (D:\temp\tb243-fe next start, source 7ea79556f); Host PID8856 (D:\temp\tb243-primary, DeploymentId=store-alpha-tb243-primary).
Edition=SingleStore Tenant=store-alpha DB=tooba_alpha. Marketplace primary NOT used. CDP9350/9351 frozen (not enumerated).

A — DELIVERY ROWS
1) Original edition/DB verified SingleStore/store-alpha/tooba_alpha on 3000/5088.
2) Original draft 01a16eeb-70b3-7000-87c2-564c82835496 post/NULL type 01a16f43-6b4a-7000-8670-4c16e14c6345: weekly SlotDurationMinutes 60 via admin PUT (rev2→3), published Selected snapshot 01a18584-6c2b… format2. Offer 964ac1b4… (laptop 1.4kg) assigned Platform+type 01a16f43 via PUT /v1/admin/offers/{id}/fulfillment. Catalog post:standard deactivated so ResolveCatalogBindings yields post/NULL matching this draft (DEV230 post/standard pointer orphaned, not presented as user method).
3) Weight: Catalog ICatalogCheckoutLookup.GetShippingWeightGramsByVariantIdsAsync reads Number attrs unit g/kg; Order goods facts fill GroupWeightGrams (1400g) so 1000–2000g rate prices. Locale: shippingDeliveryMessagesFor(fa|en) on DateChipStrip/CapturedSlotPicker/group cards; whitespace-nowrap ranges; tsc clean; continue-state 14/14; format 4/4.
4) Buyer Order: cart→projection (8 hourly Mon slots)→select 2026-11-02 14:00–15:00 ticks 504e9–540e9→selection200 rev1 (concurrent twin stale CAS)→payment-draft200 payable 2,749,545→start-payment200 checkout 01a1858c-1dfe… payment 01a1858c-1fb7… manual Pending, reservationCount1, window 14:00-15:00, history draft 01a16eeb snapshot 01a18584 rate 01a16f4e. Concurrent second start-payment 500 platform.unexpected disclosed (first won).
5) Negative: invented ticks 999/1000 → 400 storefront.captured_selection.slot_unavailable, no order.
6) History: admin 60→120 publish snapshot 01a1858d-aed0… selVer5; original Order GET unchanged window/snapshot/payable; restore 60 publish 01a1858d-af5e…. Intermediate fresh-120 projection measured 0 slots (disclosed); restored-60 review projection 8 slots.
7) Review handoff: buyer tb243-review-…@example.invalid userId 01a1858d-06c8… cart 01a1858d-06ee… v4 UNSUBMITTED; BFF /api/auth/me 200 SingleStore/store-alpha; /fa/shipping 200. Order proof cart converted separately.

DISCLOSED: concurrent StartPayment twin 500; one intermediate 120-min projection empty-slot read; method label «پیشتاز» from post service translation; selection concurrent twin stale (CAS). No PSP/VisualACCEPT. TB185 paused/defaultOFF. Checked-in gates OFF; local primary StorefrontCapturedPreview ON. No screenshots.

END_TOOBA_WORKER_RESULT
