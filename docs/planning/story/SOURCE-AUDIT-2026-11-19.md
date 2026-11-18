# گزارش بررسی کد استوری برای مارکت‌پلیس

تاریخ: ۱۹ نوامبر ۲۰۲۶
مبنای بررسی: `e242d4dd9cc69c9094187caf838bb60d0cd87c70` در `mrnikiemami-code/Tooba`
وضعیت: تحلیل سورس و برنامه‌ریزی؛ پیاده‌سازی یا تسک اجرایی صادر نشده است.

## نتیجهٔ محصول

بخش فعلی «زیرساخت استوری با پنل نیمه‌کامل» است؛ هنوز تجربهٔ قابل‌فروش برای کاسب و مارکت‌پلیس نیست. ساخت دوباره از صفر لازم نیست. پنل مشترک، تفکیک مالک فروشنده، ارسال برای بررسی و تأیید/رد وجود دارد؛ شکاف اصلی، تکمیل همان مسیر تا نمایش واقعی و ساخت استوری به نام فروشگاه توسط ادمین است.

خواستهٔ مالک: هر فروشگاه استوری خودش را ثبت کند؛ ادمین بررسی کند؛ ادمین از پنل خودش برای فروشگاه انتخاب‌شده هم استوری بسازد. فرم و تجربه مشترک باشند، اختیار تأیید/رد فقط با ادمین. فهرست از الگوی خوانا و کم‌دکمهٔ سفارش‌ها استفاده کند.

## روش و حدود بررسی

صفحات واقعی ادمین/فروشنده، کامپوننت مشترک، API کلاینت، Endpoints/Commands/Composer/Directory/Domain، گرید و مدل EF، نمایش عمومی و تست‌های موجود خوانده شدند. مرزهای Media، Party و رجیستری زبان نیز برای امکان استفادهٔ مجدد بررسی شدند. دو تصویر صاحب محصول مرجع مشکل ظاهری‌اند؛ از localhost کاربر بازدید نکردم، تست/بیلد/بانک اطلاعاتی اجرا نشده و هیچ داده یا کد اجرایی تغییر نکرده است. موارد زیر نتیجهٔ خواندن کدند، نه اثبات کارکرد محیط زنده.

## چه چیزهایی از قبل هست؟

| قابلیت | وضعیت واقعی |
| --- | --- |
| پنل مشترک ادمین و فروشنده | هر دو به StoryManagementScreen می‌رسند و capabilities متفاوت می‌گیرند |
| مالکیت فروشنده | SellerPartyId از authorizer سمت سرور؛ فهرست/جزئیات/ویرایش به tenant و فروشندهٔ همان رکورد محدود است |
| ارسال و بررسی | پیش‌نویس/ردشده -> Submitted؛ تأیید یا رد با دلیل توسط ادمین؛ فروشنده endpoint تأیید/انتشار ندارد |
| قواعد نمایش پایه | منشأ ادمین یا تأیید فروشنده، وضعیت فعال و بازهٔ زمانی در خواندن عمومی کنترل می‌شود |
| عکس و ویدیو و چند اسلاید | مدل و پخش عمومی وجود دارند؛ افزودن/ویرایش/حذف/ترتیب اسلاید در backend موجود است |
| فهرست مدیریتی | AppDataGrid و query صفحه‌بندی‌شدهٔ دیتابیس دارد؛ مشکل فقط انتخاب جدول نیست |
| همزمانی | VersionToken در EF concurrency token است؛ محافظت از «نسخه‌ای که اپراتور دیده» هنوز در فرمان HTTP ارسال نمی‌شود |

منابع: StoryManagementScreen.tsx، story-capabilities.ts، StoryAdminEndpoints.cs، StorySellerEndpoints.cs، StoryDirectory.cs، Story.cs و StoryDbContext.cs در مسیرهای انتهای گزارش.

## یافته‌ها

### S01 — ادمین نمی‌تواند برای فروشگاه مشخص استوری بسازد — مانع اصلی خواستهٔ مالک

