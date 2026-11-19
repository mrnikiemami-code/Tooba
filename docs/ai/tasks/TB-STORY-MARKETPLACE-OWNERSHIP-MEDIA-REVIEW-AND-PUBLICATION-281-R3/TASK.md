# TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3
Parent-Task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Mode: FINISH_EXISTING_OWNING_PATHS_AND_REQUIRED_FAILURE_PROOF
Expected-Base-SHA: 4c086d32346ce2b6f695c254d7fa58db98c4f322
Issued-Date: 2026-11-19 (Asia/Tehran)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT

## هدف
بخش اول استوری را با بستن نقص‌های مشخص زیر تحویل بده. این تسک ادامهٔ همان تحویل است؛ بازطراحی، تحلیل دوبارهٔ کل پروژه یا تسک برنامه‌ریزی نیست. کارهای سالم 281/R1/R2 حفظ شوند. برای تمام‌کردن تست‌های صادرشده تا انتها ادامه بده؛ صرفِ کامپایل، محدودیت زمان فرضی، یا تمام‌شدن بخشی از ماتریس دلیل توقف PARTIAL نیست. اگر مانع واقعی بیرونی وجود داشت، عملیات ناموفق و شاهد دقیقش را گزارش کن؛ PASS نساز.

فقط یک worker، بدون delegation/subagent، بدون اسکرین‌شات، بدون polling loop، بدون تسک بعدی خودکار.

## 0. Claim و بازیابی
- ابتدا main/local/origin و Expected-Base را بررسی کن. فقط delta مستنداتیِ issuance/recovery مجاز است؛ source/config/schema drift را مستقل بازبینی کن و در تعارض واقعی STOP کن.
- متن دریافت‌شدهٔ Bridge را عیناً در دو TASK.md و receipt نگه دار. نبودن Content را با ساختن متن جایگزین پنهان نکن. SHA256 متن/گیت‌blob و worktree CRLF را جدا و دقیق گزارش کن.
- قبل از تغییر، AGENTS، مهارت backend، برای تغییر frontend مهارت frontend، بررسی معمار 281-R2 و COMPLETION-LEDGER را بخوان. فقط ردیف‌های باز را ببند.
- مرجع محصول محفوظ: docs/product/OPERATOR-UX-ACCEPTANCE.md و docs/planning/story/SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
- استوری فروشنده در پنل موجود قالب است: vendor-panel/layout.tsx -> VendorShell؛ منوی موجود story.view و /vendor-panel/stories. پنل/منو/ورود تازه نساز. این یادآوری الزام تسک UI آینده است؛ تغییر ظاهری در این تسک صادر نشده.

## A. یک مسیر مالکیت برای محتوای یک فروشگاه، فارغ از نقش
نقص قطعی: AdminUpdateAsync و admin item mutation از cover/media/CTA خام استفاده می‌کنند؛ در seller مسیر کنترل دارند.
- کنترل‌های موجود shop eligibility، cover/item Media Ready+tenant+shop+actual type و product CTA را در مسیرهای owning مشترک استفاده کن. Admin-for-shop همان مالکیت seller را رعایت می‌کند. محتوای ویترین اصلی تحت قاعدهٔ صریح موجود خودش بماند؛ محصول نامرتبط را با تغییر بی‌دلیل دامنه یا دادن اختیار ادمین به فروشنده حل نکن.
- در submit/approve/publish/schedule (از جمله approve-and-* و enable کلاسیک) حق انتشار را بر اساس محتوای ذخیره‌شدهٔ همان نسخه دوباره بررسی کن؛ draft ناقص مجاز، public ناقص یا با asset بی‌مجوز/غیرReady مجاز نیست. هیچ nullable service به معنی مجوز نیست.
- نگاشت خطا/پیام از catalog موجود؛ تغییر قرارداد لازم فقط additive و owning CQRS/validators/DI. ادعای برابری مسیرها با تست همان directory و adapter واقعی اثبات شود.

