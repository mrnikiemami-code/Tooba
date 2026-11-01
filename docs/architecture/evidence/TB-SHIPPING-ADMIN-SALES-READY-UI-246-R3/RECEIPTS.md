# RECEIPTS — TB-SHIPPING-ADMIN-SALES-READY-UI-246-R3

## Identity
- TaskId: TB-SHIPPING-ADMIN-SALES-READY-UI-246-R3
- Bridge row: a9461584-ebd9-41f8-9610-adbd738822bf
- Expected-Base: cadb45a42201b2e45b7414a7c6af59bc6607a4bc
- Base at claim (docs FF): 7048dde0365054b6319e1ce72b443acd121ff772
- Code-SHA: f15b37beedcb50bb3ff56b2c2813928c49ae9e45
- Evidence-SHA: 73b2eca9872cb8daaa3f92cbab63a7810153c010
- Canonical FE http://127.0.0.1:3000 → Host 5088 SingleStore/store-alpha/tooba_alpha
- Draft: 01a17d0d-e033-7000-af43-49a1daa28822 revision 2 / bytes 2852 unchanged

## Focused checks
- sales-ready-ui-246-r3.test.ts (11)
- destination-rate-modals-225.test.ts + sales-ready-ui-246-r2.test.ts adjacent
- Result: 39/39 in combined run including R3
- typecheck: clean
- production build: exit 0; FE3000 restarted

## Live DOM (CDP 9339; no screenshots)
- me 200; method آزمایش ارسال۲۳۰; breadcrumb ایران
- addGroup 1→2 local; choose ایران picked=true
- Add → areasUnchanged true (no second ایران chip); no Save/PUT
- Host editor-document 200 rev=2 bytes=2852

## Changed paths
- src/frontend/features/admin-shipping-authoring/model/destination-overlap.ts
- src/frontend/features/admin-shipping-authoring/messages/authoring-messages.ts
- src/frontend/features/admin-shipping-authoring/ui/server-field-error.ts
- src/frontend/features/admin-shipping-authoring/ui/authoring-destination-modal.tsx
- src/frontend/features/admin-shipping-authoring/ui/authoring-groups-editor.tsx
- src/frontend/features/admin-shipping-authoring/ui/authoring-screen.tsx
- src/frontend/features/admin-shipping-authoring/sales-ready-ui-246-r3.test.ts
- docs/architecture/evidence/TB-SHIPPING-ADMIN-SALES-READY-UI-246-R3/{RESULT,RECEIPTS}.md
- AGENTS.md + recovery TOPs
