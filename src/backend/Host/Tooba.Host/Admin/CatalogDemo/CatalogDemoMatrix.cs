using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>نام دو‌زبانهٔ پایدار.</summary>
public sealed record CatalogDemoLocalizedName(string Fa, string En);

/// <summary>گره L3 برگ با دامنهٔ ویژگی و پرچم‌های schema.</summary>
public sealed record CatalogDemoL3Spec(
    string Key,
    CatalogDemoLocalizedName Name,
    string AttributeDomain,
    IReadOnlyList<CatalogDemoBindingSpec> Bindings,
    bool SeedFacets,
    IReadOnlyList<string> TagKeys);

/// <summary>گره L2 با ۱–۴ فرزند L3.</summary>
public sealed record CatalogDemoL2Spec(
    string Key,
    CatalogDemoLocalizedName Name,
    IReadOnlyList<CatalogDemoL3Spec> Children);

/// <summary>ریشهٔ L1 با حداقل یک L2.</summary>
public sealed record CatalogDemoL1Spec(
    string Key,
    CatalogDemoLocalizedName Name,
    string DescriptionFa,
    string DescriptionEn,
    IReadOnlyList<CatalogDemoL2Spec> Children,
    bool MegaMenuFeatured,
    IReadOnlyList<string> TagKeys);

/// <summary>تعریف ویژگی دانه با گزینه‌های اختیاری.</summary>
public sealed record CatalogDemoAttributeSpec(
    string CodeSuffix,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxis,
    CatalogDemoLocalizedName Name,
    string? Unit,
    bool IsFilterable,
    bool IsComparable,
    IReadOnlyList<(string Code, string Fa, string En)> Options);

/// <summary>پیوند schema محلی روی L3.</summary>
public sealed record CatalogDemoBindingSpec(
    string AttributeCodeSuffix,
    int DisplayOrder,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable);

/// <summary>برند دانه.</summary>
public sealed record CatalogDemoBrandSpec(string Key, CatalogDemoLocalizedName Name);

/// <summary>برچسب دانه.</summary>
public sealed record CatalogDemoTagSpec(string Key, CatalogDemoLocalizedName Name);

/// <summary>
/// ماتریس قطعی دانهٔ Catalog Demo — درخت نامتقارن، برند، برچسب، ویژگی و schema.
/// </summary>
public static class CatalogDemoMatrix
{
    /// <summary>locale فارسی استاندارد Admin.</summary>
    public const string LocaleFa = "fa-IR";

    /// <summary>locale انگلیسی پایدار.</summary>
    public const string LocaleEn = "en";

    /// <summary>دقیقاً ۱۵ ریشهٔ L1.</summary>
    public static IReadOnlyList<CatalogDemoL1Spec> Roots { get; } = BuildRoots();

    /// <summary>حداقل ۲۰ برند.</summary>
    public static IReadOnlyList<CatalogDemoBrandSpec> Brands { get; } =
    [
        new("samsung", new("سامسونگ", "Samsung")),
        new("apple", new("اپل", "Apple")),
        new("xiaomi", new("شیائومی", "Xiaomi")),
        new("huawei", new("هواوی", "Huawei")),
        new("sony", new("سونی", "Sony")),
        new("lg", new("ال‌جی", "LG")),
        new("asus", new("ایسوس", "ASUS")),
        new("lenovo", new("لنوو", "Lenovo")),
        new("hp", new("اچ‌پی", "HP")),
        new("dell", new("دل", "Dell")),
        new("bosch", new("بوش", "Bosch")),
        new("philips", new("فیلیپس", "Philips")),
        new("jbl", new("جی‌بی‌ال", "JBL")),
        new("nike", new("نایک", "Nike")),
        new("adidas", new("آدیداس", "Adidas")),
        new("zara", new("زارا", "Zara")),
        new("loreal", new("لورآل", "L'Oréal")),
        new("panasonic", new("پاناسونیک", "Panasonic")),
        new("canon", new("کانن", "Canon")),
        new("garmin", new("گارمین", "Garmin")),
        new("kitchenaid", new("کیچن‌اید", "KitchenAid")),
        new("nestle", new("نستله", "Nestlé")),
    ];