CreateStoryBody و CreateStoryCommand مالک فروشگاه ندارند. AdminCreateAsync همیشه CreateDraft را صدا می‌زند؛ آن factory مقدار Origin=Admin و SellerPartyId=null می‌گذارد. فروشنده هم فقط Origin=Seller و SellerPartyId خودش را می‌بیند. بنابراین صرف افزودن انتخابگر فروشگاه در ظاهر این نیاز را حل نمی‌کند.

شاهد: StoryHttpModels.cs:6–14؛ StoryModels.cs:73–82؛ StoryDirectory.cs:99–120؛ Story.cs:87–106؛ StoryDirectory.cs:624–636 و 670–680.
نیاز: فرمان مجاز ادمین با فروشگاهِ اعتبارسنجی‌شده، مالکیت درست، ثبت هویت ادمینِ ایجادکننده بدون جعل هویت فروشنده، و مشاهدهٔ همان استوری در پنل فروشگاه.

### S02 — جدول شلوغ است و الگوی سفارش‌ها را درست به‌کار نگرفته

عنوان sticky است، ولی عملیات چند دکمهٔ رنگی و پهن در هر ردیف دارد. شناسهٔ کوتاه فروشنده به‌جای نام فروشگاه، ستون‌های locale و market انگلیسی و شمارش آیتمِ بی‌معنا برای کاسب دیده می‌شوند. Orders همان AppDataGrid را با AppGridRowActionsCell و منوی عملیات جمع‌وجور استفاده می‌کند؛ نیازی به ساخت جدول دیگر نیست.

شاهد: StoryManagementScreen.tsx:128–318؛ admin-screens.tsx:218–320 و 341–360.
همچنین Admin هم listAdminStories را برای همهٔ رکوردها می‌خواند و هم query گرید را؛ انتخاب جزئیات از rows فهرست محلی انجام می‌شود و شمارش بالای صفحه rows.length است، نه نتیجهٔ فیلتر/صفحهٔ گرید. Seller همهٔ رکوردها را می‌گیرد و client-side فیلتر می‌کند. هر دو از ADMIN_STORY_GRID_VIEW_KEY یکسان استفاده می‌کنند.
نیاز: داده و شمارش هم‌معنا با query، جزئیات مستقل با GET، گرید فروشندهٔ owner-scoped و ذخیرهٔ نمای جدا برای نقش/فروشگاه. سرچ و فیلتر نام فروشگاه در StoryAdminGridPolicies فعلی نیست.

### S03 — فرم و مدیریت محتوا نیمه‌کامل‌اند

جزئیات زیر جدول باز می‌شود. ساخت در div ثابت سفارشی است، رد از window.prompt هم انجام می‌شود. فرم، locale/coverMediaUrl/ctaType/ctaTarget خام می‌خواهد و رسانهٔ نمونه پیش‌فرض دارد. برای اسلاید فقط mediaType و mediaUrl و افزودن در UI هست؛ ویرایش مشخصات اصلی، ویرایش/حذف/جابجایی اسلاید، توضیح/مدت/انتخاب مقصد واقعی در این صفحه وصل نشده‌اند، هرچند بخشی از API موجود است.
شاهد: StoryManagementScreen.tsx:86–96، 280، 396–577، 580–628.
نیاز: ویرایشگر مشترک با انتخاب فایل، پیش‌نمایش، متن ساده، مدال قالب و Toast واقعی؛ نه فرم فنی.

### S04 — انتشار می‌تواند قبل از محتوای قابل‌پخش انجام شود

ساخت ادمین بلافاصله enable را صدا می‌زند، قبل از افزودن اسلاید؛ نتیجهٔ enable نیز جدا بررسی نمی‌شود. Activate و SubmitForReview وجود اسلاید معتبر/آماده را الزام نمی‌کنند. Domain آیتم می‌تواند mediaAssetId و mediaUrl هر دو خالی داشته باشد. PublicStoryCard با Items خالی برمی‌گردد ولی StoryModal با items.length=0 اصلاً باز نمی‌شود.
شاهد: StoryManagementScreen.tsx:610–623؛ Story.cs:211–237؛ StoryItem.cs:43–79؛ StoryDirectory.cs:57–61؛ story-modal.tsx:100–106 و 288.
نیاز: پیش‌نویسِ خالی مجاز؛ ارسال برای بررسی/انتشار فقط با محتوای قابل‌پخش و تأییدشدهٔ رسانه. ذخیره با انتشار یکی نباشد.

