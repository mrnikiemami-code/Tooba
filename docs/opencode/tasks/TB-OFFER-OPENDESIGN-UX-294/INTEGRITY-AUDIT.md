# INTEGRITY AUDIT — TB-294-R3 (audit only; no design/repair performed)

## 1) Hash comparison (SHA-256)
| File | SHA-256 |
|---|---|
| 292 / offer-workspace.html (R0) | 9F399142…93ACFE192 |
| 292 / offer-workspace-r1.html | 4C7DDD4B…8B8A4500E |
| 292 / offer-workspace-r2.html (REJECTED) | 2436E076…8CD38006 |
| D:\Tooba-Offer-OpenDesign\index.html | CB781C22…0C678B766 |
| D:\…\opendesign-output\toba-offer-workspace.html | CB781C22…0C678B766 |
| OpenDesign ORIGIN (final) | 3F61B3C5…A1757F7C |

Conclusions:
- index.html == opendesign-output (CB78…) → same file (the 219,353-byte OpenDesign INTERMEDIATE snapshot).
- index.html != 292-R2 (CB78… ≠ 2436…) → current D: index.html is NOT the R2 file.
- ORIGIN (3F61…) != D: copy (CB78…) and != ALL 292 files → the true final OpenDesign artifact is distinct from every rejected 292 file.
- Packaging defect: the D: copy is a STALE 219 KB intermediate; the run finished later at 618,637 bytes (ORIGIN).

## 2) Real generated output path (origin)
`C:\OpenDesign\.od\projects\routine-1a39bd38-5ca2-4610-973b-f9a7737e71ac\`
- `toba-offer-workspace.html` — 618,637 B, SHA 3F61…, `<title>` = «توبا — فضای کاری پیشنهادها», “292” hits = 0.
- `toba-offer-workspace.html.artifact.json` — `{"kind":"html","status":"complete","entry":"toba-offer-workspace.html"}`.
- `manifest.json` + `.file-versions\<hash>\0001-…html` (OpenDesign version store).
Rendered (final origin): HTTP 200, 24 sections, 22 images, Persian/RTL. Evidence: `evidence/opendesign-render.png`.

## 3) Why the delivered images carried the 292 title
- `D:\Tooba-Offer-OpenDesign\screens\r2-*.png` (8 files, mtime 17:01) are screenshots of the REJECTED 292 **R2** prototype
  (whose header text is «بازطراحی R2 (TB-292)…»). They were produced earlier by `audit-r2.cjs` during the R0 step and were
  left inside the deliverable folder; they were never regenerated from the OpenDesign artifact.
- Also at the R0 step, `D:\index.html` was literally the R2 file (copied+tokenized). In R1 I replaced index.html with an
  OpenDesign snapshot — but the 219 KB INTERMEDIATE (not the final 618 KB) — and the old R2 screenshots remained.
- Conclusion: the “292” wording in the images comes from LEFTOVER R2 screenshots + a stale index.html; it is NOT evidence
  that the OpenDesign engine reused the 292 design.

## 4) Was the rejected design used as input / pattern?
- No HTML was fed as input. The automation used only the text prompt (below). The project directory contains ONLY
  OpenDesign-generated files (no R2/292 file; filename scan empty). Run state `userRequest` = the text prompt.
- Honest caveat: the prompt is the architect-approved REQUIREMENTS (shared spec with 292), not the rejected R2 file.
  Specification reuse is intended; design-file reuse did not occur.

## 5) Provenance of the new run
- Automation: routine-66b23571-9d07-41b8-a9b4-401c11176c70 (target = new project per run)
- agentRunId: 4732b9f0-8c38-4843-bd25-ad3de5e5713a — status **succeeded**, agent=opencode, artifactCount=1
  (state.model null while running; daemon agent model = metisdeepseek/deepseek-v4-flash as on prior successful runs)
- projectId: routine-1a39bd38-5ca2-4610-973b-f9a7737e71ac
- conversationId: routine-conv-f715b6fb-b235-455a-8afd-0451fef84003
- Evidence: `evidence/agent-run-state-final.json`, `evidence/artifact.json`, `evidence/integrity-hashes.json`,
  `evidence/opendesign-render.png`, `evidence/requirements.md`.

## 6) Full prompt submitted to the OpenDesign engine (identical file; SHA 8D26D9D7…7DAA1901)
The exact contents of the automation `--prompt-file` are appended below.

## 7) Required remediation (NOT performed here — audit-only)
1. Copy the TRUE origin over the stale intermediate:
   `Copy-Item "C:\OpenDesign\.od\projects\routine-1a39bd38-5ca2-4610-973b-f9a7737e71ac\toba-offer-workspace.html" "D:\Tooba-Offer-OpenDesign\opendesign-output\toba-offer-workspace.html" -Force` and same to `D:\Tooba-Offer-OpenDesign\index.html`.
2. Move `D:\Tooba-Offer-OpenDesign\screens\r2-*.png` to `_rejected-292-screens\`.
3. Regenerate screenshots from the final origin (never from a 292 file).
```text
Design an interactive HTML prototype for TOOBA "Offer Workspace" (Persian, RTL, Vazirmatn-like, Jalali dates, Toba design system look). Produce ONE self-contained HTML artifact with 4 views via tabs: (1) admin offers table, (2) admin offer create/edit form, (3) seller offers table, (4) seller offer form. Also include a product picker and a seller picker modal.