    /// <summary>۳۰–۵۰ برچسب مفید.</summary>
    public static IReadOnlyList<CatalogDemoTagSpec> Tags { get; } =
    [
        new("budget", new("اقتصادی", "Budget")),
        new("pro", new("حرفه‌ای", "Professional")),
        new("lightweight", new("سبک", "Lightweight")),
        new("travel", new("مناسب سفر", "Travel-friendly")),
        new("flagship", new("پرچمدار", "Flagship")),
        new("efficient", new("کم‌مصرف", "Energy efficient")),
        new("waterproof", new("ضدآب", "Waterproof")),
        new("minimal", new("مینیمال", "Minimal")),
        new("gift", new("هدیه", "Gift")),
        new("kids", new("مناسب کودک", "Kids")),
        new("organic", new("ارگانیک", "Organic")),
        new("sugar-free", new("بدون قند", "Sugar-free")),
        new("gaming", new("گیمینگ", "Gaming")),
        new("office", new("اداری", "Office")),
        new("student", new("دانشجویی", "Student")),
        new("everyday", new("روزمره", "Everyday")),
        new("winter", new("زمستانی", "Winter")),
        new("summer", new("تابستانی", "Summer")),
        new("new", new("جدید", "New")),
        new("popular", new("محبوب", "Popular")),
        new("premium", new("پرمیوم", "Premium")),
        new("compact", new("جمع‌وجور", "Compact")),
        new("durable", new("بادوام", "Durable")),
        new("eco", new("دوستدار محیط زیست", "Eco")),
        new("wireless", new("بی‌سیم", "Wireless")),
        new("fast-charge", new("شارژ سریع", "Fast charge")),
        new("portable", new("قابل حمل", "Portable")),
        new("family", new("خانوادگی", "Family")),
        new("beginner", new("مبتدی", "Beginner")),
        new("advanced", new("پیشرفته", "Advanced")),
        new("outdoor", new("فضای باز", "Outdoor")),
        new("indoor", new("فضای بسته", "Indoor")),
        new("sale", new("پرفروش", "Bestseller")),
        new("limited", new("محدود", "Limited")),
        new("classic", new("کلاسیک", "Classic")),
        new("modern", new("مدرن", "Modern")),
    ];

