# RECEIPTS — TB-SHIPPING-ADMIN-SALES-READY-UI-246-R1

## Identity
- TaskId: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R1
- Bridge row: 761e02fc-eb13-40ef-b696-167fe0eee5f9
- Expected-Base: d31f6c2385d2a2a95ebb3cf770da7245671582a6
- Base at claim (docs FF): b577bf7d2b21291c6868d98d35202e2633e58191
- Code-SHA: 0452a0adfb0364d72ff74cedad4e6281c086967c
- Evidence-SHA: (filled after evidence pin)
- Canonical FE http://127.0.0.1:3000 → Host 5088 SingleStore/store-alpha/tooba_alpha
- Draft: 01a17d0d-e033-7000-af43-49a1daa28822 revision 2

## Focused checks
- sales-ready-ui-246-r1.test.ts
- sales-ready-ui-246.test.ts
- authoring-slot-duration-241.test.ts
- closed-day-222.test.ts
- publication-223.test.ts
- authoring-general-ui.test.ts
- Result: 44/44 pass
- typecheck: clean
- production build: exit 0

## Live DOM (authorized Chrome CDP 9339; no screenshots)
- methodName: آزمایش ارسال۲۳۰
- languages: fa-IR, en-US, ru-RU
- week: زمان‌بندی تحویل + بازه کاری کامل; 7 day testids
- destination breadcrumb: ایران
- shipping-authoring-* hit targets <44px: 0
- overflow@1440/1024/390: false; sticky present
- banned publication paragraphs: false
- no Save/publish/PUT

## Changed paths
- src/frontend/features/admin-shipping-authoring/model/slot-duration-options.ts
- src/frontend/features/admin-shipping-authoring/messages/authoring-messages.ts
- src/frontend/features/admin-shipping-authoring/ui/authoring-*.tsx (screen/week/groups/general/rates/destination/money/rate-modal/problem/message)
- src/frontend/features/admin-shipping-authoring/sales-ready-ui-246-r1.test.ts
- src/frontend/features/admin-shipping-authoring/sales-ready-ui-246.test.ts