TABLES must match the Toba /admin/orders standard grid exactly: toolbar with search, advanced filter (drawer), column manager dropdown, saved views select, Excel and CSV export buttons, actions column, colored Persian status badges, Jalali dates, grouped amounts (ریال), server-side pagination footer. NO KPI cards above the table. Admin columns: محصول، گونه، فروشنده، کد فروشنده، کانال فروش، قیمت/ارز، تخفیف، موجودی قابل فروش، مسئولیت ارسال، وضعیت، آخرین تغییر، عملیات. Seller table: same but WITHOUT the فروشنده column.

PICKERS: a professional picker modal (search + brand/category filters + server-pagination footer), scaled for 1,000,000 products and 10,000 sellers; product rows show image/name/brand/variant specs; variant selection via color/size attribute chips; nothing auto-selected; seller+variant are locked after create.

FORMS: compact, no long scroll, no side gutters, two-column content grid, single primary «ذخیره» only (no duplicate buttons), action header must NOT overlap fields, larger readable text, strong contrast, clear primary vs secondary actions, advanced options collapsed under «تنظیمات پیشرفته», no technical/domain jargon shown to the user. Cover: pricing (multi-currency price rows: currency, amount with thousands separator, market, channel, Jalali start/end, status draft/active/retired, type base/campaign; add/edit-amount/retire; currency locked once saved; IRR no decimals), discounts section with live preview (percent/fixed+currency, dates, min qty/amount, coupon, customer/org restriction, market/channel, priority, status; active discount not economically editable), multi-warehouse inventory table (warehouse select, physical editable, reserved/available read-only, reason, total; add position flagged needs-contract), quantity & UoM (unit read-only from product, 0-6 decimals, step, min/max, remove-limit, unit shown next to fields), shipping (responsibility seller/platform/unset; service select only published/allowed; readonly scope/schedule/prep; note SingleStore vs Marketplace; count not final), returns (3 modes: default / custom days / non-returnable if permitted), lifecycle (Draft/Active/Suspended/Archived; stock/buyability independent; history; archived cannot be re-activated; per-section save error; unsaved-changes guard).

DEPENDENCY BEHAVIOR (must implement): if seller & product not selected → dependent sections disabled; if shipping responsibility unset → service select disabled; if non-returnable → hide return window; if draft → shown as not-purchasable; product quantity policy fields read-only; recorded warehouse identity read-only; multi-currency summary shows currency+market; new-create form shows NO fake history.

Deliver as an HTML file using CSS variables in :root for all colors (no raw hex outside :root), add data-od-id to each top-level section. Responsive/mobile. Persian labels only.

```
