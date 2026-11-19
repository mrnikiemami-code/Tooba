PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R5
Parent-Task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Close verified replay defects and correct overstated relational proof
Mode: NARROW_SOURCE_CONFIRMED_REPAIR_WITH_REAL_COUNTEREXAMPLES
Expected-Base-SHA: aad32713b7486c429dbbfb5e948e95e30eb6bb67
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## هدف
فقط نقص‌های مشخص R4 را ببند و شواهد آزمون را با آنچه واقعاً اجرا می‌شود منطبق کن. تحلیل/طراحی دوباره یا تکرار همهٔ baselineها نکن. کار سالم حفظ شود. تسک بعدی خودکار، delegation، screenshot و polling loop ممنوع.
قبل از کد: main/origin/base؛ فقط delta مستنداتی issuance مجاز. AGENTS، backend skill و در تغییر FE frontend skill، R4/ARCHITECT-REVIEW.md را بخوان. متن واقعی Bridge را عیناً در TASK و receipt ثبت کن؛ محتوا/هش گمشده را بازسازی پنهان نکن.
Protected product: existing VendorShell /vendor-panel/stories با story.view، بدون پنل/منو/ورود جدید. UI delivery هنوز NOT ISSUED. Returns/payout backlog/key state untouched.

## A — چهار نقص Replay، فقط روی مسیر موجود
A1) SellerUpdateAsync، AdminUpdateItemAsync، SellerUpdateItemAsync باید در owning boundary کلید غیرخالی بخواهند، intent کامل بسازند، exact stored occurrence را قبل stale/editable gate جواب دهند و mutation+occurrence را با هم ذخیره کنند. DTO/validator به‌تنهایی کافی نیست.
A2) AdminUpdateAsync تمام فیلدهای موثر title/locale/market/cover-id/cover-url/CTA و target+reviewed-version را در hash ببندد. هر فیلد جداگانه canonical/length-prefixed؛ pipe-join چند مقدار در یک extra هنوز مبهم است. همین روش برای update-item و seller update؛ ترتیب list/targets/role/shop حفظ شود. خام/null/normalized semantics باید با نوشتن owning command سازگار باشند.
A3) AdminSetScheduleAsync: tenant/role authorization و target binding، سپس exact occurrence replay، بعد برای intent تازه version/content gate. بعد از موفقیت، همان body/key/version قبلی باید replay شود؛ نسخهٔ تازه از snapshot به‌جایش نساز.
A4) Story duplicate winner: create/add/update SaveChanges در race با occurrence هم‌نام باید فقط روی 23505 + schema/table/constraint دقیق command_occurrences برنده را re-read و bind کند؛ changed intent conflict؛ unrelated unique error به replay تبدیل نشود. rollback/savepoint درست و scoped detach/reload فقط state همین attempt؛ caller tracked entity حفظ شود. SaveStoryAsync و create saves راه فرار نداشته باشند. concurrency/version loser typed و بدون occurrence جدید. framework جدید/ChangeTracker.Clear/global rollback ممنوع.

الگوی owning کوچک مشترک را استفاده کن، نه چند bypass. تمام hash/targetهای متاثر را یک‌بار بازبینی متمرکز کن تا همان omission تکرار نشود. نیازی به بازنویسی read/public/query سالم نیست.
FE فقط اگر برای رساندن همین fields لازم است؛ هیچ polish ظاهری/پنل جدید در این تسک.

## B — Media transaction guard به اندازهٔ ادعایش
Database.CurrentTransaction guard فعلی حفظ شود؛ پیش از object-store، ambient Transaction.Current و enlistment پشتیبانی‌شدهٔ owning EF relational connection نیز بررسی شود. از helper/روش موجود Returns برای reference استفاده کن، نه کپی بی‌دلیل چارچوب یا reflection روی raw connection.
typed code موجود واقعی را گزارش کن؛ R4 متن media.story_upload.ambient_transaction_unsupported نوشته ولی کد ValidationFailed می‌دهد. اگر code جدید لازم نیست جعلش نکن؛ اگر جدید شد catalog/localization/guard دقیق طبق مالک.
هیچ caller transaction commit/rollback/dispose یا تغییر entity انجام نشود. exact upload replay/durable object-success و cleanup attempt-only حفظ شوند. تغییر طراحی Media/object-store/outbox صادر نشده.

## C — تست‌های کوچک که واقعاً شکست را می‌گیرند
فقط focused classes مربوط به همین repair. اعداد/نام‌ها را از run واقعی بنویس، تست helper را directory/HTTP/concurrency ننام.

C1 (owning directory، controlled ports مجاز):
- برای A1 هر سه method: missing-key zero-write؛ success و same-key exact replay پس از نسخه/وضعیت جدید بدون اثر؛ changed body/item target typed conflict.
- A2 same-key changed locale، market، cover-id و cover-url هرکدام conflict؛ یک delimiter-collision pair با دو intent متفاوت conflict.
- A3 successful schedule سپس exact original replay همان occurrence؛ تغییر زمان conflict، stale NEW key refused. retry نباید fresh version قرض بگیرد.
- A4 exact matching occurrence collision با fault injection دارای constraint واقعی، winner سالم و یک row؛ unrelated unique propagates؛ هیچ effect دوم. تست صرف sequential replay کافی نیست.
یکی از counterexampleهای A را با کد قبل از repair یا negative control محدود قرمز نشان بده، restore و run نهایی؛ خطای آزمون را با ضعیف‌کردن assertion حل نکن.

