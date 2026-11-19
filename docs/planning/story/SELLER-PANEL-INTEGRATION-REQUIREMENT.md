# CURRENT — OWNER REJECTED STORY UX; ONLY TB-282-R6 ISSUED — 2026-11-19

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R6
Expected-Base-SHA: 5a0b341ab5d3fcd7ab8539d3f5c488562529dd6b
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R6/TASK.md
Evidence/receipt: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R6/
Owner rejected real admin/seller screens: incomplete Persian, ambiguous shop/placement, list review filter in detail, unchanged title-only seller creation. R5 remains accepted only for limited owner trial; visual/product acceptance REJECTED.
One consolidated R6 implements complete shared creation/edit form in BOTH existing admin and VendorShell, truthful shop/main context, list-only review filter, merchant localization and final served-role verification. Preserve R5 identity/retry/lifecycle and all unrelated work.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R6 (ISSUED; not claimed/running)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
One worker tooba-worker-01 / tooba-main. No delegation/screenshots/polling loop. ZERO migration/live business writes. Await worker result then independent Architect review; do not repeat unchanged UI handoff.
URLs /fa/admin/stories ; /fa/vendor-panel/stories ; /fa

# HISTORICAL — superseded by CURRENT above

# CURRENT — STORY READY FOR OWNER TEST; NO IMPLEMENTATION TASK ACTIVE — 2026-11-19

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R5/ARCHITECT-REVIEW.md
Reviewed code 352a252ad9cf21645cf54c2d71a51e91c4da9d98 / implementation-evidence main 71e489bcd7a0a1b871dad6bcec86bca1050c6e96.
Decision ACCEPT_FOR_OWNER_TEST_WITH_DECLARED_LIMITS. Product names are bound per slide; shared edit uses existing update-item with preserved media; actual Catalog query >40 proof present at EF InMemory scope. No further automatic repair task. Full A-D proof certification/user/commercial acceptance NOT claimed.
State: AWAITING_OWNER_TEST
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
User requested fast handoff: now user tests admin creation/main-or-shop, seller own-shop/product slides+edit/reload, submit -> admin moderation -> public playback/product/shop link, mobile grid/form. Title first/actions last existing logical-end pin must be checked in real active UI.
Important evidence limits: shell-only served geometry is NOT active editor/grid proof; retention test is direct executor, not mounted pending-card/no-toast; detailed limitations in review. Do not reopen task series solely to recertify these before owner feedback. Actual owner failure -> one narrow justified repair.
URLs http://127.0.0.1:3000/fa/admin/stories ; http://127.0.0.1:3000/fa/vendor-panel/stories ; http://127.0.0.1:3000/fa
Worker reported Next BUILD_ID SpxfHsxtpvAChwrYh4xI7 / Host29956; Architect source review only, no live writes/tests. No business fixtures/seeds/migrations. Existing VendorShell/story.view, Returns/payout backlog/separate key state preserved.

# HISTORICAL — superseded by CURRENT above

Worker PASS within issued A–D (F1 per-slide CtaProductTitle, F2 mounted update_item + retention + negative control, C real Catalog >40). Evidence: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R5/.
NOT Architect ACCEPT, NOT user acceptance. User-Review-URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa.
activeTask: NONE; automaticNextImplementationTask: NONE; STOP_FOR_ARCHITECT_AND_USER_REVIEW.
Seller remains inside existing VendorShell /vendor-panel/stories /story.view — no parallel panel/menu/login.

# HISTORICAL — TB-282-R5 ISSUED; WORKER PASS ABOVE

# HISTORICAL — TB-282-R4 REVIEWED; 282-R5 WAS ISSUED

