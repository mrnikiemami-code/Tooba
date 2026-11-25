/**
 * Offer workspace copy (fa default, en mirror). Natural product language only; no internal
 * architecture vocabulary is exposed to the operator.
 */
export interface OfferWorkspaceMessages {
  titleCreate: string;
  titleEdit: string;
  breadcrumb: string;
  save: string;
  saveDraft: string;
  saving: string;
  cancel: string;
  retry: string;
  loading: string;
  unsaved: string;
  saveErrorTitle: string;
  saveError: string;
  saved: string;
  noChanges: string;
  seller: string;
  status: string;
  buy: string;
  buyable: string;
  notBuyable: string;
  priceSummary: string;
  lastSaved: string;
  priceEmpty: string;
  market: string;
  currency: string;
  product: string;
  variant: string;
  channel: string;
  sellerSku: string;
  offerId: string;
  identityTitle: string;
  identityDesc: string;
  identityLocked: string;
  channelHint: string;
  sellerHint: string;
  sellerSessionHint: string;
  sellerRequired: string;
  productRequired: string;
  variantRequired: string;
  productPickerHint: string;
  variantHint: string;
  skuHint: string;
  skuLockedHint: string;
  offerIdHint: string;
  readiness: string;
  readinessOf: string;
  readinessHint: string;
  historyOnlyEdit: string;
  historyOnlyEditDesc: string;
  tabs: {
    identity: string;
    pricing: string;
    discounts: string;
    inventory: string;
    shipping: string;
    conditions: string;
    history: string;
  };
  tabLocked: string;
  tabNeedsCreate: string;
  pricingTitle: string;
  pricingDesc: string;
  pricingAmount: string;
  pricingCurrency: string;
  pricingType: string;
  pricingTypeBase: string;
  pricingStatus: string;
  pricingValidity: string;
  pricingCurrent: string;
  pricingNoDecimals: string;
  pricingLegacyDecimals: string;
  pricingUnavailable: string;
  discountsTitle: string;
  discountsDesc: string;
  discountsEmpty: string;
  discountsUnavailable: string;
  inventoryTitle: string;
  inventoryDesc: string;
  inventoryPhysical: string;
  inventoryReserved: string;
  inventoryAvailable: string;
  inventoryLocation: string;
  inventoryOnHand: string;
  inventoryReason: string;
  inventoryReasonHint: string;
  inventoryEmpty: string;
  inventoryUnavailable: string;
  inventoryReservedReadOnly: string;
  shippingTitle: string;
  shippingDesc: string;
  shippingResponsibility: string;
  shippingUnassigned: string;
  shippingSeller: string;
  shippingPlatform: string;
  shippingType: string;
  shippingService: string;
  shippingServiceUnavailable: string;
  shippingAdminOnly: string;
  shippingSaved: string;
  conditionsTitle: string;
  quantityTitle: string;
  returnPolicyDesc: string;
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
  precision: string;
  step: string;
  rounding: string;
  unitHint: string;
  lifecycle: string;
  lifecycleDraft: string;
  lifecycleActive: string;
  lifecycleSuspended: string;
  lifecycleArchived: string;
  archivedLocked: string;
  buyableReasonNoPrice: string;
  buyableReasonNoStock: string;
  buyableReasonInactive: string;
  historyTitle: string;
  historyDesc: string;
  historyEmpty: string;
  perTabError: string;
  dirtyTab: string;
  denied: string;
  deniedDetail: string;
  loadFailed: string;
  channelMarketplace: string;
  sellerSession: string;
  imageAlt: string;
  noImage: string;
  productMetaSeparator: string;
  variantCodeLabel: string;
  skuNone: string;
  select: string;
  notSelected: string;
  search: string;
  nextPage: string;
  prevPage: string;
  pageInfo: string;
  noResults: string;
  pickerError: string;
  createNeedsIdentity: string;
  validationAmount: string;
  validationDecimals: string;
  validationInventory: string;
  validationBelowReserved: string;
  validationReason: string;
  validationQty: string;
  validationQtyOrder: string;
  validationReturnWindow: string;
  validationReturnRange: string;
  sellerLocked: string;
  variantLocked: string;
  variantSelected: string;
  dataUnavailable: string;
}