    /// <summary>کتابخانهٔ ویژگی بر اساس دامنه.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<CatalogDemoAttributeSpec>> AttributesByDomain { get; } =
        new Dictionary<string, IReadOnlyList<CatalogDemoAttributeSpec>>(StringComparer.Ordinal)
        {
            ["mobile"] =
            [
                EnumAttr("color", "رنگ", "Color", true, true, false,
                    ("black", "مشکی", "Black"), ("white", "سفید", "White"), ("blue", "آبی", "Blue"), ("gold", "طلایی", "Gold")),
                EnumAttr("storage", "حافظه داخلی", "Internal storage", true, true, true,
                    ("64", "۶۴ گیگ", "64 GB"), ("128", "۱۲۸ گیگ", "128 GB"), ("256", "۲۵۶ گیگ", "256 GB"), ("512", "۵۱۲ گیگ", "512 GB")),
                EnumAttr("ram", "رم", "RAM", false, true, true,
                    ("4", "۴ گیگ", "4 GB"), ("6", "۶ گیگ", "6 GB"), ("8", "۸ گیگ", "8 GB"), ("12", "۱۲ گیگ", "12 GB")),
                NumAttr("screen_size", "اندازه صفحه", "Screen size", "inch", true, true),
                EnumAttr("panel", "نوع پنل", "Panel type", false, true, false,
                    ("oled", "OLED", "OLED"), ("lcd", "LCD", "LCD"), ("amoled", "AMOLED", "AMOLED")),
                NumAttr("battery", "ظرفیت باتری", "Battery capacity", "mAh", true, true),
                BoolAttr("five_g", "5G", "5G", true, false),
                NumAttr("weight", "وزن", "Weight", "g", false, true),
            ],
            ["laptop"] =
            [
                EnumAttr("cpu", "پردازنده", "Processor", false, true, true,
                    ("i5", "Core i5", "Core i5"), ("i7", "Core i7", "Core i7"), ("r5", "Ryzen 5", "Ryzen 5"), ("r7", "Ryzen 7", "Ryzen 7")),
                EnumAttr("ram", "رم", "RAM", true, true, true,
                    ("8", "۸ گیگ", "8 GB"), ("16", "۱۶ گیگ", "16 GB"), ("32", "۳۲ گیگ", "32 GB")),
                EnumAttr("storage", "حافظه", "Storage", true, true, true,
                    ("256", "۲۵۶ گیگ", "256 GB"), ("512", "۵۱۲ گیگ", "512 GB"), ("1024", "۱ ترابایت", "1 TB")),
                EnumAttr("storage_type", "نوع حافظه", "Storage type", false, true, false,
                    ("ssd", "SSD", "SSD"), ("hdd", "HDD", "HDD")),
                NumAttr("display", "اندازه نمایشگر", "Display size", "inch", true, true),
                EnumAttr("panel", "نوع پنل", "Panel type", false, true, false,
                    ("ips", "IPS", "IPS"), ("oled", "OLED", "OLED")),
                TextAttr("gpu", "GPU", "GPU", false, false),
                NumAttr("weight", "وزن", "Weight", "kg", false, true),
                EnumAttr("os", "سیستم عامل", "Operating system", false, true, false,
                    ("win11", "ویندوز ۱۱", "Windows 11"), ("macos", "macOS", "macOS"), ("chrome", "ChromeOS", "ChromeOS")),
                EnumAttr("color", "رنگ", "Color", true, true, false,
                    ("silver", "نقره‌ای", "Silver"), ("gray", "خاکستری", "Gray"), ("black", "مشکی", "Black")),
            ],
            ["clothing"] =
            [
                EnumAttr("material", "جنس", "Material", false, true, false,
                    ("cotton", "پنبه", "Cotton"), ("poly", "پلی‌استر", "Polyester"), ("wool", "پشم", "Wool")),
                EnumAttr("color", "رنگ", "Color", true, true, false,
                    ("black", "مشکی", "Black"), ("navy", "سرمه‌ای", "Navy"), ("beige", "بژ", "Beige"), ("red", "قرمز", "Red")),
                EnumAttr("size", "سایز", "Size", true, true, false,
                    ("s", "S", "S"), ("m", "M", "M"), ("l", "L", "L"), ("xl", "XL", "XL")),
                EnumAttr("season", "فصل", "Season", false, true, false,
                    ("spring", "بهار", "Spring"), ("summer", "تابستان", "Summer"), ("fall", "پاییز", "Fall"), ("winter", "زمستان", "Winter")),
                EnumAttr("collar", "نوع یقه", "Collar type", false, false, false,
                    ("crew", "گرد", "Crew"), ("v", "هفتی", "V-neck"), ("polo", "پولو", "Polo")),
                EnumAttr("pattern", "الگو", "Pattern", false, true, false,
                    ("solid", "ساده", "Solid"), ("stripe", "راه‌راه", "Stripe"), ("print", "چاپی", "Print")),
            ],
            ["home"] =
            [
                NumAttr("power", "توان", "Power", "W", true, true),
                NumAttr("capacity", "ظرفیت", "Capacity", "L", true, true),
                EnumAttr("color", "رنگ", "Color", false, true, false,
                    ("white", "سفید", "White"), ("silver", "نقره‌ای", "Silver"), ("black", "مشکی", "Black")),
                EnumAttr("body", "جنس بدنه", "Body material", false, true, false,
                    ("steel", "استیل", "Steel"), ("plastic", "پلاستیک", "Plastic"), ("glass", "شیشه", "Glass")),
                EnumAttr("energy", "رده انرژی", "Energy class", false, true, true,
                    ("a", "A", "A"), ("a_plus", "A+", "A+"), ("a_pp", "A++", "A++")),
                EnumAttr("control", "نوع کنترل", "Control type", false, true, false,
                    ("manual", "دستی", "Manual"), ("digital", "دیجیتال", "Digital"), ("smart", "هوشمند", "Smart")),
            ],
            ["books"] =
            [
                TextAttr("author", "نویسنده", "Author", true, false),
                TextAttr("publisher", "ناشر", "Publisher", false, false),
                EnumAttr("language", "زبان", "Language", false, true, false,
                    ("fa", "فارسی", "Persian"), ("en", "انگلیسی", "English")),
                EnumAttr("cover", "نوع جلد", "Cover type", false, true, false,
                    ("soft", "شومیز", "Paperback"), ("hard", "گالینگور", "Hardcover")),
                NumAttr("pages", "تعداد صفحات", "Page count", null, false, true),
                EnumAttr("age", "گروه سنی", "Age group", false, true, false,
                    ("child", "کودک", "Children"), ("teen", "نوجوان", "Teen"), ("adult", "بزرگسال", "Adult")),
            ],
            ["food"] =
            [
                NumAttr("volume", "وزن/حجم", "Weight/volume", "g", true, true),
                EnumAttr("pack", "نوع بسته‌بندی", "Packaging", false, true, false,
                    ("box", "جعبه", "Box"), ("bottle", "بطری", "Bottle"), ("bag", "کیسه", "Bag")),
                BoolAttr("diet", "رژیمی", "Diet", true, false),
                BoolAttr("sugar_free", "بدون قند", "Sugar-free", true, false),
                BoolAttr("organic", "ارگانیک", "Organic", true, false),
            ],
        };

    /// <summary>کد کامل ویژگی با پیشوند seam و دامنه.</summary>
    public static string AttributeCode(string domain, string suffix) =>
        CatalogDemoSeam.AttributeCodePrefix + domain + "_" + suffix;

    /// <summary>slug رده با پیشوند seam.</summary>
    public static string CategorySlug(string key) => CatalogDemoSeam.CategorySlugPrefix + key;

    /// <summary>slug برند با پیشوند seam.</summary>
    public static string BrandSlug(string key) => CatalogDemoSeam.BrandSlugPrefix + key;

    /// <summary>کد برچسب با پیشوند seam.</summary>
    public static string TagCode(string key) => CatalogDemoSeam.TagCodePrefix + key;

