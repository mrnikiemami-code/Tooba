namespace Tooba.Host.Storefront;

/// <summary>
/// مشخصهٔ برند نمایشی. برند در Tooba تحریری است و مالکیت فروشنده یا کمیسیون ندارد.
/// </summary>
/// <param name="Key">کلید داخلی نگاشت برند به محصول در همین دانه؛ شناسهٔ پایگاه داده نیست.</param>
/// <param name="Slug">درز slug برند برای مسیر عمومی landing برند.</param>
/// <param name="PersianName">نام نمایشی فارسی.</param>
/// <param name="LatinName">نام لاتین برای فهرست دوزبانه.</param>
internal sealed record StorefrontDemoBrandSpec(string Key, string Slug, string PersianName, string LatinName);

/// <summary>
/// مشخصهٔ محصول نمایشی. قیمت و موجودی در این مشخصه نیست چون به Product تعلق ندارد.
/// </summary>
/// <param name="Name">نام نمایشی فارسی محصول.</param>
/// <param name="BrandKey">کلید برند یا null وقتی محصول برند تحریری ندارد؛ برند جعلی ساخته نمی‌شود.</param>
internal sealed record StorefrontDemoProductSpec(string Name, string? BrandKey);

/// <summary>
/// مشخصهٔ ردهٔ فرزند نمایشی همراه محصولات آن.
/// </summary>
/// <param name="Name">نام نمایشی فارسی رده.</param>
/// <param name="Token">توکن قطعی برای ساخت slug و SKU؛ باید در کل ماتریس یکتا باشد.</param>
/// <param name="BasePrice">مبلغ پایهٔ قطعی به ریال که مبلغ Offer از آن مشتق می‌شود؛ خودِ رده قیمت ندارد.</param>
/// <param name="Products">محصولات این رده.</param>
internal sealed record StorefrontDemoChildSpec(
    string Name,
    string Token,
    decimal BasePrice,
    IReadOnlyList<StorefrontDemoProductSpec> Products);

/// <summary>
/// مشخصهٔ خانوادهٔ ردهٔ ریشه که در Mega Menu انتخاب می‌شود.
/// </summary>
/// <param name="Name">نام نمایشی فارسی ردهٔ ریشه.</param>
/// <param name="Children">رده‌های فرزند که عمق ناوبری را می‌سازند.</param>
internal sealed record StorefrontDemoFamilySpec(string Name, IReadOnlyList<StorefrontDemoChildSpec> Children);

/// <summary>
/// مشخصهٔ سازمان فروشندهٔ نمایشی برای نشان دادن تنوع Marketplace.
/// </summary>
/// <param name="DisplayName">نام نمایشی عمومی فروشنده.</param>
/// <param name="LegalName">نام حقوقی نمونه؛ ادعای ثبت واقعی نیست.</param>
internal sealed record StorefrontDemoSellerSpec(string DisplayName, string LegalName);

/// <summary>
/// مشخصهٔ محل نگهداری نمایشی ماژول Inventory.
/// </summary>
/// <param name="Code">کد یکتای محل.</param>
/// <param name="Name">نام نمایشی محل.</param>
internal sealed record StorefrontDemoLocationSpec(string Code, string Name);

/// <summary>
/// ماتریس قطعی دادهٔ نمایشی فروشگاه: هشت خانوادهٔ ریشه، سه ردهٔ فرزند برای هر خانواده و
/// سه محصول برای هر ردهٔ فرزند. هیچ مقداری از تصادف یا زمان اجرا مشتق نمی‌شود تا دانه تکرارپذیر بماند.
/// این ماتریس فقط دادهٔ توصیفی و مبلغ پایهٔ نمایشی است؛ حقیقت قیمت در Pricing و حقیقت موجودی در Inventory نوشته می‌شود.
/// </summary>
internal static class StorefrontDemoCatalogMatrix
{
    /// <summary>
    /// برندهای نمایشی منتشرشده که سطوح عمومی برند را پرمی‌کنند.
    /// </summary>
    internal static readonly IReadOnlyList<StorefrontDemoBrandSpec> Brands =
    [
        new("xiaomi", "xiaomi", "شیائومی", "Xiaomi"),
        new("samsung", "samsung", "سامسونگ", "Samsung"),
        new("apple", "apple", "اپل", "Apple"),
        new("lenovo", "lenovo", "لنوو", "Lenovo"),
        new("asus", "asus", "ایسوس", "ASUS"),
        new("bosch", "bosch", "بوش", "Bosch"),
        new("philips", "philips", "فیلیپس", "Philips"),
        new("jbl", "jbl", "جی‌بی‌ال", "JBL"),
    ];