## B. یک پروتکل نسخه و بازپخش، همهٔ مسیرهای موجود
یک جدول کوتاه route -> command -> directory -> version -> occurrence -> test بنویس؛ سپس تمام خانه‌های باز موجود را در همین تسک پیاده کن، نه فقط مستند.
- هر تغییر موجود روی story/item/reorder/submit/moderation/schedule/enable/disable/stop/return-to-draft نسخهٔ مرورشده را در owning boundary لازم دارد. missing version نباید از read تازه پر شود یا با if(non-null) دور بخورد. ایجاد تازه نسخهٔ قبلی ندارد.
- occurrence اجباری برای intent تغییردهنده؛ هویت شامل tenant، shop/role scope لازم، action، story target، item target و همهٔ فیلدهای موثر و نسخهٔ مرورشده باشد. افزودن اسلاید اکنون storyId را hash نمی‌کند؛ آن را اصلاح کن و تست «همان key/body روی story دیگر» بگذار. Encoding کانونی و بدون delimiter ambiguity.
- بعد از authorization دامنه، بازپخش دقیق occurrence ذخیره‌شده قبل از stale/lifecycle gate پاسخ می‌دهد؛ payload/target/action متفاوت typed conflict و صفر اثر. پاسخ بازپخش نباید عملیات جدید انجام دهد یا به shop دیگر راه بدهد.
- mutation و occurrence در یک SaveChanges/تراکنش مالک ذخیره شوند. scoped identity ذخیره با lookup سازگار باشد؛ PK فعلی occurrenceKey تنهاست و tenant lookup را تضمین نمی‌کند.
- unique arbitration فقط خطای واقعی constraint/table/schema همین occurrence؛ نه تشخیص با متن exception. winner را از stored facts با همان binding بخوان. از ChangeTracker.Clear، اصلاح caller entity یا commit/rollback transaction دیگری استفاده نکن. اگر transaction caller در external-upload مجاز نیست پیش از اثر typed refusal؛ با API پشتیبانی‌شدهٔ موجود، نه چارچوب جدید.
- transport/FE فقط حمل قرارداد لازم را اصلاح کن؛ حفظ کاربران قدیمی به معنی bypass نسخه نیست. منوی نقش و actionCodes فقط اقدام واقعاً مجاز را اعلام کند.

## C. Media: الگوریتم موجود را با شکست‌های واقعی ثابت کن
R2 asset+binding+occurrence را یکجا ذخیره می‌کند؛ آن را با recording object-store و owning MediaDirectory اثبات کن، نه source regex یا fake directory.
حداقل:
1) success: یک object و یک asset Ready+binding+occurrence؛ fresh context همان‌ها را می‌خواند.
2) همان occurrence با bytes/type/name یکسان: همان asset، بدون object جدید؛ عوض‌شدن bytes یا metadata: typed conflict، صفر object/row جدید.
3) store failure و DB save failure بعد از store: هیچ asset Ready بی‌مالک/occurrence دروغ باقی نماند؛ cleanup فقط object همین attempt؛ caller tracked entity با instance/state/values حفظ شود.
4) duplicate winner و lost response: winner معتبر حفظ شود و cleanup object برنده را حذف نکند؛ unique classification دقیق، خطای نامرتبط propagate/typed طبق catalog.
5) foreign tenant/shop، asset ناآماده/غلط و adapter seller/admin-for-shop از مسیر ثبت‌شدهٔ ISender+validator+authorizer، نه تزریق admin header برای seller.
best-effort object cleanup را توزیع‌شده/اتمیک با object-store ننام. در failure مبهم، واقعیت durable را بررسی/حفظ کن؛ دادهٔ موفق را پس از پاسخ گم‌شده حذف نکن. مسیر failure را با fixture کنترل‌شده آزمایش کن، بدون object-store زنده.

## D. هویت عمومی و query، بدون اختراع دامنه
- منبع واقعی /v1/storefront/sellers/{publicId} و مصرف آن در seller-profile/[publicId] را دنبال کن. شناسهٔ عمومی فروشگاه را از همان authority از طریق Contracts/adapter قانونی batch غنی کن؛ Party Guid را URL عمومی جا نزن، الگوریتم دوم نساز و برای shop ناموجود لینک نساز.
- canonical public id/name در public Story card با owning shop منطبق باشد. عدم پاسخ را صادقانه null/typed طبق قرارداد اعلام کن.
- seller query و admin shop-name filter پیاده‌شده را روی persisted facts ثابت کن: isolation، نام انسانی، filter قبل paging، totalCount درست. اگر operatorهای نامعتبر/advanced filters بی‌صدا ignore می‌شوند از policy/validator موجود همان query استفاده کن؛ فیلتر UI در حافظه جای query سرور نیست.
- ui-contract-handoff را به نمونهٔ دقیق current wire برای create/update/items/submit/review/stop/return/upload/query تکمیل کن. این handoff برای مرحلهٔ UI است، مجوز ساخت UI تازه نیست.