    private static IReadOnlyList<CatalogDemoL1Spec> BuildRoots()
    {
        CatalogDemoBindingSpec B(string suffix, int order, bool req, bool filter, bool variant, bool comparable) =>
            new(suffix, order, req, filter, variant, comparable);

        CatalogDemoL3Spec L3(
            string key,
            string fa,
            string en,
            string domain,
            bool facets,
            string[] tags,
            params CatalogDemoBindingSpec[] bindings) =>
            new(key, new(fa, en), domain, bindings, facets, tags);

        CatalogDemoL2Spec L2(string key, string fa, string en, params CatalogDemoL3Spec[] children) =>
            new(key, new(fa, en), children);

        CatalogDemoL1Spec L1(
            string key,
            string fa,
            string en,
            string descFa,
            string descEn,
            bool featured,
            string[] tags,
            params CatalogDemoL2Spec[] children) =>
            new(key, new(fa, en), descFa, descEn, children, featured, tags);

        return
        [
            L1("mobile-tablet", "موبایل و تبلت", "Mobile & Tablet",
                "گوشی و تبلت برای استفادهٔ روزمره و حرفه‌ای.", "Phones and tablets for everyday and pro use.",
                true, ["flagship", "popular"],
                L2("phones", "گوشی موبایل", "Mobile phones",
                    L3("smartphones", "گوشی هوشمند", "Smartphones", "mobile", true, ["flagship", "new"],
                        B("color", 10, true, true, true, false),
                        B("storage", 20, true, true, true, true),
                        B("ram", 30, false, true, false, true),
                        B("screen_size", 40, false, true, false, true),
                        B("five_g", 50, false, true, false, false)),
                    L3("feature-phones", "گوشی ساده", "Feature phones", "mobile", false, ["budget"],
                        B("color", 10, true, true, true, false),
                        B("battery", 20, false, true, false, true)),
                    L3("phone-accessories", "لوازم جانبی موبایل", "Phone accessories", "mobile", true, ["everyday"],
                        B("color", 10, false, true, false, false),
                        B("weight", 20, false, false, false, true))),
                L2("tablets", "تبلت", "Tablets",
                    L3("android-tablets", "تبلت اندروید", "Android tablets", "mobile", true, ["student", "popular"],
                        B("storage", 10, true, true, true, true),
                        B("ram", 20, true, true, false, true),
                        B("screen_size", 30, true, true, false, true),
                        B("panel", 40, false, true, false, false)),
                    L3("tablet-accessories", "لوازم جانبی تبلت", "Tablet accessories", "mobile", false, ["gift"],
                        B("color", 10, false, true, false, false)))),

            L1("laptop-computer", "لپ‌تاپ و کامپیوتر", "Laptop & Computer",
                "لپ‌تاپ، دسکتاپ و متعلقات محاسباتی.", "Laptops, desktops and computing gear.",
                true, ["office", "gaming"],
                L2("laptops", "لپ‌تاپ", "Laptops",
                    L3("ultrabooks", "اولترابوک", "Ultrabooks", "laptop", true, ["lightweight", "office"],
                        B("cpu", 10, true, true, false, true),
                        B("ram", 20, true, true, true, true),
                        B("storage", 30, true, true, true, true),
                        B("weight", 40, false, true, false, true),
                        B("color", 50, false, true, true, false)),
                    L3("gaming-laptops", "لپ‌تاپ گیمینگ", "Gaming laptops", "laptop", true, ["gaming", "advanced"],
                        B("cpu", 10, true, true, false, true),
                        B("gpu", 20, true, false, false, false),
                        B("ram", 30, true, true, true, true),
                        B("display", 40, false, true, false, true)),
                    L3("student-laptops", "لپ‌تاپ دانشجویی", "Student laptops", "laptop", true, ["student", "budget"],
                        B("cpu", 10, true, true, false, false),
                        B("ram", 20, true, true, true, true),
                        B("storage", 30, false, true, true, false),
                        B("os", 40, false, true, false, false)),
                    L3("laptop-bags", "کیف لپ‌تاپ", "Laptop bags", "clothing", false, ["travel"],
                        B("color", 10, false, true, false, false),
                        B("material", 20, false, true, false, false))),
                L2("desktops", "کامپیوتر رومیزی", "Desktops",
                    L3("towers", "کیس آماده", "Prebuilt towers", "laptop", true, ["gaming", "office"],
                        B("cpu", 10, true, true, false, true),
                        B("ram", 20, true, true, false, true),
                        B("storage", 30, true, true, false, true),
                        B("gpu", 40, false, false, false, false)),
                    L3("aio", "آل‌این‌وان", "All-in-one", "laptop", false, ["office", "compact"],
                        B("display", 10, true, true, false, true),
                        B("ram", 20, true, true, false, true),
                        B("os", 30, false, true, false, false)))),

            L1("home-appliances", "لوازم خانگی", "Home appliances",
                "لوازم برقی آشپزخانه و شست‌وشو.", "Kitchen and laundry appliances.",
                true, ["efficient", "family"],
                L2("kitchen-appliances", "لوازم آشپزخانه", "Kitchen appliances",
                    L3("refrigerators", "یخچال و فریزر", "Refrigerators", "home", true, ["family", "efficient"],
                        B("capacity", 10, true, true, false, true),
                        B("energy", 20, true, true, false, true),
                        B("color", 30, false, true, false, false),
                        B("control", 40, false, true, false, false)),
                    L3("microwaves", "مایکروویو", "Microwaves", "home", true, ["everyday"],
                        B("power", 10, true, true, false, true),
                        B("capacity", 20, false, true, false, true),
                        B("control", 30, false, true, false, false)),
                    L3("blenders", "مخلوط‌کن", "Blenders", "home", false, ["everyday", "compact"],
                        B("power", 10, true, true, false, true),
                        B("body", 20, false, true, false, false))),
                L2("laundry", "شست‌وشو", "Laundry",
                    L3("washing-machines", "ماشین لباسشویی", "Washing machines", "home", true, ["family", "efficient"],
                        B("capacity", 10, true, true, false, true),
                        B("energy", 20, true, true, false, true),
                        B("control", 30, false, true, false, false)),
                    L3("dryers", "خشک‌کن", "Dryers", "home", false, ["premium"],
                        B("capacity", 10, true, true, false, true),
                        B("energy", 20, false, true, false, true)))),

            L1("av", "صوتی و تصویری", "Audio & Video",
                "تلویزیون، اسپیکر و تجهیزات AV.", "TVs, speakers and AV gear.",
                true, ["popular", "premium"],
                L2("tv", "تلویزیون", "Televisions",
                    L3("smart-tv", "تلویزیون هوشمند", "Smart TVs", "home", true, ["popular", "new"],
                        B("capacity", 10, false, false, false, false),
                        B("color", 20, false, true, false, false),
                        B("energy", 30, false, true, false, true),
                        B("control", 40, false, true, false, false)),
                    L3("tv-mounts", "پایه و براکت", "TV mounts", "home", false, ["everyday"],
                        B("body", 10, false, true, false, false))),
                L2("audio", "صوتی", "Audio",
                    L3("headphones", "هدفون", "Headphones", "mobile", true, ["wireless", "travel"],
                        B("color", 10, true, true, true, false),
                        B("weight", 20, false, true, false, true),
                        B("five_g", 30, false, false, false, false)),
                    L3("speakers", "اسپیکر", "Speakers", "home", true, ["wireless", "gift"],
                        B("power", 10, true, true, false, true),
                        B("color", 20, false, true, false, false),
                        B("control", 30, false, true, false, false)),
                    L3("soundbars", "ساندبار", "Soundbars", "home", false, ["premium"],
                        B("power", 10, true, true, false, true),
                        B("color", 20, false, true, false, false)))),

            L1("fashion", "مد و پوشاک", "Fashion & Apparel",
                "پوشاک مردانه و زنانه.", "Men's and women's apparel.",
                true, ["everyday", "summer"],
                L2("men", "مردانه", "Men",
                    L3("men-shirts", "پیراهن مردانه", "Men's shirts", "clothing", true, ["office", "everyday"],
                        B("color", 10, true, true, true, false),
                        B("size", 20, true, true, true, false),
                        B("material", 30, false, true, false, false),
                        B("collar", 40, false, false, false, false)),
                    L3("men-pants", "شلوار مردانه", "Men's pants", "clothing", true, ["everyday"],
                        B("color", 10, true, true, true, false),
                        B("size", 20, true, true, true, false),
                        B("material", 30, false, true, false, false)),
                    L3("men-shoes", "کفش مردانه", "Men's shoes", "clothing", true, ["everyday", "durable"],
                        B("color", 10, true, true, true, false),
                        B("size", 20, true, true, true, false),
                        B("season", 30, false, true, false, false))),
                L2("women", "زنانه", "Women",
                    L3("women-dresses", "پیراهن زنانه", "Women's dresses", "clothing", true, ["summer", "gift"],
                        B("color", 10, true, true, true, false),
                        B("size", 20, true, true, true, false),
                        B("pattern", 30, false, true, false, false),
                        B("season", 40, false, true, false, false)),
                    L3("women-tops", "تاپ و بلوز", "Tops & blouses", "clothing", true, ["everyday"],
                        B("color", 10, true, true, true, false),
                        B("size", 20, true, true, true, false),
                        B("material", 30, false, true, false, false)),
                    L3("women-shoes", "کفش زنانه", "Women's shoes", "clothing", true, ["popular"],
                        B("color", 10, true, true, true, false),
                        B("size", 20, true, true, true, false)),
                    L3("women-bags", "کیف زنانه", "Women's bags", "clothing", false, ["gift", "minimal"],
                        B("color", 10, false, true, false, false),
                        B("material", 20, false, true, false, false)))),

            L1("beauty", "زیبایی و سلامت", "Beauty & Health",
                "مراقبت پوست و آرایش.", "Skincare and makeup.",
                false, ["gift", "popular"],
                L2("skincare", "مراقبت پوست", "Skincare",
                    L3("moisturizers", "مرطوب‌کننده", "Moisturizers", "food", false, ["everyday"],
                        B("volume", 10, true, true, false, true),
                        B("organic", 20, false, true, false, false),
                        B("pack", 30, false, true, false, false)),
                    L3("serums", "سرم", "Serums", "food", true, ["premium"],
                        B("volume", 10, true, true, false, true),
                        B("organic", 20, false, true, false, false))),
                L2("makeup", "آرایشی", "Makeup",
                    L3("lipstick", "رژ لب", "Lipstick", "clothing", true, ["gift", "popular"],
                        B("color", 10, true, true, true, false),
                        B("pattern", 20, false, false, false, false)),
                    L3("foundation", "کرم پودر", "Foundation", "clothing", false, ["everyday"],
                        B("color", 10, true, true, false, false)),
                    L3("mascara", "ریمل", "Mascara", "clothing", false, ["everyday"],
                        B("color", 10, false, true, false, false)))),

            L1("home-kitchen", "خانه و آشپزخانه", "Home & Kitchen",
                "ظروف پخت و دکوراسیون منزل.", "Cookware and home décor.",
                false, ["family", "gift"],
                L2("cookware", "ظروف پخت", "Cookware",
                    L3("pots", "قابلمه", "Pots", "home", true, ["durable", "family"],
                        B("body", 10, true, true, false, false),
                        B("capacity", 20, false, true, false, true),
                        B("color", 30, false, true, false, false)),
                    L3("pans", "تابه", "Pans", "home", false, ["everyday"],
                        B("body", 10, true, true, false, false),
                        B("color", 20, false, true, false, false))),
                L2("decor", "دکوراسیون", "Décor",
                    L3("lighting", "روشنایی", "Lighting", "home", true, ["modern"],
                        B("power", 10, false, true, false, true),
                        B("color", 20, false, true, false, false),
                        B("control", 30, false, true, false, false)),
                    L3("textiles", "منسوجات منزل", "Home textiles", "clothing", false, ["minimal"],
                        B("material", 10, false, true, false, false),
                        B("color", 20, false, true, false, false),
                        B("pattern", 30, false, true, false, false)),
                    L3("storage", "باکس و نظم‌دهنده", "Storage organizers", "home", false, ["compact"],
                        B("body", 10, false, true, false, false),
                        B("color", 20, false, true, false, false)))),

            L1("sport-travel", "ورزش و سفر", "Sport & Travel",
                "تناسب اندام و سفر.", "Fitness and travel gear.",
                true, ["outdoor", "travel"],
                L2("fitness", "تناسب اندام", "Fitness",
                    L3("yoga", "یوگا", "Yoga", "clothing", false, ["beginner", "indoor"],
                        B("color", 10, false, true, false, false),
                        B("material", 20, false, true, false, false)),
                    L3("weights", "وزنه و دمبل", "Weights", "home", true, ["advanced", "durable"],
                        B("capacity", 10, true, true, false, true),
                        B("body", 20, false, true, false, false)),
                    L3("cardio", "هوازی", "Cardio", "home", false, ["advanced"],
                        B("power", 10, false, true, false, true),
                        B("control", 20, false, true, false, false))),
                L2("travel", "سفر", "Travel",
                    L3("luggage", "چمدان", "Luggage", "clothing", true, ["travel", "durable"],
                        B("color", 10, false, true, false, false),
                        B("material", 20, false, true, false, false),
                        B("size", 30, false, true, false, false)),
                    L3("camping", "کمپینگ", "Camping", "home", false, ["outdoor"],
                        B("body", 10, false, true, false, false),
                        B("capacity", 20, false, true, false, true)))),

            L1("books-stationery", "کتاب و لوازم تحریر", "Books & Stationery",
                "کتاب و نوشت‌افزار.", "Books and stationery.",
                false, ["student", "gift"],
                L2("books", "کتاب", "Books",
                    L3("fiction", "داستان", "Fiction", "books", true, ["popular"],
                        B("author", 10, true, false, false, false),
                        B("language", 20, false, true, false, false),
                        B("cover", 30, false, true, false, false),
                        B("pages", 40, false, false, false, true)),
                    L3("nonfiction", "غیر داستانی", "Non-fiction", "books", true, ["office", "student"],
                        B("author", 10, true, false, false, false),
                        B("publisher", 20, false, false, false, false),
                        B("language", 30, false, true, false, false),
                        B("age", 40, false, true, false, false)),
                    L3("children-books", "کتاب کودک", "Children's books", "books", false, ["kids", "gift"],
                        B("author", 10, false, false, false, false),
                        B("age", 20, true, true, false, false),
                        B("cover", 30, false, true, false, false))),
                L2("stationery", "لوازم تحریر", "Stationery",
                    L3("notebooks", "دفتر و دفترچه", "Notebooks", "clothing", false, ["student"],
                        B("color", 10, false, true, false, false),
                        B("size", 20, false, true, false, false)),
                    L3("pens", "خودکار و روان‌نویس", "Pens", "clothing", false, ["everyday"],
                        B("color", 10, false, true, false, false)))),

            L1("kids-toys", "کودک و اسباب‌بازی", "Kids & Toys",
                "اسباب‌بازی و کالای کودک.", "Toys and baby goods.",
                false, ["kids", "gift"],
                L2("toys", "اسباب‌بازی", "Toys",
                    L3("educational", "آموزشی", "Educational toys", "books", true, ["kids", "beginner"],
                        B("age", 10, true, true, false, false),
                        B("language", 20, false, true, false, false)),
                    L3("outdoor-toys", "فضای باز", "Outdoor toys", "home", false, ["kids", "outdoor"],
                        B("color", 10, false, true, false, false),
                        B("body", 20, false, true, false, false)),
                    L3("puzzles", "پازل", "Puzzles", "books", false, ["kids", "family"],
                        B("age", 10, true, true, false, false),
                        B("pages", 20, false, false, false, false))),
                L2("baby", "نوزاد", "Baby",
                    L3("baby-care", "مراقبت نوزاد", "Baby care", "food", false, ["kids", "family"],
                        B("volume", 10, false, true, false, true),
                        B("organic", 20, false, true, false, false)),
                    L3("strollers", "کالسکه", "Strollers", "home", true, ["kids", "premium"],
                        B("color", 10, false, true, false, false),
                        B("body", 20, false, true, false, false),
                        B("capacity", 30, false, true, false, true)))),

            L1("auto-moto", "خودرو و موتورسیکلت", "Auto & Motorcycle",
                "لوازم خودرو و موتور.", "Car and motorcycle parts.",
                false, ["durable", "outdoor"],
                L2("car", "خودرو", "Car",
                    L3("car-care", "مراقبت خودرو", "Car care", "home", false, ["everyday"],
                        B("volume", 10, false, true, false, true),
                        B("pack", 20, false, true, false, false)),
                    L3("car-electronics", "الکترونیک خودرو", "Car electronics", "mobile", true, ["wireless"],
                        B("color", 10, false, true, false, false),
                        B("five_g", 20, false, false, false, false)),
                    L3("car-accessories", "لوازم جانبی خودرو", "Car accessories", "home", false, ["gift"],
                        B("color", 10, false, true, false, false),
                        B("body", 20, false, true, false, false))),
                L2("moto", "موتورسیکلت", "Motorcycle",
                    L3("moto-gear", "لوازم موتور", "Motorcycle gear", "clothing", true, ["outdoor", "durable"],
                        B("color", 10, false, true, false, false),
                        B("size", 20, false, true, false, false),
                        B("material", 30, false, true, false, false)))),

            // تک L2 برای نامتقارن بودن درخت
            L1("tools", "ابزار و تجهیزات", "Tools & Equipment",
                "ابزار دستی و برقی کارگاهی.", "Hand and power tools.",
                false, ["durable", "office"],
                L2("hand-tools", "ابزار دستی", "Hand tools",
                    L3("screwdrivers", "پیچ‌گوشتی", "Screwdrivers", "home", false, ["everyday", "durable"],
                        B("body", 10, false, true, false, false),
                        B("color", 20, false, true, false, false)),
                    L3("wrenches", "آچار", "Wrenches", "home", true, ["durable"],
                        B("body", 10, true, true, false, false),
                        B("capacity", 20, false, true, false, true)),
                    L3("tool-sets", "ست ابزار", "Tool sets", "home", true, ["gift", "family"],
                        B("body", 10, false, true, false, false),
                        B("capacity", 20, false, true, false, true),
                        B("color", 30, false, true, false, false)))),

            L1("supermarket", "سوپرمارکت", "Supermarket",
                "خوراکی و نوشیدنی روزمره.", "Everyday groceries and drinks.",
                true, ["everyday", "family"],
                L2("grocery", "خوراکی", "Grocery",
                    L3("dairy", "لبنیات", "Dairy", "food", true, ["everyday", "family"],
                        B("volume", 10, true, true, false, true),
                        B("pack", 20, false, true, false, false),
                        B("diet", 30, false, true, false, false)),
                    L3("snacks", "تنقلات", "Snacks", "food", true, ["everyday"],
                        B("volume", 10, true, true, false, true),
                        B("sugar_free", 20, false, true, false, false),
                        B("pack", 30, false, true, false, false)),
                    L3("pantry", "خشکبار و انباری", "Pantry", "food", false, ["family"],
                        B("volume", 10, true, true, false, true),
                        B("organic", 20, false, true, false, false)),
                    L3("spices", "ادویه", "Spices", "food", false, ["everyday"],
                        B("volume", 10, false, true, false, true),
                        B("pack", 20, false, true, false, false))),
                L2("beverages", "نوشیدنی", "Beverages",
                    L3("soft-drinks", "نوشابه و آبمیوه‌", "Soft drinks", "food", true, ["summer"],
                        B("volume", 10, true, true, false, true),
                        B("sugar_free", 20, false, true, false, false),
                        B("pack", 30, false, true, false, false)),
                    L3("tea-coffee", "چای و قهوه", "Tea & coffee", "food", false, ["everyday", "gift"],
                        B("volume", 10, true, true, false, true),
                        B("organic", 20, false, true, false, false),
                        B("pack", 30, false, true, false, false)))),

            // تک L2
            L1("watches-accessories", "ساعت و اکسسوری", "Watches & Accessories",
                "ساعت و زیورآلات.", "Watches and accessories.",
                false, ["gift", "premium"],
                L2("watches", "ساعت", "Watches",
                    L3("smartwatches", "ساعت هوشمند", "Smartwatches", "mobile", true, ["new", "wireless"],
                        B("color", 10, true, true, true, false),
                        B("battery", 20, false, true, false, true),
                        B("five_g", 30, false, false, false, false),
                        B("weight", 40, false, true, false, true)),
                    L3("classic-watches", "ساعت کلاسیک", "Classic watches", "clothing", true, ["classic", "gift"],
                        B("color", 10, false, true, false, false),
                        B("material", 20, false, true, false, false),
                        B("size", 30, false, true, false, false)))),

            L1("digital-gadgets", "کالای دیجیتال و گجت‌ها", "Digital gadgets",
                "گجت‌های پوشیدنی و جانبی دیجیتال.", "Wearables and digital accessories.",
                true, ["new", "popular"],
                L2("wearables", "پوشیدنی", "Wearables",
                    L3("fitness-bands", "دستبند سلامتی", "Fitness bands", "mobile", true, ["portable", "everyday"],
                        B("color", 10, true, true, true, false),
                        B("battery", 20, false, true, false, true),
                        B("weight", 30, false, true, false, true)),
                    L3("earbuds", "ایرباد", "Earbuds", "mobile", true, ["wireless", "travel"],
                        B("color", 10, true, true, true, false),
                        B("weight", 20, false, true, false, true))),
                L2("gadget-accessories", "جانبی گجت", "Gadget accessories",
                    L3("chargers", "شارژر", "Chargers", "mobile", true, ["fast-charge", "everyday"],
                        B("color", 10, false, true, false, false),
                        B("weight", 20, false, false, false, true)),
                    L3("cables", "کابل", "Cables", "mobile", false, ["everyday"],
                        B("color", 10, false, true, false, false)),
                    L3("power-banks", "پاوربانک", "Power banks", "mobile", true, ["portable", "travel"],
                        B("battery", 10, true, true, false, true),
                        B("color", 20, false, true, false, false),
                        B("weight", 30, false, true, false, true)))),
        ];
    }

    private static CatalogDemoAttributeSpec EnumAttr(
        string suffix,
        string fa,
        string en,
        bool variant,
        bool filterable,
        bool comparable,
        params (string Code, string Fa, string En)[] options) =>
        new(suffix, CatalogAttributeValueKind.Enumeration, variant, new(fa, en), null, filterable, comparable, options);

    private static CatalogDemoAttributeSpec NumAttr(
        string suffix,
        string fa,
        string en,
        string? unit,
        bool filterable,
        bool comparable) =>
        new(suffix, CatalogAttributeValueKind.Number, false, new(fa, en), unit, filterable, comparable, []);

    private static CatalogDemoAttributeSpec BoolAttr(
        string suffix,
        string fa,
        string en,
        bool filterable,
        bool comparable) =>
        new(suffix, CatalogAttributeValueKind.Boolean, false, new(fa, en), null, filterable, comparable, []);

    private static CatalogDemoAttributeSpec TextAttr(
        string suffix,
        string fa,
        string en,
        bool filterable,
        bool comparable) =>
        new(suffix, CatalogAttributeValueKind.Text, false, new(fa, en), null, filterable, comparable, []);
}