    /// <summary>
    /// سازمان‌های فروشندهٔ نمایشی که Offerها بین آن‌ها به‌صورت قطعی توزیع می‌شود.
    /// </summary>
    internal static readonly IReadOnlyList<StorefrontDemoSellerSpec> Sellers =
    [
        new("فروشگاه توبا مارکت", "Tooba Market Demo Legal"),
        new("تجارت الکترونیک پارس", "Pars E-Commerce Demo Legal"),
        new("خانه و کالای مهر", "Mehr Home Goods Demo Legal"),
    ];

    /// <summary>
    /// محل‌های نگهداری نمایشی ماژول Inventory.
    /// </summary>
    internal static readonly IReadOnlyList<StorefrontDemoLocationSpec> Locations =
    [
        new("WH-DEMO-THR", "انبار نمایشی تهران"),
        new("WH-DEMO-MSH", "انبار نمایشی مشهد"),
    ];

    /// <summary>
    /// خانواده‌های ردهٔ الزامی و عمق فرزند آن‌ها.
    /// </summary>
    internal static readonly IReadOnlyList<StorefrontDemoFamilySpec> Families =
    [
        new("محصولات دیجیتال",
        [
            new("گوشی موبایل", "mobile", 24_900_000m,
            [
                new("گوشی موبایل سامسونگ Galaxy A55", "samsung"),
                new("گوشی موبایل شیائومی Redmi Note 13", "xiaomi"),
                new("گوشی موبایل اپل iPhone 13", "apple"),
            ]),
            new("لپ‌تاپ", "laptop", 41_500_000m,
            [
                new("لپ‌تاپ لنوو IdeaPad Slim 3", "lenovo"),
                new("لپ‌تاپ ایسوس VivoBook 15", "asus"),
                new("لپ‌تاپ اپل MacBook Air M2", "apple"),
            ]),
            new("هدفون و صوتی", "audio", 3_200_000m,
            [
                new("هدفون بی‌سیم جی‌بی‌ال Tune 720BT", "jbl"),
                new("اسپیکر بلوتوثی شیائومی Mi Portable", "xiaomi"),
                new("هندزفری سیمی فیلیپس TAE1105", "philips"),
            ]),
        ]),
        new("لوازم خانگی",
        [
            new("نوشیدنی‌ساز", "beverage", 4_800_000m,
            [
                new("چای‌ساز فیلیپس HD7301", "philips"),
                new("قهوه‌ساز بوش TKA3A031", "bosch"),
                new("آب‌مرکبات‌گیری شیائومی Mi Juicer", "xiaomi"),
            ]),
            new("پخت‌وپز", "cooking", 7_600_000m,
            [
                new("سرخ‌کن بدون روغن فیلیپس Airfryer", "philips"),
                new("مایکروویو سامسونگ MG23", "samsung"),
                new("غذاساز بوش MCM3501", "bosch"),
            ]),
            new("نظافت", "cleaning", 9_300_000m,
            [
                new("جاروبرقی بوش GL30", "bosch"),
                new("جاروشارژی شیائومی G10", "xiaomi"),
                new("بخارشوی فیلیپس STE3170", "philips"),
            ]),
        ]),
        new("مد و پوشاک",
        [
            new("پوشاک مردانه", "menswear", 1_850_000m,
            [
                new("پیراهن مردانه لینن آستین بلند", null),
                new("شلوار جین مردانه راسته", null),
                new("سویشرت مردانه کلاه‌دار", null),
            ]),
            new("پوشاک زنانه", "womenswear", 2_150_000m,
            [
                new("مانتو زنانه کتان جلوبسته", null),
                new("شومیز زنانه آستین بلند", null),
                new("شلوار زنانه پارچه‌ای دم‌پا", null),
            ]),
            new("کفش", "footwear", 2_950_000m,
            [
                new("کفش ورزشی مردانه رانینگ", null),
                new("کتانی زنانه روزمره", null),
                new("نیم‌بوت چرم مردانه", null),
            ]),
        ]),
        new("زیبایی و سلامت",
        [
            new("مراقبت پوست", "skincare", 780_000m,
            [
                new("کرم آبرسان صورت", null),
                new("سرم ویتامین C", null),
                new("کرم ضدآفتاب SPF50", null),
            ]),
            new("آرایشی", "makeup", 650_000m,
            [
                new("رژ لب مات", null),
                new("ریمل حجم‌دهنده", null),
                new("کرم پودر پوشش‌دهنده", null),
            ]),
            new("بهداشت شخصی", "personal-care", 1_450_000m,
            [
                new("ریش‌تراش فیلیپس S3231", "philips"),
                new("مسواک برقی فیلیپس Sonicare", "philips"),
                new("سشوار حرفه‌ای یون‌ساز", null),
            ]),
        ]),
        new("خانه و آشپزخانه",
        [
            new("ظروف پخت‌وپز", "cookware", 3_400_000m,
            [
                new("سرویس قابلمه گرانیتی ۹ پارچه", null),
                new("تابه نچسب ۲۸ سانتی‌متری", null),
                new("زودپز استیل ۶ لیتری", null),
            ]),
            new("سرو و پذیرایی", "tableware", 2_600_000m,
            [
                new("سرویس غذاخوری چینی ۲۸ پارچه", null),
                new("ست لیوان شش‌عددی شیشه‌ای", null),
                new("سینی سرو چوبی دست‌ساز", null),
            ]),
            new("دکوراسیون", "decor", 1_900_000m,
            [
                new("تابلو دیواری مدرن", null),
                new("گلدان سرامیکی دست‌ساز", null),
                new("آینه دیواری قدی", null),
            ]),
        ]),
        new("خودرو و موتور",
        [
            new("لوازم خودرو", "car-accessory", 2_300_000m,
            [
                new("تیغه برف‌پاک‌کن بوش Aerotwin", "bosch"),
                new("شارژر فندکی سریع شیائومی", "xiaomi"),
                new("کفپوش سه‌بعدی خودرو", null),
            ]),
            new("قطعات مصرفی", "car-part", 1_750_000m,
            [
                new("فیلتر روغن بوش", "bosch"),
                new("لنت ترمز جلو بوش", "bosch"),
                new("شمع موتور بوش", "bosch"),
            ]),
            new("لوازم موتورسیکلت", "moto", 1_250_000m,
            [
                new("کلاه کاسکت فول‌فیس", null),
                new("دستکش موتورسواری زمستانی", null),
                new("چادر محافظ موتورسیکلت", null),
            ]),
        ]),
        new("ورزش و سفر",
        [
            new("ورزش خانگی", "fitness", 5_400_000m,
            [
                new("تردمیل خانگی تاشو", null),
                new("دوچرخه ثابت اسپینینگ", null),
                new("ست دمبل ۲۰ کیلوگرمی", null),
            ]),
            new("کمپینگ", "camping", 3_100_000m,
            [
                new("چادر مسافرتی چهار نفره", null),
                new("کیسه خواب کوهنوردی", null),
                new("اجاق گاز سفری", null),
            ]),
            new("کیف و چمدان", "luggage", 2_700_000m,
            [
                new("چمدان چرخ‌دار سایز بزرگ", null),
                new("کوله‌پشتی لپ‌تاپ ۱۵ اینچی", null),
                new("کیف دوشی چرم طبیعی", null),
            ]),
        ]),
        new("کتاب، هنر و سرگرمی",
        [
            new("کتاب", "book", 420_000m,
            [
                new("کتاب رمان ایرانی معاصر", null),
                new("کتاب تاریخ ایران باستان", null),
                new("کتاب مهارت‌های مدیریت زمان", null),
            ]),
            new("لوازم تحریر", "stationery", 350_000m,
            [
                new("دفتر یادداشت سیمی ۱۰۰ برگ", null),
                new("ست خودکار و روان‌نویس", null),
                new("پک ماژیک رنگی ۱۲ رنگ", null),
            ]),
            new("بازی و سرگرمی", "game", 1_600_000m,
            [
                new("دسته بازی بی‌سیم", null),
                new("پازل ۱۰۰۰ تکه", null),
                new("بازی فکری رومیزی خانوادگی", null),
            ]),
        ]),
    ];

    /// <summary>
    /// تعداد ردهٔ ریشهٔ ماتریس.
    /// </summary>
    internal static int TopLevelCategoryCount => Families.Count;

    /// <summary>
    /// تعداد ردهٔ فرزند ماتریس.
    /// </summary>
    internal static int ChildCategoryCount => Families.Sum(family => family.Children.Count);

    /// <summary>
    /// تعداد محصول نمایشی ماتریس.
    /// </summary>
    internal static int ProductCount => Families.Sum(family => family.Children.Sum(child => child.Products.Count));

    /// <summary>
    /// تعداد Offer قطعی ماتریس: Offer پایه، فروشندهٔ دوم رده‌ها و سه Offer برای گونه‌های نمایشی اضافه.
    /// </summary>
    internal static int ExpectedOfferCount => ProductCount + ChildCategoryCount + 3;
}