### S05 — زمان‌بندی آینده در مسیر فعلی به شروع نمایش نمی‌رسد

SetSchedule برای شروع آینده Status=Scheduled می‌گذارد؛ GetPublicStoriesAsync فقط Active را از DB می‌گیرد و IsPubliclyVisible هم Active می‌خواهد. گذشت زمان به‌تنهایی این دو شرط را عوض نمی‌کند. در module registration و فایل‌های Story مسیر job انتقال Scheduled به Active دیده نشد. تست موجود فقط «قبل از شروع دیده نشود» را می‌سنجد، نه دیده‌شدن پس از شروع.
شاهد: Story.cs:193–208 و 306–311؛ StoryDirectory.cs:30–43؛ StoryModule.cs؛ StoryFoundationTests.cs:106–121.
همچنین Activate بعد از Expired/Disabled زمان‌های قبلی را اصلاح نمی‌کند؛ ممکن است برچسب Active باشد ولی به‌خاطر EndAt گذشته دیده نشود.
نیاز: یک قاعدهٔ قطعی و مشترک «وضعیت واقعی انتشار»؛ آزمون مرز شروع/پایان و روشن‌بودن بازنشر. راهکار بر اساس کد جاری انتخاب شود، نه افزودن daemon بی‌دلیل.

### S06 — بارگذاری رسانه برای فروشنده، مسیر آمادهٔ قابل‌اتکا ندارد

Story هیچ پورت Media resolve/ownership/ready را در Directory/Composer مصرف نمی‌کند؛ فقط مرجع/URL ذخیره می‌کند. کتابخانهٔ آمادهٔ MediaLibraryDialog به API ادمین وصل است. MediaAdminEndpoints مجوز IAdminPanelAccess می‌خواهد؛ اتصال آن به فروشنده با هدر ادمین راه‌حل نیست. Media upload Contracts موجود برای Payment طراحی شده و مالکیت فروشگاه را به‌تنهایی اثبات نمی‌کند.
شاهد: StoryDirectory.cs constructor و create/item methods؛ media-library-dialog.tsx:200–205؛ MediaAdminEndpoints.cs:28–51؛ IMediaAssetUploadPort.cs.
نیاز: reuse ظاهر/زیرساخت Media، با API مجاز و مالکیت واقعی فروشنده؛ نوع/حجم/آمادگی و حق استفاده از asset در سرور. عدم دسترسی به کتابخانهٔ دیگر فروشگاه‌ها.

### S07 — فروشنده پس از تأیید عملاً راه ویرایش دوباره ندارد

IsSellerContentEditable فقط Draft + None/Rejected را مجاز می‌کند. UI هم approved را غیرقابل‌ویرایش می‌داند؛ مسیر درخواست تغییر/بازگشت به پیش‌نویس/توقف توسط فروشنده وجود ندارد. UI canEditSellerStory حتی Status را بررسی نمی‌کند و برای Disabled+None ممکن است دکمه نشان دهد که سرور رد می‌کند.
شاهد: Story.cs:157–161؛ StorySellerEndpoints.cs:24–34؛ story-capabilities.ts:39–51.
نیاز: چرخهٔ روشن تغییر محتوای تأییدشده با بازبینی دوباره؛ تغییر تأییدنشده عمومی نشود. اختیار توقف استوری خود، پیشنهاد زمان‌بندی و بازنشر در طرح محصول مشخص شود.

### S08 — تأیید محتوا با انتشار دو کارند، ولی تجربه مبهم است

