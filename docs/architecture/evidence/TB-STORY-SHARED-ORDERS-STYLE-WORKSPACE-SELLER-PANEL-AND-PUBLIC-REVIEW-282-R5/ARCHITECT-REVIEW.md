# ARCHITECT REVIEW — TB-282-R5 — 2026-11-19

Decision: ACCEPT_FOR_OWNER_TEST_WITH_DECLARED_LIMITS.
R5 targeted source repairs are accepted for owner trial; no new implementation task issued. This is NOT full A-D proof certification, user acceptance, or marketplace/commercial readiness.

Reviewed code: 352a252ad9cf21645cf54c2d71a51e91c4da9d98.
Fresh implementation/evidence main: 71e489bcd7a0a1b871dad6bcec86bca1050c6e96.
Posted outer final 72279b95 was followed by docs-only 71e489bc; confirmed no additional production delta.
Method: independent GitHub source/diff/evidence review. Architect did NOT execute builds/tests or inspect the user's Windows served page.

## Source closure accepted for owner trial
1. F1: destinationLabelForItem no longer borrows selectedProductLabel from a different editor. It uses that item's own CtaProductTitle or target-keyed map and explicit unknown. AdminStoryItemSnapshot has an additive optional CtaProductTitle; EnrichAsync/EnrichMany get existing selected ids through Catalog.Contracts, including ids outside the picker page. FE mapper carries the owning label and detail load resets the target map from returned items. Scope authorization and lifecycle remain existing owner responsibilities.
2. F2 edit: mounted admin and seller tests actually change product, caption and duration then capture one owning update-item PUT, expectedVersion, occurrence and retained media id/url. This closes the previous edit-open-only evidence gap. The current edit branch does not upload/delete/re-add. Test transport recording counts PUTs rather than every prohibited verb, so that negative-effect claim additionally rests on inspected branch source, not a comprehensive network spy.
3. S1: direct executor tests retain update-item intent for empty/non-JSON/wrong-Story 200 then accept a bound response. RECEIPTS reports a precise temporary pre-fix production change causing the SAME retention fact to fail (ok !== failed), followed by restored green. This is worker-reported negative-control evidence, not independently rerun.
4. S2: CatalogPublishedProductSearch282R5Tests invokes the ACTUAL CatalogPublishedProductSearchLookup with 55 Published + 5 draft EF InMemory rows, later-page match, disjoint pages/count/nonpublic exclusion. Accepted at that provider scope, not PostgreSQL translation/concurrency proof. Retained R4 seller adapter isolation is controlled Contracts evidence.

## Declared owner-test / evidence limits, not hidden as closed
- F1 test mounts pre-enriched HTTP DTOs and checks both stored labels with one target outside picker page; it opens/cancels a dialog but does not actually select a changed product before cancel or perform a second full reload. Source fix is accepted; exhaustive hydration/scope/cancel lifecycle certification is not claimed.
- Unknown-response retention fact is a direct executor test, NOT a mounted visible pending-card/no-toast/no-navigation scenario. It additionally calls clearStoryIntentIfCurrent after success, so its final absence assertion alone does not independently prove executor auto-clear. The inspected executor retains its existing matching clear path.
- No actual runtime active-grid old-saved-view/scroll pin proof is supplied. Existing legacy bridge pins actions at logical end and list source/default normalization are retained; helper tests do not demonstrate geometry.
- Served overflow 0 at 1440/390 is measured on an UNAUTHENTICATED shell. It is NOT evidence of active editor/dialog/grid geometry. Owner must test the real active screens.
- Worker fixtures and controlled ports do not demonstrate live create/upload/submit/approve/public playback. These are the next OWNER test, not another automatic test-task phase.
- Keep unrelated critical-storefront bg-white failure and Persian-primary lifecycle toolbar labels disclosed; neither is newly certified by Story proof.

## Handoff and durable state
activeTask: NONE
automaticNextImplementationTask: NONE
State: AWAITING_OWNER_TEST
Stop-State: STOP_FOR_USER_REVIEW

Do not initiate R6 or another development wave automatically. User asked to reach their own test quickly. If owner reports an actual failure, capture a concrete reproduction and issue one narrow consolidated repair only as needed. Passing unit tests are not user acceptance.

Owner checklist (plain Persian):
1. ادمین: لیست استوری‌ها را باز کن؛ عنوان اول و عملیات آخر هنگام اسکرول ثابت باشد. برای ویترین اصلی یا یک فروشگاه استوری بساز.
2. فروشنده: فقط فروشگاه و محصولات خودش را ببیند؛ دو اسلاید با دو محصول متفاوت بسازد. محصول یکی را ویرایش کند، صفحه را دوباره باز کند و نام هر دو را بررسی کند.
3. فروشنده برای تأیید بفرستد؛ ادمین بررسی و منتشر کند؛ نمایش و لینک محصول/فروشگاه در ویترین کنترل شود.
4. در موبایل هم فرم ویرایش و اسکرول لیست بررسی شود. هر اقدام ذخیره/ارسال در تست مالک، دادهٔ واقعی می‌سازد؛ worker هیچ پرونده یا استوری نمایشی نساخته است.

URLs: http://127.0.0.1:3000/fa/admin/stories ; http://127.0.0.1:3000/fa/vendor-panel/stories ; http://127.0.0.1:3000/fa

Worker-reported serving: Next14136 BUILD_ID SpxfHsxtpvAChwrYh4xI7; Host29956 DLL F42F3132837E7DEA841C829BFFBE3428E2ADE977E5EAC003CB43E31F32F50655; health/ready/routes 200, skip-bootstrap/store-alpha. No Architect live verification.
Worker reported Host focused 3/3, FE R5+column order 8/8, retained R4 9/9, tsc/build green; zero migration/live Story-Media business writes.
Existing VendorShell/story.view/shared feature, Returns acceptances, payout backlog, contained key state and user work unchanged.