const fa: OfferWorkspaceMessages = {
  titleCreate: "ایجاد آفر",
  titleEdit: "ویرایش آفر",
  breadcrumb: "خانه / آفرها",
  save: "ذخیره تغییرات",
  saveDraft: "ذخیره پیش‌نویس",
  saving: "در حال ذخیره…",
  cancel: "انصراف",
  retry: "تلاش دوباره",
  loading: "در حال بارگذاری…",
  unsaved: "تغییرات ذخیره‌نشده",
  saveErrorTitle: "ذخیره انجام نشد",
  saveError: "تغییرات شما حفظ شد؛ دوباره تلاش کنید.",
  saved: "تغییرات ذخیره شد.",
  noChanges: "تغییری برای ذخیره وجود ندارد.",
  seller: "فروشنده",
  status: "وضعیت آفر",
  buy: "خرید",
  buyable: "قابل خرید",
  notBuyable: "غیرقابل خرید",
  priceSummary: "قیمت · بازار و ارز",
  lastSaved: "ذخیره‌شده",
  priceEmpty: "قیمتی ثبت نشده است",
  market: "بازار",
  currency: "ارز",
  product: "محصول",
  variant: "تنوع محصول",
  channel: "کانال فروش",
  sellerSku: "کد کالای فروشنده",
  offerId: "شناسه آفر",
  identityTitle: "شناسه آفر",
  identityDesc: "فروشنده، محصول و تنوع، هویت آفر را می‌سازند. تا این سه مورد کامل نشود، تب‌های دیگر فعال نمی‌شوند.",
  identityLocked: "این آفر ایجاد شده است؛ فروشنده و تنوع قابل تغییر نیستند. برای تغییر آن‌ها باید آفر جدید بسازید.",
  channelHint: "کانال فروش این آفر بازارگاه است و پس از ایجاد تغییر نمی‌کند.",
  sellerHint: "جست‌وجو بر اساس نام یا کد فروشنده، با صفحه‌بندی سروری.",
  sellerSessionHint: "هویت فروشنده از نشست شما خوانده می‌شود.",
  sellerRequired: "فروشنده را انتخاب کنید.",
  productRequired: "محصول را از کاتالوگ انتخاب کنید.",
  variantRequired: "یک تنوع معتبر انتخاب کنید.",
  productPickerHint: "انتخاب قبلی هنگام صفحه‌بندی حفظ می‌شود.",
  variantHint: "فقط تنوع‌های معتبر همین محصول نمایش داده می‌شود.",
  skuHint: "کد یکتای کالا در سیستم فروشنده (حروف انگلیسی).",
  skuLockedHint: "پس از ایجاد آفر قابل تغییر نیست.",
  offerIdHint: "شناسه یکتا در سرور ساخته می‌شود و قابل تغییر نیست.",
  readiness: "مسیر تکمیل",
  readinessOf: "از ۳ مورد آماده است.",
  readinessHint: "۵ تب دیگر پس از انتخاب هر سه مورد فعال می‌شوند.",
  historyOnlyEdit: "تاریخچه، فقط در ویرایش",
  historyOnlyEditDesc: "برای آفر تازه تاریخچه‌ای وجود ندارد. رویدادها فقط پس از تغییر واقعی ثبت می‌شوند.",
  tabs: {
    identity: "مشخصات آفر",
    pricing: "قیمت‌گذاری",
    discounts: "تخفیفات",
    inventory: "موجودی",
    shipping: "ارسال",
    conditions: "شرایط فروش",
    history: "تاریخچه",
  },
  tabLocked: "پس از انتخاب فروشنده، محصول و تنوع فعال می‌شود.",
  tabNeedsCreate: "پس از ذخیره پیش‌نویس فعال می‌شود.",
  pricingTitle: "قیمت‌گذاری",
  pricingDesc: "قیمت پایه همین آفر را ثبت کنید. مبلغ ریال با اعداد فارسی و بدون اعشار نمایش داده می‌شود.",
  pricingAmount: "مبلغ",
  pricingCurrency: "ارز",
  pricingType: "نوع قیمت",
  pricingTypeBase: "پایه",
  pricingStatus: "وضعیت قیمت",
  pricingValidity: "اعتبار",
  pricingCurrent: "قیمت جاری",
  pricingNoDecimals: "مبلغ ریال بدون اعشار است.",
  pricingLegacyDecimals: "این قیمت اعشار دارد؛ تا زمانی که آن را تغییر ندهید، مقدار ذخیره‌شده دست‌نخورده می‌ماند.",
  pricingUnavailable: "ثبت قیمت برای بازارها و ارزهای دیگر از این صفحه در دسترس نیست.",
  discountsTitle: "تخفیفات",
  discountsDesc: "تخفیف‌های مرتبط با این آفر و شرایط اعمال آن‌ها.",
  discountsEmpty: "تخفیفی برای این آفر ثبت نشده است.",
  discountsUnavailable: "تعریف تخفیف برای یک آفر از این صفحه هنوز فعال نیست.",
  inventoryTitle: "موجودی",
  inventoryDesc: "موجودی هر انبار را جداگانه ثبت کنید. رزرو و موجودی قابل فروش فقط‌خواندنی هستند.",
  inventoryPhysical: "موجودی فیزیکی",
  inventoryReserved: "رزرو",
  inventoryAvailable: "قابل فروش",
  inventoryLocation: "انبار",
  inventoryOnHand: "موجودی",
  inventoryReason: "دلیل اصلاح",
  inventoryReasonHint: "برای هر اصلاح موجودی الزامی است.",
  inventoryEmpty: "برای این آفر هنوز موجودی ثبت نشده است.",
  inventoryUnavailable: "افزودن انبار جدید از این صفحه ممکن نیست.",
  inventoryReservedReadOnly: "رزرو و قابل فروش فقط‌خواندنی‌اند؛ موجودی فیزیکی نباید کمتر از رزرو باشد.",
  shippingTitle: "ارسال",
  shippingDesc: "مسئولیت ارسال این آفر و نوع ارسال آن را تعیین کنید.",
  shippingResponsibility: "مسئولیت ارسال",
  shippingUnassigned: "تعیین‌نشده",
  shippingSeller: "فروشنده",
  shippingPlatform: "پلتفرم",
  shippingType: "نوع ارسال",
  shippingService: "سرویس ارسال",
  shippingServiceUnavailable: "انتخاب سرویس ارسال برای این آفر در این نسخه ممکن نیست.",
  shippingAdminOnly: "تعیین مسئولیت ارسال فقط توسط مدیر پلتفرم انجام می‌شود.",
  shippingSaved: "مسئولیت ارسال ثبت شد.",
  conditionsTitle: "شرایط فروش",
  quantityTitle: "مقدار و واحد فروش",
  returnPolicyDesc: "سیاست مرجوعی این آفر؛ پیش‌فرض فروشگاه یا مهلت اختصاصی.",
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
  precision: "دقت اعشار",
  step: "گام",
  rounding: "روش گرد کردن",
  unitHint: "واحد، دقت اعشار، گام و گرد کردن از محصول خوانده می‌شود.",
  lifecycle: "وضعیت آفر",
  lifecycleDraft: "پیش‌نویس",
  lifecycleActive: "فعال",
  lifecycleSuspended: "معلق",
  lifecycleArchived: "بایگانی‌شده",
  archivedLocked: "آفر بایگانی‌شده قابل فعال‌سازی دوباره نیست.",
  buyableReasonNoPrice: "قیمتی برای این آفر ثبت نشده است",
  buyableReasonNoStock: "موجودی قابل فروش صفر است",
  buyableReasonInactive: "آفر فعال نیست",
  historyTitle: "تاریخچه",
  historyDesc: "تغییرات واقعی این آفر به ترتیب زمان.",
  historyEmpty: "تاریخچه‌ای برای این آفر در دسترس نیست.",
  perTabError: "ذخیره این بخش ناموفق بود.",
  dirtyTab: "تغییر ذخیره‌نشده",
  denied: "دسترسی مجاز نیست",
  deniedDetail: "این آفر متعلق به فروشنده دیگری است یا مجوز کافی وجود ندارد.",
  loadFailed: "خواندن آفر انجام نشد",
  channelMarketplace: "بازارگاه",
  sellerSession: "فروشنده نشست",
  imageAlt: "تصویر محصول",
  noImage: "بدون تصویر",
  productMetaSeparator: "·",
  variantCodeLabel: "کد تنوع",
  skuNone: "بدون کد",
  select: "انتخاب",
  notSelected: "انتخاب نشده",
  search: "جست‌وجو",
  nextPage: "صفحه بعد",
  prevPage: "صفحه قبل",
  pageInfo: "صفحه",
  noResults: "نتیجه‌ای پیدا نشد.",
  pickerError: "خواندن فهرست ممکن نشد.",
  createNeedsIdentity: "ابتدا فروشنده، محصول و تنوع را انتخاب کنید.",
  validationAmount: "مبلغ نامعتبر است.",
  validationDecimals: "ریال بدون اعشار است.",
  validationInventory: "موجودی نامعتبر است.",
  validationBelowReserved: "موجودی فیزیکی نباید کمتر از رزرو باشد.",
  validationReason: "دلیل اصلاح الزامی است.",
  validationQty: "مقدار خرید نامعتبر است.",
  validationQtyOrder: "حداقل مقدار خرید نباید از حداکثر بیشتر باشد.",
  validationReturnWindow: "مهلت اختصاصی نامعتبر است.",
  validationReturnRange: "مهلت اختصاصی خارج از محدوده مجاز است.",
  sellerLocked: "شناسه قفل",
  variantLocked: "تنوع قفل",
  variantSelected: "انتخاب‌شده",
  dataUnavailable: "اطلاعات این بخش در دسترس نیست.",
};

