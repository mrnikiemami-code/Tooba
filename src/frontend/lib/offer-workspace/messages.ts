/**
 * Offer workspace copy (fa default, en mirror). UI text only; no domain decisions.
 */
export interface OfferWorkspaceMessages {
  titleCreate: string;
  titleEdit: string;
  breadcrumb: string;
  backToList: string;
  save: string;
  saveDraft: string;
  saving: string;
  cancel: string;
  retry: string;
  loading: string;
  unsaved: string;
  saveError: string;
  saved: string;
  noChanges: string;
  offerId: string;
  seller: string;
  product: string;
  variant: string;
  channel: string;
  status: string;
  buyable: string;
  notBuyable: string;
  price: string;
  market: string;
  lockedIdentity: string;
  lockedSeller: string;
  lockedVariant: string;
  channelImmutable: string;
  sellerSku: string;
  selectSeller: string;
  selectProduct: string;
  search: string;
  nextPage: string;
  prevPage: string;
  pageInfo: string;
  noResults: string;
  pickerError: string;
  dependentDisabled: string;
  createNeedsIdentity: string;
  identityTab: string;
  pricingTab: string;
  discountsTab: string;
  inventoryTab: string;
  shippingTab: string;
  conditionsTab: string;
  historyTab: string;
  tabNeedsCreate: string;
  pricingCurrent: string;
  pricingAmount: string;
  pricingCurrency: string;
  pricingType: string;
  pricingTypeBase: string;
  pricingStatus: string;
  pricingNotWired: string;
  pricingIrradix: string;
  discountsGated: string;
  discountsOwner: string;
  inventoryPhysical: string;
  inventoryReserved: string;
  inventoryAvailable: string;
  inventoryLocation: string;
  inventoryOnHand: string;
  inventoryReason: string;
  inventoryReasonHint: string;
  inventoryImmutableLocation: string;
  inventoryNeedsContract: string;
  inventoryNoPositions: string;
  inventoryReservedReadOnly: string;
  shippingResponsibility: string;
  shippingUnassigned: string;
  shippingSeller: string;
  shippingPlatform: string;
  shippingType: string;
  shippingService: string;
  shippingServiceDisabled: string;
  shippingServiceGated: string;
  shippingCurrentUnknown: string;
  shippingAdminOnly: string;
  shippingSave: string;
  shippingSaved: string;
  returnPolicy: string;
  returnDefault: string;
  returnCustom: string;
  returnNonReturnable: string;
  returnCustomDays: string;
  returnWindowHint: string;
  returnSellerCannotOverride: string;
  minQty: string;
  maxQty: string;
  unit: string;
  unitReadOnly: string;
  roundingNeedsContract: string;
  lifecycle: string;
  lifecycleDraft: string;
  lifecycleActive: string;
  lifecycleSuspended: string;
  lifecycleArchived: string;
  archivedLocked: string;
  buyableReasonNoPrice: string;
  buyableReasonNoStock: string;
  buyableReasonInactive: string;
  historyGated: string;
  historyNoFake: string;
  perTabError: string;
  dirtyTab: string;
  denied: string;
  deniedDetail: string;
  missingIdentity: string;
  loadFailed: string;
  channelMarketplace: string;
  sellerSession: string;
  readonlySeam: string;
}