C2 (PostgreSQL موجود، rollback-only، همان tooba_alpha):
- C1 قدیمی را به fresh StoryDbContext روی همان caller transaction تبدیل کن؛ expired negative اضافه؛ actual AdminStoryGridQueryEngine با Party port مجاز برای shop-name filter + page + totalCount و seller query isolation را بخوان. این assertionها الان در R4 نیستند.
- دو context با original version یکسان روی همان transaction: writer اول از owning command save؛ writer دوم با stale original از owning command/EF SaveChanges شکست typed و صفر occurrence اضافی. این EF concurrency است؛ مستقیم EnsureExpectedVersion عددی جای آن نیست.
- scoped PK/winner/savepoint را واقعاً بخوان؛ connection مستقل بعد rollback، test-owned Story و occurrence ids صفر.
- B: real TransactionScope ambient و owning-provider explicit enlistment به‌علاوه EF CurrentTransaction؛ پیش از action assert guard setup واقعی و بعد 0 store calls و caller state سالم. از harness/روش ثابت Returns relational استفاده کن. نیازی به committed two-writer stress نیست.
هیچ guard production برای ورود test به مسیر غیرمجاز خاموش نشود. fixture فقط داخل transaction rollback؛ no live business row/object-store. no EnsureCreated/schema/manual DDL/new DB.
C3 (Media focused):
- pre-save failure یک caller-owned tracked entity واقعی stage کند و instance/state/values قبل/بعد assert شود؛ عنوان آزمون نباید چیزی اثبات‌نشده را بگوید.
- after-save throw: fresh context durable asset+binding+occurrence و object موجود؛ replay همان id.
- Media relational committedRows=0 را برای test-owned occurrence/binding/asset ids با read مستقل ثابت کن، نه چاپ عبارت. No secret output.
اگر یک claim لازم تحت provider قابل اثبات نیست، علت واقعی و روش محدود معتبر را صادقانه بنویس؛ PASS کلی با assertion خیالی نده.

## Scope / validation / runtime
ZERO migration/DDL/backfill/seed/reset/cleanup/restore. Applied R3 occurrence migration immutable. Canonical tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha؛ Host5088 FE3000. ZERO live Story/Media mutation/upload/provider/notification، live GET only. private rollback fixtures هیچ row پایدار نگذارند.
Focused A/B/C classes و affected Story/Media guards، Release build و Host publish/refresh لازم با process-only skip-bootstrap موجود. FE فقط در تغییر transport: focused test/tsc و build/refresh در تغییر served artifact. whole Host/repository suite یا baseline replay نه. اصلاح failure واقعی و rerun focused مجاز.
ثبت listener PID/path/DLL/health/ready/platform واقعی؛ no seed on restart، no config/auth/edition/port/key change.
Normal commits/push/fresh fetch parity؛ no rebase/amend/force/reset/stash. scratch/keys/output در git نه.

## Evidence / exact success
R4 historical evidence unchanged؛ یک CORRECTION-ADDENDUM برای claimهای بیش‌ازشاهد، Code/Final SHA درست.
RESULT + RECEIPTS + exact result-contract، tests ledger «method -> assertion -> provider -> observed result»، corrected all-route-matrix، corrected relational/media proof و current wire handoff. هر جدول با source واقعی تطبیق داده شود؛ نوشتن «no OPEN» خودش اثبات نیست.
AGENTS CURRENT، recovery، START-HERE و Story plan به نتیجه درست و mandatory existing Seller panel اشاره کنند؛ تاریخچه و unrelated acceptance حذف نشود.
Success فقط: A1–A4 source+counterexamples بسته؛ B guard واقعاً ambient/enlistment را می‌گیرد؛ C assertionهای اعلام‌شده واقعاً اجرا شده و tagged durable rows=0؛ no new defect hidden. مانع بیرونی واقعی -> command/failure/log دقیق بدون secret، وضعیت PARTIAL/BLOCKED؛ «این wake اجرا نشد» blocker نیست.
آخر یک BEGIN_TOOBA_WORKER_RESULT/END_TOOBA_WORKER_RESULT با Task-ID دقیق، Status، Expected/Base/Code/Evidence/Final SHA، parity، A/B/C states، validation counts، limitations، user-work-preserved، Recovery-Next-Task NONE، Next-Recommended-Task NONE، automaticNextImplementationTask NONE، STOP_FOR_ARCHITECT.
Worker PASS != Architect ACCEPT != user acceptance. UI/shared Orders-style delivery پس از review؛ خودت task جدید صادر نکن.

END_TOOBA_TASK