const en: OfferWorkspaceMessages = {
  ...fa,
  titleCreate: "Create offer",
  titleEdit: "Edit offer",
  breadcrumb: "Home / Offers",
  save: "Save changes",
  saveDraft: "Save draft",
  saving: "Saving…",
  cancel: "Cancel",
  retry: "Retry",
  loading: "Loading…",
  unsaved: "Unsaved changes",
  saveErrorTitle: "Save failed",
  saveError: "Your changes were kept; try again.",
  saved: "Changes saved.",
  noChanges: "Nothing to save.",
  seller: "Seller",
  status: "Offer status",
  buy: "Purchase",
  buyable: "Buyable",
  notBuyable: "Not buyable",
  priceSummary: "Price · market and currency",
  lastSaved: "Saved",
  priceEmpty: "No price recorded",
  market: "Market",
  currency: "Currency",
  product: "Product",
  variant: "Product variant",
  channel: "Sales channel",
  sellerSku: "Seller item code",
  offerId: "Offer id",
  identityTitle: "Offer identity",
  identityDesc: "Seller, product and variant define the offer identity. Other tabs stay disabled until all three are complete.",
  identityLocked: "This offer exists; seller and variant cannot change. Create a new offer to change them.",
  channelHint: "This offer's sales channel is Marketplace and cannot change after creation.",
  sellerHint: "Search by seller name or code with server paging.",
  sellerSessionHint: "The seller identity comes from your session.",
  sellerRequired: "Choose a seller.",
  productRequired: "Choose a product from the catalogue.",
  variantRequired: "Choose a valid variant.",
  productPickerHint: "Your selection is kept while paging.",
  variantHint: "Only valid variants of this product are shown.",
  skuHint: "Unique item code in the seller system (Latin characters).",
  skuLockedHint: "Cannot change after creation.",
  offerIdHint: "The unique server id is created automatically and cannot change.",
  readiness: "Completion path",
  readinessOf: "of 3 ready.",
  readinessHint: "The other five tabs enable after all three are chosen.",
  historyOnlyEdit: "History, edit mode only",
  historyOnlyEditDesc: "A new offer has no history. Events are recorded only after a real change.",
  tabs: {
    identity: "Offer identity",
    pricing: "Pricing",
    discounts: "Discounts",
    inventory: "Inventory",
    shipping: "Shipping",
    conditions: "Sales conditions",
    history: "History",
  },
  tabLocked: "Enabled after seller, product and variant are chosen.",
  tabNeedsCreate: "Enabled after the draft is saved.",
  pricingTitle: "Pricing",
  pricingDesc: "Record this offer's base price. IRR is shown in Persian digits without decimals.",
  pricingAmount: "Amount",
  pricingCurrency: "Currency",
  pricingType: "Price type",
  pricingTypeBase: "Base",
  pricingStatus: "Price status",
  pricingValidity: "Validity",
  pricingCurrent: "Current price",
  pricingNoDecimals: "IRR has no decimals.",
  pricingLegacyDecimals: "This price has decimals; the stored value stays untouched until you change it.",
  pricingUnavailable: "Recording prices for other markets and currencies is not available on this screen.",
  discountsTitle: "Discounts",
  discountsDesc: "Discounts related to this offer and their conditions.",
  discountsEmpty: "No discount is recorded for this offer.",
  discountsUnavailable: "Defining a discount for one offer is not available on this screen yet.",
  inventoryTitle: "Inventory",
  inventoryDesc: "Record each warehouse separately. Reserved and available are read-only.",
  inventoryPhysical: "Physical",
  inventoryReserved: "Reserved",
  inventoryAvailable: "Available",
  inventoryLocation: "Warehouse",
  inventoryOnHand: "On hand",
  inventoryReason: "Adjustment reason",
  inventoryReasonHint: "Required for every inventory adjustment.",
  inventoryEmpty: "No inventory is recorded for this offer yet.",
  inventoryUnavailable: "Adding a new warehouse is not available on this screen.",
  inventoryReservedReadOnly: "Reserved and available are read-only; on-hand must not be below reserved.",
  shippingTitle: "Shipping",
  shippingDesc: "Set this offer's fulfilment responsibility and shipping type.",
  shippingResponsibility: "Fulfilment responsibility",
  shippingUnassigned: "Unassigned",
  shippingSeller: "Seller",
  shippingPlatform: "Platform",
  shippingType: "Shipping type",
  shippingService: "Shipping service",
  shippingServiceUnavailable: "Choosing a shipping service for this offer is not available in this version.",
  shippingAdminOnly: "Only the platform admin assigns fulfilment responsibility.",
  shippingSaved: "Fulfilment responsibility recorded.",
  conditionsTitle: "Sales conditions",
  quantityTitle: "Quantity and unit",
  returnPolicyDesc: "This offer's return policy; store default or a custom window.",
  returnPolicy: "Return policy",
  returnDefault: "Store default policy",
  returnCustom: "Custom window",
  returnNonReturnable: "Non-returnable",
  returnCustomDays: "Custom window (days)",
  returnWindowHint: "Store default",
  returnSellerCannotOverride: "The seller cannot override the return policy.",
  minQty: "Minimum order quantity",
  maxQty: "Maximum order quantity",
  unit: "Product unit",
  precision: "Decimal precision",
  step: "Step",
  rounding: "Rounding",
  unitHint: "Unit, decimal precision, step and rounding come from the product.",
  lifecycle: "Offer status",
  lifecycleDraft: "Draft",
  lifecycleActive: "Active",
  lifecycleSuspended: "Suspended",
  lifecycleArchived: "Archived",
  archivedLocked: "An archived offer cannot be reactivated.",
  buyableReasonNoPrice: "No price is recorded for this offer",
  buyableReasonNoStock: "Available units are zero",
  buyableReasonInactive: "The offer is not active",
  historyTitle: "History",
  historyDesc: "Real changes to this offer in time order.",
  historyEmpty: "No history is available for this offer.",
  perTabError: "This section failed to save.",
  dirtyTab: "Unsaved change",
  denied: "Access denied",
  deniedDetail: "This offer belongs to another seller or the actor lacks permission.",
  loadFailed: "Offer could not be read",
  channelMarketplace: "Marketplace",
  sellerSession: "Session seller",
  imageAlt: "Product image",
  noImage: "No image",
  productMetaSeparator: "·",
  variantCodeLabel: "Variant code",
  skuNone: "No code",
  select: "Select",
  notSelected: "Not selected",
  search: "Search",
  nextPage: "Next page",
  prevPage: "Previous page",
  pageInfo: "Page",
  noResults: "No result found.",
  pickerError: "The list could not be read.",
  createNeedsIdentity: "Choose seller, product and variant first.",
  validationAmount: "Invalid amount.",
  validationDecimals: "IRR has no decimals.",
  validationInventory: "Invalid stock value.",
  validationBelowReserved: "On-hand must not be below reserved.",
  validationReason: "Adjustment reason is required.",
  validationQty: "Invalid purchase quantity.",
  validationQtyOrder: "Minimum quantity must not exceed maximum.",
  validationReturnWindow: "Invalid custom window.",
  validationReturnRange: "Custom window is outside the allowed range.",
  sellerLocked: "Identity locked",
  variantLocked: "Variant locked",
  variantSelected: "Selected",
  dataUnavailable: "This section's data is unavailable.",
};

export const offerWorkspaceMessages: Record<"fa" | "en", OfferWorkspaceMessages> = { fa, en };
