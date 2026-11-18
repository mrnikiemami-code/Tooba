# TB-280 — UX before → after (user-visible defects only)

Task: `TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280`
Acceptance standard: `docs/product/OPERATOR-UX-ACCEPTANCE.md` (TOOBA-UX-001)
Case under review (read-only): `01a1eefb-ea96-7000-838d-9a0b37661c8b` — requested 1, received 0, coherent.

| # | User-visible defect | Before | After | Source | Proof |
| --- | --- | --- | --- | --- | --- |
| 1 | A review was offered on a case where nothing had arrived, opening a modal whose every ceiling was `0` | «ثبت برنامهٔ تفکیک اقلام» appeared whenever `publish_resolution` was published | published only for actually received goods, labelled «بررسی مرجوعی» | `ReturnWorkPolicy.cs` (`receivingSummary.Received > 0m`) + workspace action plan | `ReturnsPlainLanguageWorkflow280Tests` 4/4; live API `canPublishResolution=false`, `actionCodes=["record_receipt"]` |
| 2 | Opening that modal immediately showed red errors on untouched fields | both accepted-portion checks ran on empty strings ⇒ `problem:"empty"` on open | an empty portion means zero accepted; red feedback only after typing | `admin-return-workspace.tsx` resolution render + submit | TB-280 mount: dialogs open with `role=alert` count 0; live 390px DOM alerts 0 |
| 3 | The required per-line reason was labelled «اختیاری» while the owning writer requires it | optional label, `null` reason sent, server refused after typing | «دلیل این قلم (الزامی)» + min-3-char hint + local refusal before any bytes are stored | workspace resolution dialog | TB-280 mount: reasonless review posts 0 with «دلیل بررسی را بنویسید»; with a reason the exact five fields are sent |
| 4 | Technical vocabulary in the working area: «برنامهٔ تفکیک»، «نسخهٔ تصمیم»، «نسخهٔ بررسی‌شده»، «فقط افزودنی»، «تلاش مشتق‌شده» | section titles, dialog titles and paragraphs | plain retail words: «بررسی مرجوعی»، «بازگشت وجه»، «تحویل کالای ردشده به مشتری»، «قدم بعدی»، «اقدام‌های دیگر»، «سابقه» | workspace + `admin-error-map.ts` `return.resolution.*` copy | TB-280 mount: working area has no «نسخهٔ»/«تفکیک»/`occurrence`/`idempot`; served chunk `7944-e29aaa760b08cd39.js` has none |
| 5 | Ten equally-weighted technical buttons; the safe next step was not obvious | one flat button wall | ONE full-width primary next step with its own sentence; the rest grouped under «اقدام‌های دیگر» as quiet secondary buttons, still only server-published codes | workspace `actionPlan` + `ActionButton` secondary tone | TB-280 mount: primary is `record-receipt`, approve reachable in the secondary group; live DOM primary 68px tall at 390px |
| 6 | Empty technical decision/resolution blocks dominated the page | empty sections rendered inline above the work | audit/receipt/refund/decision detail moved behind ONE collapsed «سابقه» disclosure; outstanding rejected goods stay visible | `<details data-testid="admin-return-history">` | TB-280 mount: disclosure exists and is closed; only that node is removed before asserting the plain working copy |
| 7 | Status card did not answer "what is this case waiting for?" | status/badges only | status card leads with a plain sentence and business money words | `describeNextStep`, `describeRefundMoney` | live DOM detail = «هنوز کالایی دریافت نشده است؛ ابتدا دریافت کالا را ثبت کنید.»; mount T1 |
| 8 | The next step sat below the whole history block | after all audit sections | next-step card sits immediately after the case status, above the items table | workspace section order | live DOM at 1440/390: primary visible, document overflow 0 |
| 9 | A vanished technical action could hide an uncertain earlier send | unchanged constraint | preserved: the unresolved-intent card, frozen envelope and exact replay are untouched | workspace send/retain path (unchanged) | 276/277/279 suites 23/23 incl. unknown → refusal → exact replay retention |

## Explicitly NOT changed (accepted 279-R3 boundaries)

- write rules: receipt write gate, `PublishResolutionAsync`, approval/refund/stock/custody commands, unit quantum and shared ceilings;
- the wire: routes, body fields, frozen decision/resolution versions, occurrence keys, idempotent replay;
- schema/data: zero migration, zero live business write, the stored case untouched (`updated_at` unchanged).
