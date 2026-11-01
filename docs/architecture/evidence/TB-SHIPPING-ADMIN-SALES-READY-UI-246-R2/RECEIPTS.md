# RECEIPTS — TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2

## Identity
- TaskId: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2
- Bridge row: 4e242c0e-aa5a-49a0-adb8-08f7d67eae41
- Expected-Base: 6e4b15cb9788e2204da92c674a7b862a678279ef
- Base at claim (docs FF): 4c88021d6ffef34a42e31f6e89ff4b858722b5b9
- Code-SHA: 6e779159d0fe25f8f30b162da6b71c4d2d8fc1b4
- Evidence-SHA: (filled after evidence pin)
- Canonical FE http://127.0.0.1:3000 → Host 5088 SingleStore/store-alpha/tooba_alpha
- Draft: 01a17d0d-e033-7000-af43-49a1daa28822 revision 2 / bytes 2852 unchanged

## Focused checks
- sales-ready-ui-246.test.ts
- sales-ready-ui-246-r1.test.ts
- sales-ready-ui-246-r2.test.ts
- authoring-slot-duration-241.test.ts
- closed-day-222.test.ts
- publication-223.test.ts
- authoring-general-ui.test.ts
- Result: 50/50 pass
- typecheck: clean
- production build: exit 0; FE3000 PID restarted on rebuilt `.next`

## Live DOM (authorized Chrome CDP 9339; no screenshots)
- me 200 userId 01a036c2… tenant store-alpha
- methodName: آزمایش ارسال۲۳۰
- languages: fa-IR, en-US, ru-RU
- applyCount=1 cancelCount=1 stickyCount=0; toolbar present; sticky header overlapNext=false
- fullDayChecked hides duration select; hourly click reveals duration select
- radio glyphs 16×16; labelH 44; giantRadios=0; shortLabels=0
- 7 day rows; Friday closed hasTime=false; boundsDup=false
- overflow@1440/1024/390: false
- banned publication paragraphs: false
- no Save/publish/PUT
- Host editor-document 200 rev=2 bytes=2852 after local exercise

## Changed paths
- src/frontend/features/admin-shipping-authoring/messages/authoring-messages.ts
- src/frontend/features/admin-shipping-authoring/ui/authoring-screen.tsx
- src/frontend/features/admin-shipping-authoring/ui/authoring-general-tab.tsx
- src/frontend/features/admin-shipping-authoring/ui/authoring-week-editor.tsx
- src/frontend/features/admin-shipping-authoring/sales-ready-ui-246-r1.test.ts
- src/frontend/features/admin-shipping-authoring/sales-ready-ui-246-r2.test.ts
- docs/architecture/evidence/TB-SHIPPING-ADMIN-SALES-READY-UI-246-R2/{RESULT,RECEIPTS}.md
- AGENTS.md + recovery TOPs