Approve فقط ReviewStatus را عوض می‌کند و هنوز Active نمی‌کند؛ انتخاب زمان‌بندی/فعال‌سازی جدا لازم است. در UI دکمهٔ «فعال» حتی روی draft فروشندهٔ تأییدنشده نشان داده می‌شود و سرور ردش می‌کند. یک قاعدهٔ مشترکِ عملیاتی بر اساس role+origin+status+review+window لازم است؛ پرچم ثابت frontend به‌تنهایی کافی نیست.
شاهد: Story.cs:240–264؛ StoryManagementScreen.tsx:239–265؛ story-capabilities.ts.
هدف پیشنهادی: ادمین محتوای دیده‌شده را «تأیید و انتشار» یا «تأیید و زمان‌بندی» کند؛ تغییر دقیق معنای فرمان‌ها در اجرای آینده به‌صورت صریح و آزموده، نه دو درخواست پنهان.

### S09 — هویت فروشگاه در نمایش عمومی و پنل گم می‌شود

PublicStoryCard نه شناسهٔ عمومی فروشگاه/نام/نشان آن را دارد، نه scope فروشگاه در endpoint عمومی هست؛ فقط tenant+locale+market است. StoryModal برچسب @story و «اکنون» ثابت نشان می‌دهد. صفحهٔ seller-profile/[publicId] فقط محصولات دارد و Story را مصرف نمی‌کند.
شاهد: StoryModels.cs:19–28؛ StoryStorefrontEndpoints.cs:23–34؛ story-modal.tsx:390–403؛ seller-profile/[publicId]/page.tsx.
نیاز: خواندن نام/هویت مجاز از Party/مرجع فروشگاه با Contracts؛ ریل عمومی با انتساب روشن به فروشگاه. نمایش فقط استوری همان فروشگاه در صفحهٔ اختصاصی، اگر در تحویل نهایی وارد دامنه شود، باید scope واقعی داشته باشد.

### S10 — چند دکمهٔ عمومی صرفاً ظاهری‌اند

لایک/ذخیره/نظر در useState هستند و با تغییر استوری پاک می‌شوند؛ نظر به سرور ارسال نمی‌شود. «گزارش مشکل» handler ندارد. share لینک صفحهٔ جاری را می‌دهد و شناسهٔ استوری را اضافه نمی‌کند؛ بازکردن همان استوری برای گیرنده ثابت نشده. در خطای رسانه URL خام نمایش داده می‌شود.
شاهد: story-modal.tsx:81–93، 150–158، 207–233، 235–252، 361–365 و 417–425.
نیاز: دکمهٔ بدون قابلیت واقعی در نسخهٔ قابل‌فروش نمایش داده نشود. لازم نیست همین تحویل تبدیل به شبکهٔ اجتماعی شود؛ قابلیت‌های اجتماعی فقط با قرارداد و ذخیرهٔ واقعی و دامنهٔ جدا. اشتراک‌گذاری باید همان استوری مجاز را باز کند، نه صرفاً Home.

### S11 — زمان ویدیو و CTA باید با هدف فروش هماهنگ شوند

پیشرفت ویدیو از تایمر ثابت است؛ ویدیو loop دارد و به ended/currentTime متکی نیست؛ Pause فقط تایمر را متوقف می‌کند نه خود ویدیو. مقصدها متن خام‌اند؛ «product/category» هویت واقعی محصول و مالکیت فروشنده را اثبات نمی‌کنند و storefront عمدتاً target را مسیر فرض می‌کند.
شاهد: story-modal.tsx:36–43، 166–190، 258–276، 337–346؛ StoryRules.cs:47–92.
نیاز: پخش و توقف واقعی، خطای خوانا، مقصد انتخاب‌شده و معتبر؛ جلوگیری از معرفی کالای دیگر فروشگاه از مسیر فروشنده طبق قرارداد عرضهٔ Marketplace.

### S12 — زبان/تاریخ و تست، هنوز معیار محصول را پوشش نمی‌دهند