export const offerWorkspaceMessages: Record<"fa" | "en", OfferWorkspaceMessages> = {
  fa: {
    titleCreate: "ایجاد آفر",
    titleEdit: "ویرایش آفر",
    breadcrumb: "خانه / آفرها",
    backToList: "بازگشت",
    save: "ذخیره تغییرات",
    saveDraft: "ذخیرهٔ پیش‌نویس",
    saving: "در حال ذخیره…",
    cancel: "انصراف",
    retry: "تلاش دوباره",
    loading: "در حال بارگذاری…",
    unsaved: "تغییرات ذخیره‌نشده",
    saveError: "ذخیره انجام نشد؛ داده‌های واردشده حفظ شد.",
    saved: "تغییرات ذخیره شد.",
    noChanges: "تغییری برای ذخیره وجود ندارد.",
    offerId: "شناسهٔ آفر",
    seller: "فروشنده",
    product: "محصول",
    variant: "گونه",
    channel: "کانال فروش",
    status: "وضعیت",
    buyable: "قابل خرید",
    notBuyable: "غیرقابل خرید",
    price: "قیمت",
    market: "بازار",
    lockedIdentity: "پس از ایجاد، فروشنده و گونه قفل می‌شوند.",
    lockedSeller: "فروشنده قفل است",
    lockedVariant: "گونه قفل است",
    channelImmutable: "کانال پس از ایجاد تغییرناپذیر است.",
    sellerSku: "SKU فروشنده",
    selectSeller: "انتخاب فروشنده",
    selectProduct: "انتخاب محصول",
    search: "جست‌وجو",
    nextPage: "صفحهٔ بعد",
    prevPage: "صفحهٔ قبل",
    pageInfo: "صفحه",
    noResults: "نتیجه‌ای پیدا نشد.",
    pickerError: "خواندن فهرست ممکن نشد.",
    dependentDisabled: "تا انتخاب فروشنده، محصول و گونه، زبانه‌های دیگر غیرفعال‌اند.",
    createNeedsIdentity: "ابتدا فروشنده، محصول و گونه را انتخاب کنید.",
    identityTab: "مشخصات آفر",
    pricingTab: "قیمت‌گذاری",
    discountsTab: "تخفیفات",
    inventoryTab: "موجودی",
    shippingTab: "ارسال",
    conditionsTab: "شرایط فروش",
    historyTab: "تاریخچه",
    tabNeedsCreate: "پس از ذخیرهٔ پیش‌نویس فعال می‌شود.",
    pricingCurrent: "قیمت جاری",
    pricingAmount: "مبلغ",
    pricingCurrency: "ارز",
    pricingType: "نوع قیمت",
    pricingTypeBase: "پایه",
    pricingStatus: "وضعیت قیمت",
    pricingNotWired: "مدیریت چندارزی/چندبازاری و بازهٔ اعتبار نیازمند قرارداد خواندن فهرست قیمت‌ها است و در این نسخه نمایش داده نمی‌شود.",
    pricingIrradix: "ریال بدون اعشار است.",
    discountsGated: "تعریف تخفیف برای یک آفر در مالکیت Promotion است و قرارداد آن هنوز تصمیم‌گیری نشده است.",
    discountsOwner: "هیچ تخفیف ساختگی یا پیش‌نمایش قیمت جعلی نمایش داده نمی‌شود.",
    inventoryPhysical: "موجودی فیزیکی",
    inventoryReserved: "رزرو",
    inventoryAvailable: "قابل فروش",
    inventoryLocation: "محل انبار",
    inventoryOnHand: "موجودی",
    inventoryReason: "دلیل اصلاح",
    inventoryReasonHint: "برای هر اصلاح موجودی الزامی است.",
    inventoryImmutableLocation: "هویت محل انبار تغییرناپذیر است.",
    inventoryNeedsContract: "افزودن یا حذف موقعیت انبار نیازمند قرارداد و کنترل وابستگی است.",
    inventoryNoPositions: "هنوز موقعیت انباری برای این آفر ثبت نشده است.",
    inventoryReservedReadOnly: "رزرو و قابل فروش فقط‌خواندنی‌اند؛ موجودی فیزیکی نباید کمتر از رزرو باشد.",
    shippingResponsibility: "مسئولیت ارسال",
    shippingUnassigned: "تعیین‌نشده",
    shippingSeller: "فروشنده",
    shippingPlatform: "پلتفرم",
    shippingType: "نوع ارسال",
    shippingService: "سرویس ارسال",
    shippingServiceDisabled: "سرویس ارسال تا تعیین مسئولیت غیرفعال است.",
    shippingServiceGated: "انتساب سرویس ارسال هر آفر در مرحلهٔ ۳ تصمیم‌گیری نشده و این نسخه آن را جعل نمی‌کند.",
    shippingCurrentUnknown: "قرارداد خواندن مسئولیت فعلی آفر در دسترس نیست؛ پس از ذخیره در همین نشست نمایش داده می‌شود.",
    shippingAdminOnly: "تعیین مسئولیت ارسال فقط توسط مدیر پلتفرم انجام می‌شود.",
    shippingSave: "ذخیرهٔ مسئولیت ارسال",
    shippingSaved: "مسئولیت ارسال ذخیره شد.",
    returnPolicy: "سیاست مرجوعی",
    returnDefault: "سیاست پیش‌فرض فروشگاه",
    returnCustom: "مهلت اختصاصی",
    returnNonReturnable: "غیرقابل مرجوعی",
    returnCustomDays: "مهلت اختصاصی (روز)",
    returnWindowHint: "مهلت پیش‌فرض فروشگاه",
    returnSellerCannotOverride: "تغییر سیاست مرجوعی توسط فروشنده مجاز نیست.",
    minQty: "حداقل مقدار خرید",
    maxQty: "حداکثر مقدار خرید",
    unit: "واحد کالا",
    unitReadOnly: "واحد، دقت اعشار، گام و گرد کردن فقط‌خواندنی و از محصول می‌آید.",
    roundingNeedsContract: "نمایش دقت اعشار/گام/گرد کردن به قرارداد تکمیلی محصول نیاز دارد.",
    lifecycle: "چرخهٔ عمر",
    lifecycleDraft: "پیش‌نویس",
    lifecycleActive: "فعال",
    lifecycleSuspended: "معلق",
    lifecycleArchived: "بایگانی‌شده",
    archivedLocked: "آفر بایگانی‌شده قابل فعال‌سازی دوباره نیست.",
    buyableReasonNoPrice: "قیمت مؤثر ثبت نشده است",
    buyableReasonNoStock: "موجودی قابل فروش صفر است",
    buyableReasonInactive: "آفر فعال نیست",
    historyGated: "قرارداد خواندن تاریخچه/رویدادهای آفر در دسترس نیست.",
    historyNoFake: "هیچ تاریخچه یا رویداد ساختگی نمایش داده نمی‌شود.",
    perTabError: "ذخیرهٔ این بخش ناموفق بود.",
    dirtyTab: "تغییر ذخیره‌نشده",
    denied: "دسترسی مجاز نیست",
    deniedDetail: "این آفر متعلق به فروشندهٔ دیگری است یا مجوز کافی وجود ندارد.",
    missingIdentity: "هویت فروشنده در نشست مشخص نیست.",
    loadFailed: "خواندن آفر انجام نشد.",
    channelMarketplace: "بازارگاه",
    sellerSession: "فروشندهٔ نشست",
    readonlySeam: "قیمت از Pricing و موجودی از Inventory خوانده می‌شود؛ روی محصول نوشته نمی‌شود.",
  },
  en: {
    titleCreate: "Create offer",
    titleEdit: "Edit offer",
    breadcrumb: "Home / Offers",
    backToList: "Back",
    save: "Save changes",
    saveDraft: "Save draft",
    saving: "Saving…",
    cancel: "Cancel",
    retry: "Retry",
    loading: "Loading…",
    unsaved: "Unsaved changes",
    saveError: "Save failed; entered data was preserved.",
    saved: "Changes saved.",
    noChanges: "Nothing to save.",
    offerId: "Offer id",
    seller: "Seller",
    product: "Product",
    variant: "Variant",
    channel: "Sales channel",
    status: "Status",
    buyable: "Buyable",
    notBuyable: "Not buyable",
    price: "Price",
    market: "Market",
    lockedIdentity: "After creation, seller and variant are locked.",
    lockedSeller: "Seller is locked",
    lockedVariant: "Variant is locked",
    channelImmutable: "Channel is immutable after creation.",
    sellerSku: "Seller SKU",
    selectSeller: "Select seller",
    selectProduct: "Select product",
    search: "Search",
    nextPage: "Next page",
    prevPage: "Previous page",
    pageInfo: "Page",
    noResults: "No result found.",
    pickerError: "List could not be read.",
    dependentDisabled: "Other tabs stay disabled until seller, product and variant are chosen.",
    createNeedsIdentity: "Choose seller, product and variant first.",
    identityTab: "Offer identity",
    pricingTab: "Pricing",
    discountsTab: "Discounts",
    inventoryTab: "Inventory",
    shippingTab: "Shipping",
    conditionsTab: "Sales conditions",
    historyTab: "History",
    tabNeedsCreate: "Available after the draft is saved.",
    pricingCurrent: "Current price",
    pricingAmount: "Amount",
    pricingCurrency: "Currency",
    pricingType: "Price type",
    pricingTypeBase: "Base",
    pricingStatus: "Price status",
    pricingNotWired: "Multi-currency/market rows and validity ranges need a price-list read contract and are not shown in this build.",
    pricingIrradix: "IRR has no decimals.",
    discountsGated: "Per-offer discount definition is owned by Promotion and its contract is not decided yet.",
    discountsOwner: "No fabricated discount or fake price preview is shown.",
    inventoryPhysical: "Physical",
    inventoryReserved: "Reserved",
    inventoryAvailable: "Available",
    inventoryLocation: "Warehouse",
    inventoryOnHand: "On hand",
    inventoryReason: "Adjustment reason",
    inventoryReasonHint: "Required for every inventory adjustment.",
    inventoryImmutableLocation: "Warehouse identity is immutable.",
    inventoryNeedsContract: "Adding or deleting an inventory position needs a contract and dependency control.",
    inventoryNoPositions: "No inventory position is recorded for this offer yet.",
    inventoryReservedReadOnly: "Reserved and available are read-only; on-hand must not be below reserved.",
    shippingResponsibility: "Fulfillment responsibility",
    shippingUnassigned: "Unassigned",
    shippingSeller: "Seller",
    shippingPlatform: "Platform",
    shippingType: "Shipping type",
    shippingService: "Shipping service",
    shippingServiceDisabled: "Shipping service is disabled until responsibility is set.",
    shippingServiceGated: "Per-offer service assignment is an unresolved Stage-3 decision and is not fabricated here.",
    shippingCurrentUnknown: "No read contract exposes the current assignment; it is shown for this session after save.",
    shippingAdminOnly: "Fulfillment responsibility is assigned by the platform admin only.",
    shippingSave: "Save responsibility",
    shippingSaved: "Fulfillment responsibility saved.",
    returnPolicy: "Return policy",
    returnDefault: "Store default policy",
    returnCustom: "Custom window",
    returnNonReturnable: "Non-returnable",
    returnCustomDays: "Custom window (days)",
    returnWindowHint: "Store default",
    returnSellerCannotOverride: "Seller is not allowed to override the return policy.",
    minQty: "Minimum order quantity",
    maxQty: "Maximum order quantity",
    unit: "Product unit",
    unitReadOnly: "Unit, decimal precision, step and rounding are read-only from the product.",
    roundingNeedsContract: "Decimal precision/step/rounding display needs a product contract extension.",
    lifecycle: "Lifecycle",
    lifecycleDraft: "Draft",
    lifecycleActive: "Active",
    lifecycleSuspended: "Suspended",
    lifecycleArchived: "Archived",
    archivedLocked: "An archived offer cannot be reactivated.",
    buyableReasonNoPrice: "No effective price is recorded",
    buyableReasonNoStock: "Available units are zero",
    buyableReasonInactive: "Offer is not active",
    historyGated: "No offer history/event read contract is available.",
    historyNoFake: "No fabricated history or events are shown.",
    perTabError: "This section failed to save.",
    dirtyTab: "Unsaved change",
    denied: "Access denied",
    deniedDetail: "This offer belongs to another seller or the actor lacks permission.",
    missingIdentity: "Seller identity is missing from the session.",
    loadFailed: "Offer could not be read.",
    channelMarketplace: "Marketplace",
    sellerSession: "Session seller",
    readonlySeam: "Price comes from Pricing and stock from Inventory; nothing is written onto the product.",
  },
};