## E. اثبات متمرکز، سپس پایان
مجموعهٔ کوچک و معنادار روی owner واقعی بساز؛ جدول A/B را با counterexample مثبت/منفی ببند. تستی که فقط helper یا count معماری را می‌سنجد جای رفتار owning نیست.
- Media recording-store suite بخش C.
- Story directory: fresh persisted public visibility (future/expired/unreviewed/empty hidden)، shop/media/CTA refusal، مسیر admin-for-shop هم‌ارز seller، نسخه missing/stale و replay exact/changed/cross-target، stop -> read -> return -> edit -> resubmit، نقش admin/seller.
- PostgreSQL rollback-only روی همان tooba_alpha با harness موجود: fresh-context read با Items، scoped occurrence uniqueness/winner+version concurrency، query counts/isolation. کار در تراکنش caller-owned فقط داخل تست با rollback، سپس اتصال مستقل committedRows=0. برای شکستی که transaction را abort می‌کند savepoint واقعی لازم است. InMemory proof را PostgreSQL ننام.
- DI/endpoint mapping با scope/actor واقعی test harness پوشش داده شود؛ fixture داخل تست مجاز است، live fixture ممنوع.
- فقط همین کلاس‌های focused، guard Story/Media متاثر، build/publish Host؛ در تغییر FE transport تست مرتبط+tsc، build/restart FE فقط اگر artifact مرورگر عوض شد. whole-repo/whole-Host/تکرار baselineها مجاز نیست. تکرار تست پس از اصلاح failure واقعی مجاز و در نتیجه روشن ثبت شود.
- با شکست واقعی implementation همین دامنه را اصلاح کن و دوباره focused run کن؛ تست/permission/schema pin را برای سبزکردن بی‌دلیل ضعیف نکن.

## Schema و runtime
- یک canonical tooba_alpha روی 127.0.0.1:5432، SingleStore/store-alpha؛ Host5088، FE3000.
- migrations اعمال‌شدهٔ 281/R1 immutable. پیش‌فرض migration جدید صفر؛ فقط اگر اصلاح scoped occurrence کلید واقعاً schema لازم داشت، یک migration additive محدود Story/Media با FK/model/snapshot درست مجاز است. قبل apply: بررسی pending کل ماژول‌ها، fresh private pg_dump، size+SHA256 و pg_restore --list موفق. MigrationRunner فقط pending دقیق این تسک؛ ambiguity/unrelated pending -> STOP قبل apply. هیچ Down/manual DDL/backfill/seed/repair/restore.
- runtime refresh از همان skip-bootstrap process-only موجود استفاده کند؛ هیچ seed/bootstrapping توسعه اجرا نشود. default flag/Development auth/تنظیم edition/port/keys عوض نشود. listener واقعی PID/مسیر/DLL hash/health/ready/platform را تأیید کن؛ hash فایل دیسک به‌تنهایی proof بارشدن نیست.
- ZERO live Story/Media business mutation، upload، demo row، notification/provider operation؛ live فقط GET/health. rollback fixtures پایدار نمانند. با روشن‌کردن seed برای UI نمونه نساز.
- no key read/rotation/revocation، no unrelated data/history cleanup. no second DB/Docker.

## Deliverables / gate
- update COMPLETION-LEDGER with exact method + real test + final status؛ اگر open ماند علت واقعی، نه وعدهٔ خودکار R4.
- RESULT.md، RECEIPTS.md، result-contract.txt، media-failure-proof.md، rollback-only-proof.md، all-route-matrix.md، ui-contract-handoff.md؛ task received bytes و issuance receipt.
- AGENTS CURRENT، TOOBA-RECOVERY-CONTEXT، START-HERE و Story plan به نتیجهٔ درست اشاره کنند؛ الزام VendorShell محفوظ؛ Returns و backlog روش تسویه untouched.
- normal commit/push، fresh fetch HEAD==origin/main، دقیق‌ترین Code/Evidence/Final SHA. no rebase/amend/force/reset/stash؛ scratch/key/output وارد git نشود.
- Worker PASS != Architect ACCEPT != user acceptance. UI delivery هنوز NOT ISSUED؛ فقط پس از review این نتیجه.
- یک Result کامل در Bridge، STOP_FOR_ARCHITECT، automaticNextImplementationTask NONE.