draft.locale پیش‌فرض fa و input آزاد است؛ رجیستری زبان وصل نیست. datetime-local میلادی/منطقهٔ مرورگر استفاده می‌شود. validator عمومی fa-IR را با raw.All(char.IsLetterOrDigit) رد می‌کند، با اینکه MatchesLocale زبان دارای خط تیره را می‌شناسد.
تست frontend سه تست capabilities و سه تست mapping است؛ mounted workflow دوپنلی در بخش Story پیدا نشد. StoryFoundationTests از Testcontainers/Docker و seed استفاده می‌کند و در نبود Docker skip دارد؛ این تحلیل هیچ‌کدام را اجرا نکرده است.
شاهد: StoryManagementScreen.tsx:59–72، 86–91 و 483–517؛ StoryValidators.cs:223–231؛ StoryFoundationTests.cs:34–50؛ story-capabilities.test.ts؛ story-api.test.ts.
نیاز: زبان از رجیستری واقعی، تاریخ/ساعت مطابق استاندارد جاری و تست مسیر واقعی از فروشنده تا نمایش عمومی، نه صرفاً شمارش route/validator.

### S13 — مقاومت در برابر کلیک تکراری و بازبینی قدیمی کامل نشده

ایجاد/افزودن شناسهٔ تازه می‌سازد؛ کلاینت قفل ارسال/شناسهٔ رخدادِ پایدار ندارد. VersionToken برمی‌گردد اما commandهای بررسی/ویرایش expectedVersion ندارند؛ EF concurrency فقط تداخل دو Save همزمان را می‌گیرد، نه فرم قدیمی اپراتور را که بعداً روی محتوای تازه تأیید می‌کند.
شاهد: StoryManagementScreen.tsx state و handlers؛ Story.cs:89، 325–338 و 423–427؛ StoryHttpModels.cs؛ StoryDbContext.cs:52.
نیاز: جلوگیری از ثبت تکراری با هویت پایدار و مقایسهٔ درخواست، و binding تأیید به محتوای دیده‌شده. این توصیه برنامه است، نه ادعای رخداد زندهٔ تکرار/نفوذ.

## اجزای قابل استفادهٔ مجدد

- جدول و آیکون مشاهده/منوی عملیات: admin-screens.tsx و design-system/app-data-grid/app-grid-row-actions.tsx.
- Dialog، فرم‌ها، Toast اصلی و استاندارد TOOBA-UX-001؛ بدون دست‌کاری جهانی سفارش‌ها.
- MediaLibraryDialog و Media-owned upload/validation، با مرز seller-scoped واقعی.
- IPartyLookup.GetDisplayNamesAsync / FindByIdAsync و IPartyAdminSellerReadGateway برای نام/وضعیت مالک؛ عدم join دیتابیس Party در Story.
- رجیستری زبان موجود در features/admin-languages/api/language-api.ts؛ فروشنده باید خواندن مجاز تنظیمات فعال را داشته باشد، نه دسترسی ادمین.
- Story Domain/Directory/CQRS/typed errors و endpointهای موجود؛ ساخت ماژول یا دو صفحهٔ مستقل تازه لازم نیست.

## مسیرهای اصلی برای بازیابی

- src/frontend/app/stories/management/StoryManagementScreen.tsx
- src/frontend/app/stories/management/story-capabilities.ts
- src/frontend/app/stories/story-api.ts
- src/frontend/app/admin/admin-screens.tsx
- src/frontend/app/storefront/stories/home-stories.tsx
- src/frontend/app/storefront/stories/story-modal.tsx
- src/backend/Modules/Story/Tooba.Story.Domain/Aggregates/{Story.cs,StoryItem.cs}
- src/backend/Modules/Story/Tooba.Story.Infrastructure/Directories/StoryDirectory.cs
- src/backend/Modules/Story/Tooba.Story.Infrastructure/Grid/{StoryAdminGridPolicies.cs,AdminStoryGridQueryEngine.cs}
- src/backend/Modules/Story/Tooba.Story.Application/Stories/{Models/StoryModels.cs,Presentation/StoryPresentationComposer.cs,Validators/StoryValidators.cs}
- src/backend/Modules/Story/Tooba.Story.Endpoints/{Admin/StoryAdminEndpoints.cs,Seller/StorySellerEndpoints.cs,Models/StoryHttpModels.cs,Storefront/StoryStorefrontEndpoints.cs}
- src/backend/Host/Tooba.Host.Tests/StoryFoundationTests.cs

گزارش همراه برنامهٔ STORY-MARKETPLACE-COMPLETION-PLAN.md خوانده شود. Implementation-State: NOT_ISSUED.