Reviewed implementation 35b9ffc6406a770147ddfd8fc1ef514f0db20a1e; fresh main/evidence 08f73aaa7dce43c73ff25d4a67c8ffec6a394314.
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4/ARCHITECT-REVIEW.md.
Retain compact save/localized file/shared slide Dialog/update-item wire and upload dispatch gate progress. Full A-E PASS not accepted: F1 row product label borrows current editor selection and lacks stored target name hydration; F2 actual save/no-extra-write, malformed-response mounted proof, true negative control, real grid/saved-view/geometry and owning Catalogue query proof remain incomplete. Finish existing narrow obligations only; no new UI/backend phase.
Correction: actions logical-end pin already comes from existing legacy-grid-bridge, so missing sticky on Story column alone was not proof of missing pin.
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
Owner may inspect /fa/admin/stories, /fa/vendor-panel/stories and /fa; no user/commercial/complete connected acceptance claimed. Worker-reported Next build VKOLCT7WFUKHA67tlaFV-; Host18724; Architect source/evidence review only.
Existing VendorShell /vendor-panel/stories/story.view retained; seller fixed shop/products, admin shop/main placement and moderation. ZERO migration/live-write scope; Returns/payout backlog/separate key state unchanged.

# HISTORICAL — superseded by CURRENT above

Read docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4/TASK.md and evidence TB-...-282-R4/{RESULT,RECEIPTS,COMPLETION-LEDGER,user-review}.
Status PASS within issued A–E (Orders-style list, shared merchant editor fa/en catalogs, per-slide update_item, S1 bind every send, S2 >40 catalogue proof). Host5088/Next3000 BUILD_ID VKOLCT7WFUKHA67tlaFV-; ZERO migration; ZERO live Story/Media write.
NOT Architect ACCEPT, NOT user acceptance. User-Review-URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa.
Expected-Base f35f476c51346858bb1d61e96bb5e7d2068c5a79. automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT_AND_USER_REVIEW.
Seller remains VendorShell /vendor-panel/stories /story.view — no parallel panel.

# HISTORICAL — TB-282-R4 ISSUED; WORKER PASS ABOVE


# HISTORICAL — superseded by CURRENT above

Reviewed implementation 70c5dd36a2a9508f7a95edce1ee575c0fda9dfae / evidence 9047660042696e205c04cf161c5c035abd912840.
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R3/ARCHITECT-REVIEW.md.
Retain malformed-2xx retention, explicit storage barriers and real published-catalogue search/page/count source progress. Worker PASS does NOT close issued S1/S2: upload_add executor bypasses the allowed-route gate; new R3 tests do not mount the workspace or run the claimed negative control; required >40-product owning-query/paging/shop-isolation proof is absent. Only finish these existing obligations; no UI redesign/backend wave/Gate0/Media reopening.
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
Current screens may be visually inspected at /fa/admin/stories, /fa/vendor-panel/stories and /fa; full connected/user/commercial acceptance NOT claimed. Worker-reported Next build fFmkskCMJiO4PrZyHPO6l, Host36320; Architect source review only, no live writes/tests.
Seller remains inside existing VendorShell /vendor-panel/stories/story.view, no parallel shell/menu/login. Returns acceptance, payout backlog and separate key state unchanged.

# HISTORICAL — superseded by CURRENT above

TB-282-R3 worker PASS closed S1/S2 on the existing VendorShell/story.view/shared editor path. No parallel panel/menu/login. Await Architect/user review; automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Existing VendorShell/story.view integration remains mandatory. Current task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2 finishes actual recovery, lawful lookup/date UI, scoped grid/shell and served proof. No duplicate panel/menu/login. Historical status blocks below do not issue tasks. Await R2 result; automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Only issued task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1. Original integration requirements below remain binding. Complete real VendorShell behavior and mounted proof; regex checking the page import is not acceptance. Shared feature within existing seller shell/auth, no duplicate menu/panel/login. Prior task/status paragraphs below are historical. Await R1 result; automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

The existing seller-panel requirement below remains binding. Current task is TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282. See its exact TASK.md and top recovery. Historical R2 active / UI NOT ISSUED statements below record the original note only; 282 now explicitly authorizes the shared UI after its Gate0. No duplicate panel/menu/login; one worker, await result. automaticNextImplementationTask NONE.

