/**
 * Content contextual help — reusable keys + FA/EN copy (plain human language).
 * Used by Help affordances and the central /admin/content/help page.
 */

export type ContentHelpLocale = "fa" | "en";

export type ContentHelpKey =
  | "language"
  | "draftPublished"
  | "author"
  | "category"
  | "tags"
  | "featuredImage"
  | "galleryMedia"
  | "seoSocial"
  | "readiness"
  | "preview"
  | "publishSchedule"
  | "unpublishRepublish"
  | "history"
  | "comments"
  | "homeFeature"
  | "inlineImage"
  | "shareImage";

export type ContentHelpTopic = {
  key: ContentHelpKey;
  titleFa: string;
  titleEn: string;
  summaryFa: string;
  summaryEn: string;
  whatFa: string;
  whatEn: string;
  whyFa: string;
  whyEn: string;
  doFa: string;
  doEn: string;
};

export const CONTENT_HELP_TOPICS: ContentHelpTopic[] = [
  {
    key: "language",
    titleFa: "زبان مقاله",
    titleEn: "Article language",
    summaryFa: "هر مقاله فقط به یک زبان نوشته می‌شود و نسخهٔ زبان دیگر مقالهٔ جداست.",
    summaryEn: "Each article is written in one language; another language is a separate article.",
    whatFa: "زبان مشخص می‌کند متن، دسته، برچسب و مسیر عمومی مقاله به کدام مخاطب می‌رسد.",
    whatEn: "Language controls which audience sees the text, category, tags, and public path.",
    whyFa: "اگر زبان اشتباه باشد، مقاله در فهرست یا صفحهٔ عمومی زبان دیگر دیده نمی‌شود.",
    whyEn: "A wrong language hides the article from the intended public list or page.",
    doFa: "قبل از نوشتن محتوا زبان درست را انتخاب کنید. پس از ذخیرهٔ معنادار، تغییر زبان ممکن است قفل شود.",
    doEn: "Pick the correct language before writing. After meaningful content exists, language may lock.",
  },
  {
    key: "draftPublished",
    titleFa: "پیش‌نویس و منتشرشده",
    titleEn: "Draft vs published",
    summaryFa: "پیش‌نویس فقط در پنل دیده می‌شود؛ منتشرشده در زمان مناسب برای عموم قابل مشاهده است.",
    summaryEn: "Drafts stay in admin; published articles become public when the schedule allows.",
    whatFa: "وضعیت انتشار نشان می‌دهد آیا مقاله هنوز در حال کار است یا برای مخاطب آماده شده.",
    whatEn: "Publication status shows whether the article is still in progress or ready for readers.",
    whyFa: "انتشار زودهنگام مطلب ناقص را عمومی می‌کند؛ ماندن در پیش‌نویس مانع نمایش عمومی است.",
    whyEn: "Publishing too early exposes incomplete work; staying draft keeps it off the public site.",
    doFa: "تا آماده شدن محتوا در پیش‌نویس بمانید، سپس از تب انتشار اقدام کنید.",
    doEn: "Stay in draft until ready, then use the Publication tab.",
  },
  {
    key: "author",
    titleFa: "نویسنده",
    titleEn: "Author",
    summaryFa: "نویسندهٔ فعال از فهرست نویسندگان به مقاله وصل می‌شود و نامش در صفحه نمایش داده می‌شود.",
    summaryEn: "An active author from the author list is linked and shown on the article page.",
    whatFa: "نویسنده هویت تحریری مقاله است؛ نام نمایشی از پروفایل نویسنده همگام می‌شود.",
    whatEn: "Author is the editorial identity; display name syncs from the author profile.",
    whyFa: "بدون نویسنده، انتشار مجاز نیست و اعتبار مطلب برای خواننده مشخص نمی‌شود.",
    whyEn: "Without an author, publishing is blocked and readers lack clear attribution.",
    doFa: "از فهرست نویسندگان فعال انتخاب کنید؛ در صورت نیاز نویسنده را از بخش نویسندگان بسازید.",
    doEn: "Choose an active author; create one under Authors if needed.",
  },
  {
    key: "category",
    titleFa: "دسته‌بندی",
    titleEn: "Category",
    summaryFa: "دسته مقاله را در ساختار دو سطح محتوا جای می‌دهد و باید هم‌زبان مقاله باشد.",
    summaryEn: "Category places the article in the two-level content tree and must match the article language.",
    whatFa: "دسته برای مرور و فیلتر عمومی مقالات استفاده می‌شود.",
    whatEn: "Categories support browsing and filtering published articles.",
    whyFa: "دستهٔ هم‌زبان پیدا شدن مطلب را آسان می‌کند و از درهم‌ریختگی زبان‌ها جلوگیری می‌کند.",
    whyEn: "Matching language keeps navigation clean and discoverable.",
    doFa: "دستهٔ مناسب همان زبان را انتخاب کنید؛ در صورت نیاز از مدیریت دسته‌ها بسازید.",
    doEn: "Pick a same-language category; create one under Categories if needed.",
  },
  {
    key: "tags",
    titleFa: "برچسب‌ها",
    titleEn: "Tags",
    summaryFa: "برچسب‌ها موضوعات فرعی مقاله را مشخص می‌کنند و به زبان مقاله وابسته‌اند.",
    summaryEn: "Tags describe secondary topics and are language-scoped.",
    whatFa: "برچسب‌ها برای جستجو و ارتباط بین مقالات مرتبط به کار می‌روند.",
    whatEn: "Tags help search and relate similar articles.",
    whyFa: "برچسب‌های واضح پیدا کردن مطلب را آسان‌تر می‌کند بدون شلوغ کردن دسته‌ها.",
    whyEn: "Clear tags improve findability without overloading categories.",
    doFa: "چند برچسب کوتاه و مرتبط اضافه کنید؛ از تکرار بی‌مورد پرهیز کنید.",
    doEn: "Add a few short related tags; avoid unnecessary duplicates.",
  },
  {
    key: "featuredImage",
    titleFa: "تصویر شاخص",
    titleEn: "Featured image",
    summaryFa: "تصویر اصلی کارت و بالای مقاله از کتابخانهٔ رسانهٔ توبا انتخاب می‌شود.",
    summaryEn: "The main card/header image is picked from Tooba Media Library.",
    whatFa: "تصویر شاخص چهرهٔ بصری مقاله در فهرست و صفحه است.",
    whatEn: "The featured image is the visual face of the article in lists and on the page.",
    whyFa: "بدون تصویر شاخص، مقاله در فهرست‌ها ضعیف‌تر دیده می‌شود.",
    whyEn: "Without a featured image, list presentation looks weaker.",
    doFa: "از کتابخانه رسانه تصویر مناسب انتخاب کنید. حذف از مقاله فایل اصلی را پاک نمی‌کند.",
    doEn: "Choose a suitable image from Media Library. Removing it from the article does not delete the file.",
  },
  {
    key: "galleryMedia",
    titleFa: "گالری و کتابخانه رسانه",
    titleEn: "Gallery and media library",
    summaryFa: "گالری تصاویر تکمیلی مقاله است؛ همه از همان کتابخانهٔ رسانهٔ مشترک می‌آیند.",
    summaryEn: "The gallery holds extra article images; all come from the shared Media Library.",
    whatFa: "کتابخانه رسانه محل نگهداری مشترک فایل‌هاست؛ مقاله فقط به آن‌ها ارجاع می‌دهد.",
    whatEn: "Media Library is shared storage; the article only references assets.",
    whyFa: "این جداسازی از حذف تصادفی فایل‌های مشترک جلوگیری می‌کند.",
    whyEn: "Separation prevents accidental deletion of shared files.",
    doFa: "گالری را در تب رسانه مدیریت کنید. حذف از گالری فقط ارجاع مقاله را برمی‌دارد.",
    doEn: "Manage the gallery in the Media tab. Removing from gallery only drops the article link.",
  },
  {
    key: "inlineImage",
    titleFa: "تصویر داخل متن",
    titleEn: "Inline body image",
    summaryFa: "تصاویر داخل بدنه از همان کتابخانهٔ رسانه داخل ویرایشگر درج می‌شوند.",
    summaryEn: "Body images are inserted from the same Media Library inside the editor.",
    whatFa: "تصویر داخل متن بخشی از محتوای تحریری است، نه تصویر شاخص.",
    whatEn: "Inline images are editorial body content, not the featured image.",
    whyFa: "استفاده از کتابخانهٔ مشترک کیفیت و کنترل فایل‌ها را یکدست نگه می‌دارد.",
    whyEn: "Shared library keeps file quality and control consistent.",
    doFa: "در ویرایشگر از دکمهٔ درج تصویر کتابخانه استفاده کنید؛ آپلود موازی جدا نسازید.",
    doEn: "Use the editor’s media insert; do not invent a parallel upload path.",
  },
  {
    key: "shareImage",
    titleFa: "تصویر اشتراک‌گذاری",
    titleEn: "Share image",
    summaryFa: "تصویری که ممکن است هنگام اشتراک لینک در شبکه‌های اجتماعی نمایش داده شود.",
    summaryEn: "The image that may appear when the article link is shared on social networks.",
    whatFa: "می‌تواند همان تصویر شاخص باشد یا تصویر جداگانه برای اشتراک.",
    whatEn: "It can reuse the featured image or use a dedicated share image.",
    whyFa: "تصویر مناسب اشتراک، ظاهر لینک را در پیام‌رسان‌ها و شبکه‌ها بهتر می‌کند (بدون تضمین رفتار هر پلتفرم).",
    whyEn: "A good share image improves link previews (without promising exact third-party behavior).",
    doFa: "در تب جستجو و اشتراک، تصویر شاخص را استفاده کنید یا تصویر جدا انتخاب کنید.",
    doEn: "In Search & share, reuse the featured image or pick a dedicated one.",
  },
  {
    key: "seoSocial",
    titleFa: "جستجو و اشتراک‌گذاری",
    titleEn: "Search and sharing",
    summaryFa: "عنوان و توضیح کوتاه برای نتایج جستجو و پیش‌نمایش لینک؛ تصویر اشتراک جداگانه قابل تنظیم است.",
    summaryEn: "Short title and description for search results and link previews; share image is configurable.",
    whatFa: "این فیلدها به موتورهای جستجو و پیش‌نمایش لینک کمک می‌کنند، نه متن اصلی مقاله.",
    whatEn: "These fields help search engines and link previews, not the main article body.",
    whyFa: "عنوان و توضیح واضح باعث می‌شود خواننده قبل از کلیک بداند مطلب درباره چیست.",
    whyEn: "Clear title and description help readers know the topic before clicking.",
    doFa: "عنوان کوتاه جذاب و توضیح یک‌دو جمله‌ای بنویسید؛ تصویر اشتراک را بررسی کنید.",
    doEn: "Write a short compelling title and a one–two sentence description; review the share image.",
  },
  {
    key: "readiness",
    titleFa: "آمادگی انتشار",
    titleEn: "Publish readiness",
    summaryFa: "چک‌لیست موارد الزامی و پیشنهادی قبل از انتشار.",
    summaryEn: "Checklist of required and recommended items before publishing.",
    whatFa: "آمادگی نشان می‌دهد چه چیزی هنوز برای انتشار کم است.",
    whatEn: "Readiness shows what is still missing before publish.",
    whyFa: "از انتشار ناقص جلوگیری می‌کند و مسیر تکمیل را روشن می‌سازد.",
    whyEn: "It blocks incomplete publish and clarifies what to finish.",
    doFa: "موارد قرمز/الزامی را از طریق میانبر به تب مربوطه کامل کنید؛ موارد پیشنهادی کیفیت را بالا می‌برند.",
    doEn: "Complete required items via shortcuts to each tab; recommended items improve quality.",
  },
  {
    key: "preview",
    titleFa: "پیش‌نمایش",
    titleEn: "Preview",
    summaryFa: "نمای نزدیک به صفحهٔ عمومی بدون عمومی‌کردن پیش‌نویس.",
    summaryEn: "A near-public view without making the draft public.",
    whatFa: "پیش‌نمایش ظاهر عنوان، تصویر و بدنه را قبل از انتشار نشان می‌دهد.",
    whatEn: "Preview shows title, image, and body appearance before publish.",
    whyFa: "اشکالات چیدمان و متن را زودتر می‌بینید.",
    whyEn: "You catch layout and copy issues earlier.",
    doFa: "پس از ذخیرهٔ تغییرات مهم، پیش‌نمایش را باز کنید.",
    doEn: "After saving important changes, open Preview.",
  },
  {
    key: "publishSchedule",
    titleFa: "انتشار فوری و زمان‌بندی",
    titleEn: "Publish now vs schedule",
    summaryFa: "اگر زمان انتشار گذشته/اکنون باشد فوری عمومی می‌شود؛ اگر آینده باشد تا موعد مخفی می‌ماند.",
    summaryEn: "Past/now schedule publishes immediately; a future time stays hidden until then.",
    whatFa: "زمان انتشار مشخص می‌کند مقاله از چه لحظه‌ای در مسیر عمومی دیده شود.",
    whatEn: "Publish time controls when the article becomes publicly visible.",
    whyFa: "زمان‌بندی به برنامه‌ریزی تحریری بدون فراموشی انتشار کمک می‌کند.",
    whyEn: "Scheduling supports editorial planning without forgetting to publish.",
    doFa: "زمان را در تقویم جلالی/میلادی تنظیم کنید سپس انتشار را بزنید.",
    doEn: "Set the date (Jalali/Gregorian as shown), then publish.",
  },
  {
    key: "unpublishRepublish",
    titleFa: "لغو انتشار و انتشار مجدد",
    titleEn: "Unpublish and republish",
    summaryFa: "لغو انتشار مطلب را از عموم برمی‌دارد؛ انتشار مجدد دوباره آن را در دسترس می‌کند.",
    summaryEn: "Unpublish removes public visibility; republish restores it.",
    whatFa: "این کارها وضعیت انتشار را عوض می‌کنند بدون حذف تاریخچه.",
    whatEn: "These actions change publication state without deleting history.",
    whyFa: "برای اصلاح اضطراری یا بازگشت موقت از دید عموم لازم است.",
    whyEn: "Useful for urgent fixes or temporary removal from public view.",
    doFa: "از دکمهٔ انتشار در سرصفحه یا تب انتشار استفاده کنید و تأیید را بخوانید.",
    doEn: "Use the header or Publication publish control and read the confirmation.",
  },
  {
    key: "history",
    titleFa: "تاریخچه",
    titleEn: "History",
    summaryFa: "رویدادهای مهم چرخهٔ عمر مقاله مثل ایجاد، انتشار و لغو انتشار.",
    summaryEn: "Important lifecycle events such as create, publish, and unpublish.",
    whatFa: "تاریخچه یک خط زمانی خوانا از تغییرات کلیدی است.",
    whatEn: "History is a readable timeline of key changes.",
    whyFa: "شفافیت برای تیم تحریر و پیگیری تصمیم‌های انتشار.",
    whyEn: "Transparency for the editorial team and publish decisions.",
    doFa: "برای فهم وضعیت فعلی، تب تاریخچه را مرور کنید؛ رویدادها حذف نمی‌شوند.",
    doEn: "Review the History tab for current context; events are not deleted.",
  },
  {
    key: "comments",
    titleFa: "نظرات و تعدیل",
    titleEn: "Comments moderation",
    summaryFa: "نظرات مقاله را بررسی، تأیید، رد یا پنهان کنید بدون پاک کردن تاریخچه.",
    summaryEn: "Review, approve, reject, or hide article comments without deleting history.",
    whatFa: "نظرات در انتظار باید قبل از نمایش عمومی (در صورت وجود سطح عمومی) تأیید شوند.",
    whatEn: "Pending comments must be approved before any public display.",
    whyFa: "تعدیل از انتشار محتوای نامناسب جلوگیری می‌کند.",
    whyEn: "Moderation prevents inappropriate content from going public.",
    doFa: "در تب نظرات فیلتر کنید، متن را بخوانید و اقدام مناسب را بزنید.",
    doEn: "In the Comments tab, filter, read, and take the right action.",
  },
  {
    key: "homeFeature",
    titleFa: "نمایش در صفحه اصلی",
    titleEn: "Home page articles section",
    summaryFa: "اگر فعال باشد، مقاله می‌تواند در بخش مقالات صفحه اصلی دیده شود (طبق قوانین فعلی فروشگاه).",
    summaryEn: "When enabled, the article may appear in the home page articles section (per current storefront rules).",
    whatFa: "این گزینه اولویت نمایش در بخش مقالات خانه را علامت می‌زند؛ رتبه‌بندی کامل فروشگاه را عوض نمی‌کند.",
    whatEn: "This flag marks home-section eligibility; it does not rewrite full storefront ranking.",
    whyFa: "کمک می‌کند مطالب مهم‌تر در خانه دیده شوند بدون جابه‌جایی منطق انتخاب.",
    whyEn: "Helps highlight important pieces on home without changing selection semantics.",
    doFa: "فقط برای مقالات آماده و مرتبط با خانه فعال کنید.",
    doEn: "Enable only for ready, home-relevant articles.",
  },
];

export function getContentHelpTopic(key: ContentHelpKey): ContentHelpTopic | undefined {
  return CONTENT_HELP_TOPICS.find((t) => t.key === key);
}

export function contentHelpTitle(topic: ContentHelpTopic, locale: ContentHelpLocale): string {
  return locale === "en" ? topic.titleEn : topic.titleFa;
}

export function contentHelpSummary(topic: ContentHelpTopic, locale: ContentHelpLocale): string {
  return locale === "en" ? topic.summaryEn : topic.summaryFa;
}

export const CONTENT_HELP_PAGE_HREF = "/admin/content/help";
