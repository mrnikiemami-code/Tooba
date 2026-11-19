# TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4
Parent-Task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Mode: CLOSE_THREE_VERIFIED_TRANSPORT_DURABILITY_AND_RELATIONAL_GAPS
Expected-Base-SHA: 14b2395eea3585b7e12af71d135c84c77ce7f9c2
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## هدف و حد کار
این تسک فقط سه بخش باقی‌ماندهٔ واقعی R3 را تمام می‌کند: مسیر کامل درخواست‌ها، شکست ذخیرهٔ فایل، و آزمون PostgreSQL. طراحی دوباره، گزارش‌نویسی به‌جای اجرا، یا تقسیم خودکار به R5 مجاز نیست. کارهای سالم 281 تا R3 حفظ شوند. «این wake اجرا نشد» مانع فنی نیست؛ در نبود مانع واقعی تا اتمام همین محدوده ادامه بده.
یک worker، بدون delegation/subagent، screenshot، polling loop یا تسک موازی.

Claim main==origin/main و base ancestor. فقط delta مستنداتی issuance مجاز؛ drift واقعی را بررسی کن، نه force/reset/rebase/stash. متن Bridge/TASK را عیناً ثبت کن؛ content unavailable را بازسازی پنهان نکن. قبل کد: مهارت backend و در تغییر FE مهارت frontend، AGENTS، R3/ARCHITECT-REVIEW.md، all-route-matrix.md.
شناسه و header همین تسک ثابت است؛ Task-ID/Parent-Task/Worker-Id/Agent-Type/Stop-State را با نام اختراعی عوض نکن.

## A — مسیر کامل درخواست، نه فقط امضای directory
۱) سه regression مشخص را ببند:
- نسخهٔ لازم در admin remove-item / reorder-items و seller remove-item از endpoint به command، validator، composer، interface و directory منتقل شود. trailing optional parameter که هیچ caller مقدارش را نمی‌فرستد تحویل نیست؛ missing/stale همچنان typed refusal.
- AdminSetScheduleAsync همان stored-content authorization لازم را رعایت کند. submit/approve/composite/enable/schedule محتوای همان نسخه را می‌سنجند.
- platform Story با product CTA به‌دلیل null بودن sellerPartyId ممنوع نشود. محصول ویترین اصلی از مرجع Catalog موجود معتبر بودنش ثابت شود؛ shop Story علاوه بر اعتبار، مالکیت فروشگاه را رعایت کند. یک authority و adapter قانونی، نه حذف کنترل یا سرویس جدید برای دورزدن آن.

۲) ماتریس همهٔ routeهای تغییردهندهٔ موجود را کامل کن:
create, update, story reorder, item add/update/delete/reorder, submit, approve/reject, approve-and-publish/schedule, enable/disable/schedule, seller stop/return. route/read/query نیاز occurrence ندارد.
هر mutation یک occurrence غیرخالی و هر رکورد موجود نسخهٔ مرورشده می‌خواهد. reorder چند Story نسخهٔ هر رکورد متاثر را لازم دارد و همه یا هیچ ذخیره می‌شود. مرور تازهٔ سرور جای نسخهٔ کاربر نیست.
یک پروتکل owning مشترک برای replay-before-stale پس از authorization دامنه، hash تمام intent شامل tenant/shop/action/story/item/ordered ids/versions/body، conflict برای تغییر intent و ذخیرهٔ mutation+occurrence با هم. از framework عمومی جدید یا چند کپی قانون نساز.
مسیر create تکراری هیچ رکورد دیگری نسازد؛ replay مسیر item هدف اشتباه را جواب ندهد. scoped unique winner فقط با constraint واقعی خودش fold شود. caller state با Clear/global detach از بین نرود.
admin compositeها باید occurrence را روی wire داشته باشند. seller/admin create/add/submit/stop/return که optional بودند بدون occurrence دیگر اثر نکنند. old callers fail closed؛ bypass برای سازگاری نساز.
FE transport موجود را برای همین قرارداد حمل کن، از جمله version و occurrence در delete/reorder/composites. کلید یک درخواست مرورشده در retry همان است؛ در کلیک/ارسال دوباره بی‌دلیل کلید تازه نساز. این task مجوز ساخت editor/list جدید نیست.
all-route-matrix هر route -> body/header/query -> command -> owner -> test واقعی را ثبت کند؛ خانهٔ OPEN باقی نگذار.