# الزام اتصال استوری به پنل فروشندهٔ موجود قالب

تاریخ: ۱۹ نوامبر ۲۰۲۶
وضعیت: دستور صریح صاحب محصول؛ فقط یادداشت و بازیابی به‌روز شد، پیاده‌سازی جدید صادر نشد.

## خواسته

صاحب محصول تأکید کرد پنل Seller در قالب خریداری‌شده موجود است و استوری باید به همان پنل اضافه/تکمیل شود. پنل فروشندهٔ مستقل، پوستهٔ تازه، ورود جداگانه یا کپی پنل ادمین ساخته نشود.

## تأیید از سورس

مبنای بررسی: `73202686f10ee4bceeb56fc4a2e43d0e74d8f949`.

- `src/frontend/app/vendor-panel/layout.tsx`: پوستهٔ موجود را با `VendorShell` دور children می‌گذارد؛ `data-panel-theme="seller"` و `seller-panel-theme-root`.
- `src/frontend/app/vendor-panel/vendor-shell.tsx`: پوستهٔ فروشنده با هندسهٔ Shopeiva، header/sidebar/drawer و زمینهٔ Actor+Seller موجود است.
- همان فایل، ردیف منوی `stories` با عنوان «استوری‌ها»، مسیر `/vendor-panel/stories` و `viewPermission: "story.view"` از قبل دارد؛ هنگام تکمیل، منوی تکراری اضافه نشود.
- `src/frontend/app/vendor-panel/stories/page.tsx` از قبل `StoryManagementScreen` مشترک را با `SELLER_STORY_CAPABILITIES` نمایش می‌دهد.
- `src/frontend/app/vendor-panel/page.tsx` داشبورد فعلی فروشنده است؛ برای پیاده‌سازی استوری به بازنویسی داشبورد نیاز نیست.
- مسیر ادمین همچنان `/admin/stories` و پوستهٔ ادمین است؛ اشتراک در feature/editor/grid است، نه تعویض پوستهٔ هر نقش.

## الزام تحویل رابط بعدی

۱. تکمیل در همان `/vendor-panel/stories`، داخل VendorShell موجود؛ sidebar/header/drawer/نشان فعال/RTL/موبایل قالب حفظ شوند.
۲. همان فرم و منطق feature مشترک با ادمین، با داده و امکانات مجاز همان فروشگاه. ادمین بررسی/تأیید و انتخاب فروشگاه دارد؛ فروشندهٔ عادی تأیید/دادهٔ دیگر فروشگاه‌ها ندارد.
۳. actor/shop و مجوز از زمینهٔ موجود و مرجع سرور استفاده کنند؛ auth محلی موازی، هدر ادمین یا جعل نقش برای کارکرد فرم ساخته نشود.
۴. نمای ذخیره‌شده، state/intent و دادهٔ grid میان admin و فروشگاه‌های مختلف مخلوط نشوند؛ تغییر زمینهٔ فروشگاه نباید پاسخ قبلی را روی فروشگاه جدید بنشاند.
۵. آزمون یکپارچهٔ صفحه در پوستهٔ واقعی VendorShell، مسیر منو، مجوز story.view، نقش/فروشگاه و موبایل جزء پذیرش تحویل رابط باشد؛ تست کامپوننت بی‌پوسته به‌تنهایی کافی نیست.
۶. جدول از الگوی Orders استفاده کند ولی ظاهر و ناوبری Seller قالب را به Admin تبدیل نکند.

## وضعیت اجرا و بازیابی معمار

تسک فعال همان TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2 است؛ دامنهٔ آن با این یادداشت گسترش نمی‌یابد. رابط مشترک هنوز تسک صادرشده ندارد. هنگام نوشتن تسک رابط بعدی، این فایل باید مرجع اجباری باشد. منتظر نتیجهٔ R2 و بررسی معمار بمانید؛ خودکار تسک بعدی اجرا نشود.

automaticNextImplementationTask: NONE
New-Implementation-Issued-By-This-Note: NO