## B — شکست واقعی Media را تزریق و اصلاح کن
تست Db_save_failure_after_store_cleans_attempt_object_and_detaches در R3 فقط replay عادی است. نام/ادعایش را صادقانه اصلاح کن؛ سپس tests واقعی از MediaDirectory با recording store و SaveChanges/commit interceptor کنترل‌شده بساز:
- store موفق، DB fail قطعی قبل commit: صفر asset/owner/occurrence durable؛ فقط object همان attempt حذف؛ caller entity instance/state/values سالم.
- DB success با پاسخ گم‌شده (after-save throw): fresh durable read نشان دهد asset+binding+occurrence صحیح و object باقی است؛ retry همان asset را جواب دهد، نه حذف object موفق و نه asset دوم.
- collision واقعی/کنترل‌شده با winner: loser فقط object خودش را cleanup کند؛ winner unchanged. changed payload conflict. خطای unique نامرتبط وارد winner-fold نشود.
- PostgreSQL identity دقیق SqlState+SchemaName+TableName+ConstraintName برای occurrence؛ substring table OR constraint جای آن نیست. نام واقعی model/migration را بخوان، حدس نزن.
- caller/ambient transaction upload را اگر owning durable guarantee ندارد پیش از object اثر typed refusal کن؛ transaction دیگری را commit/rollback/dispose نکن. DB commit مبهم را failure قطعی فرض نکن. cleanup بهترین‌تلاش را distributed transaction ننام.
حداقل تغییر owning code برای گذر از این counterexampleها؛ object-store/outbox/worker جدید نساز. اگر durable outcome را نمی‌توان خواند، object احتمالا موفق را حذف نکن؛ پاسخ صادقانه و intent قابل‌بازپخش حفظ شود.
این tests فایل/object-store زنده مصرف نمی‌کنند. success/replay/store-fail/foreign happy paths R3 حفظ شوند.

## C — سه بستهٔ کوچک PostgreSQL واقعی، rollback-only
روی canonical tooba_alpha با harness موجود Host تست کن؛ ایجاد DB دوم، Docker، seed یا commit fixture ممنوع.
C1 fresh context persisted Items + publication visibility (publishable positive؛ future/expired/unreviewed/empty negative)، seller isolation و admin shop-name filter/paging/count روی دادهٔ همان تراکنش.
C2 occurrence composite key روی دو tenant با همان key، real duplicate constraint و winner read، نسخهٔ stale و واقعی EF concurrency از دو context، صفر mutation/occurrence اضافه در loser. برای 23505 داخل transaction savepoint درست؛ context دوم می‌تواند همان transaction را enlist کند. concurrency را در همین محدوده ثابت کن، stress/committed two-writer race لازم نیست.
C3 Media B را با recording object-store روی relational context اثبات کن؛ مسیر دارای caller transaction اگر اکنون قبل اثر رد می‌شود همان refusal را relational ثابت کن و شکست save/success-ack را با controlled interception در test provider مناسب اثبات کن؛ bypass production guard نساز و test-only fake Directory ننویس. محدودیت provider را دقیق گزارش کن.
هر بسته finally rollback و اتصال مستقل committedRows=0 برای شناسه‌های test-owned؛ قبل/بعد rows واقعی دست‌نخورده. EnsureCreated/DDL دستی ممنوع؛ schema موجود استفاده شود.
فقط اجرای migration قبلی proof رفتار نیست. اگر اتصال/دسترسی واقعا شکست خورد، command+typed failure بدون secret گزارش کن؛ بدون تلاش واقعی نگوی unavailable.

## Validation و تحویل
Focused A/B/C tests + Story/Media guards متاثر، backend build/Host publish؛ برای FE transport تست مرتبط+tsc و در تغییر artifact build/restart لازم. whole-repository/whole-Host suite یا baseline replay نه. بعد از failure واقعی اصلاح و focused rerun مجاز؛ runs را صادقانه ثبت کن. تست pin را بدون تغییر قرارداد واقعی جابه‌جا نکن.
یک critical failure را برای اثبات تست با implementation قدیمی یا یک negative control محدود نشان بده، سپس restore و run نهایی. هیچ ابزار debug در محصول نماند.
FINAL PASS فقط وقتی A/B/C اجرا و بسته شده‌اند؛ external blocker واقعی با شاهد -> BLOCKED/PARTIAL و دقیق، نه ادعای readiness. 57/57 قدیمی جای این proof نیست.

Schema/migration/DDL: ZERO. migration اعمال‌شدهٔ 20261119020000_Tb281R3TenantScopedCommandOccurrences و Media قبلی immutable، دوباره apply/ویرایش نشوند.
Runtime: یک tooba_alpha 127.0.0.1:5432، SingleStore/store-alpha، Host5088، FE3000. refresh فقط با skip-bootstrap process-only موجود؛ هیچ development seed/setting/edition/port/key تغییر نکند. actual listener PID/path/DLL+health/ready/platform را ثبت کن.
ZERO live Story/Media business POST/upload، demo case/row، notification/provider، reset/reseed/backfill/cleanup/restore؛ live GET فقط. هیچ key operation مجاز نیست. fixture تست پس از rollback پایدار نباشد.
normal commit/push/fetch parity؛ no rebase/amend/force/reset/stash. scratch scripts/keys/binaries را stage نکن.
RESULT، RECEIPTS، result-contract، updated COMPLETION-LEDGER + all-route-matrix، media-failure-proof، rollback-only-proof، exact ui-contract-handoff. historical report را بازنویسی نکن؛ correction addendum برای R3 test name/Final-HEAD.
AGENTS/recovery/START-HERE/Story plan به نتیجهٔ صحیح به‌روز شوند. mandatory seller UI reminder: purchased template EXISTING VendorShell و /vendor-panel/stories با story.view؛ shared feature، no duplicate panel/menu/login. UI همچنان PLANNED NOT ISSUED تا review؛ Returns و payout backlog محفوظ.
Worker PASS != Architect ACCEPT != user acceptance. یک Result نهایی، STOP_FOR_ARCHITECT، automaticNextImplementationTask NONE.

