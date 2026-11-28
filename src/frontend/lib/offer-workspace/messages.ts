/**
 * Offer workspace copy (fa default, en mirror). Natural product language only; no internal
 * architecture vocabulary is exposed to the operator.
 */
import { shippingMessagesEn, shippingMessagesFa } from "./messages-shipping.ts";

export interface OfferWorkspaceMessages {
  titleCreate: string;
  titleEdit: string;
  breadcrumb: string;
  save: string;
  saveDraft: string;
  saving: string;
  cancel: string;
  close: string;
  retry: string;
  loading: string;
  unsaved: string;
  saveErrorTitle: string;
  partialTitle: string;
  dirtyExitTitle: string;
  dirtyExitText: string;
  dirtyExitConfirm: string;
  dirtyExitCancel: string;
  saveError: string;
  saved: string;
  noChanges: string;
  partialSaved: string;
  saveUnknown: string;
  createUnknown: string;
  reloadState: string;
  concurrentChange: string;
  draftRestored: string;
  reloaded: string;
  seller: string;
  status: string;
  buy: string;
  buyable: string;
  buyDisplay: string;
  buyabilityHint: string;
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
  channelPolicyPending: string;
  sellerHint: string;
  sellerSessionHint: string;
  offerCreatePermissionRequired: string;
  productAdditionalInformation: string;
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
  pricingRetired: string;
  pricingRemove: string;
  pricingAddCurrency: string;
  pricingRemoveConfirm: string;
  historyKindPriceRetired: string;
  conflictUnavailable: string;
  pricingValidity: string;
  pricingCurrent: string;
  pricingStoreCurrency: string;
  pricingNoDecimals: string;
  pricingLegacyDecimals: string;
  pricingUnavailable: string;
  pricingSelectCurrency: string;
  pricingSaveBeforeSwitch: string;
  pricingNoPrices: string;
  pricingPrimaryPrefix: string;
  discountsTitle: string;
  discountsDesc: string;
  discountsEmpty: string;
  discountsUnavailable: string;
  inventoryTitle: string;
  inventoryDesc: string;
  inventoryPhysical: string;
  inventoryReserved: string;
  inventoryAvailable: string;
  inventoryOfferedQuantity: string;
  inventoryOfferedHint: string;
  inventoryAllocationExceedsPhysical: string;
  inventoryLocation: string;
  inventoryOnHand: string;
  inventoryReason: string;
  inventoryReasonHint: string;
  inventoryEmpty: string;
  inventoryAddLocation: string;
  inventoryRemoveLocation: string;
  inventoryNoEligible: string;
  inventoryUnavailable: string;
  inventoryProvisionNote: string;
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
  shippingMode: string;
  shippingModeAuto: string;
  shippingModeAutoHint: string;
  shippingModeSpecific: string;
  shippingModeSpecificHint: string;
  shippingMethods: string;
  shippingAddMethod: string;
  shippingSearchMethod: string;
  shippingNoMethodResults: string;
  shippingNoMethods: string;
  shippingMethodsError: string;
  shippingNoSelection: string;
  shippingSelectAtLeastOne: string;
  shippingRemoveMethod: string;
  shippingLockedAuto: string;
  shippingMethodNote: string;
  shippingMethodStale: string;
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
  roundingNearest: string;
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
  historyActor: string;
  historyKindCreated: string;
  historyKindCommercial: string;
  historyKindPrice: string;
  historyKindInventory: string;
  historyKindAllocation: string;
  historyKindUnknown: string;
  historyKindFulfillment: string;
  perTabError: string;
  dirtyTab: string;
  denied: string;
  deniedDetail: string;
  loadFailed: string;
  channelMarketplace: string;
  channelDirect: string;
  channelAgency: string;
  channelCorporate: string;
  channelAffiliate: string;
  channelApi: string;
  sellerSession: string;
  imageAlt: string;
  noImage: string;
  productMetaSeparator: string;
  variantCodeLabel: string;
  skuNone: string;
  select: string;
  selectSellerTitle: string;
  selectProductTitle: string;
  selectVariantTitle: string;
  changeProduct: string;
  changeVariant: string;
  chooseProductFirst: string;
  productsEmpty: string;
  variantsEmpty: string;
  singleVariantAuto: string;
  productPickerError: string;
  variantPickerError: string;
  variantUnnamed: string;
  productResultCount: string;
  variantCount: string;
  notSelected: string;
  search: string;
  nextPage: string;
  prevPage: string;
  pageInfo: string;
  noResults: string;
  pickerError: string;
  createNeedsIdentity: string;
  duplicateActiveTitle: string;
  duplicateActive: string;
  viewExistingOffer: string;
  conflictChecking: string;
  validationAmount: string;
  priceRequired: string;
  validationDecimals: string;
  validationInventory: string;
  validationBelowReserved: string;
  validationReason: string;
  validationQty: string;
  fulfillmentProtected: string;
  validationStep: string;
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
  close: "بستن",
  retry: "تلاش دوباره",
  loading: "در حال بارگذاری…",
  unsaved: "تغییرات ذخیره‌نشده",
  saveErrorTitle: "ذخیره انجام نشد",
  partialTitle: "ذخیره ناقص",
  dirtyExitTitle: "تغییرات ذخیره‌نشده",
  dirtyExitText: "تغییرات ذخیره‌نشده‌ای دارید. با خروج، این تغییرات از این نشست پاک می‌شوند.",
  dirtyExitConfirm: "خروج بدون ذخیره",
  dirtyExitCancel: "بازگشت به فرم",
  saveError: "تغییرات شما حفظ شد؛ دوباره تلاش کنید.",
  saved: "تغییرات ذخیره شد.",
  noChanges: "تغییری برای ذخیره وجود ندارد.",
  partialSaved: "بخشی از تغییرات ذخیره شد؛ بخش باقی‌مانده با مقادیر شما حفظ شده است.",
  saveUnknown: "ارتباط قطع شد و نتیجه ذخیره قطعی نیست. برای جلوگیری از ثبت تکراری، وضعیت را بازخوانی کنید.",
  createUnknown: "ارتباط قطع شد و نتیجه ایجاد پیش‌نویس قطعی نیست. فهرست آفرها را بازخوانی کنید؛ انتخاب‌های شما حفظ شده است.",
  reloadState: "بازخوانی وضعیت",
  concurrentChange: "این آفر توسط نشست دیگری تغییر کرده است. ابتدا وضعیت را بازخوانی کنید؛ تغییرات شما حفظ می‌شود.",
  draftRestored: "پیش‌نویس محلی و انتخاب‌های ذخیره‌نشده بازیابی شد.",
  reloaded: "وضعیت سرور بازخوانی شد؛ تغییرات ذخیره‌نشده شما حفظ شده است.",
  seller: "فروشنده",
  status: "وضعیت آفر",
  buy: "خرید",
  buyable: "قابل خرید",
  buyDisplay: "قابلیت خرید (نمایشی)",
  buyabilityHint: "این شاخص از وضعیت فعال، قیمت و موجودی ساخته می‌شود؛ تأیید نهایی در سبد خرید انجام می‌شود.",
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
  channelPolicyPending: "کانال فعلی سامانه؛ سیاست انتخاب کانال هنوز از سوی دامنه تعیین نشده و به‌صورت انتخاب کاربر نمایش داده نمی‌شود.",
  sellerHint: "جست‌وجو بر اساس نام یا کد فروشنده، با صفحه‌بندی سروری.",
  sellerSessionHint: "هویت فروشنده از نشست شما خوانده می‌شود.",
  offerCreatePermissionRequired: "مجوز ایجاد آفر برای فروشنده فعلی فعال نیست.",
  productAdditionalInformation: "اطلاعات تکمیلی محصول",
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
  pricingRetired: "غیرفعال",
  pricingRemove: "حذف قیمت",
  pricingAddCurrency: "افزودن ارز دیگر",
  pricingRemoveConfirm: "قیمت این ارز از فروش خارج شود؟ سابقه قیمت حفظ می‌شود.",
  historyKindPriceRetired: "حذف قیمت",
  conflictUnavailable: "بررسی آفر موجود ممکن نیست. دوباره تلاش کنید.",
  pricingValidity: "اعتبار",
  pricingCurrent: "قیمت جاری",
  pricingStoreCurrency: "ارز پیش‌فرض فروشگاه",
  pricingNoDecimals: "این ارز بدون اعشار است.",
  pricingLegacyDecimals: "این قیمت اعشار دارد؛ تا زمانی که آن را تغییر ندهید، مقدار ذخیره‌شده دست‌نخورده می‌ماند.",
  pricingUnavailable: "فهرست ارزهای مجاز در دسترس نیست.",
  pricingSelectCurrency: "انتخاب ارز",
  pricingSaveBeforeSwitch: "ابتدا قیمت فعلی را ذخیره کنید یا تغییر آن را برگردانید.",
  pricingNoPrices: "هنوز قیمتی برای این آفر ثبت نشده است.",
  pricingPrimaryPrefix: "قیمت پایه",
  discountsTitle: "تخفیفات",
  discountsDesc: "تخفیف‌های مرتبط با این آفر و شرایط اعمال آن‌ها.",
  discountsEmpty: "تخفیفی برای این آفر ثبت نشده است.",
  discountsUnavailable: "تعریف تخفیف برای یک آفر از این صفحه هنوز فعال نیست.",
  inventoryTitle: "موجودی",
  inventoryDesc: "موجودی هر انبار را جداگانه ثبت کنید. رزرو و موجودی قابل فروش فقط‌خواندنی هستند.",
  inventoryPhysical: "موجودی فیزیکی",
  inventoryReserved: "رزرو",
  inventoryAvailable: "قابل فروش",
  inventoryOfferedQuantity: "مقدار عرضه",
  inventoryOfferedHint: "مقدار عرضه شامل واحدهای رزروشده است؛ موجودی فیزیکی تغییر نمی‌کند.",
  inventoryAllocationExceedsPhysical: "مقدار عرضه نباید از موجودی فیزیکی بیشتر باشد.",
  inventoryLocation: "انبار",
  inventoryOnHand: "موجودی",
  inventoryReason: "دلیل اصلاح",
  inventoryReasonHint: "برای هر اصلاح موجودی الزامی است.",
  inventoryEmpty: "برای این آفر هنوز موجودی ثبت نشده است.",
  inventoryAddLocation: "افزودن انبار",
  inventoryRemoveLocation: "حذف انبار",
  inventoryNoEligible: "انبار قابل انتخاب در دسترس نیست.",
  inventoryUnavailable: "افزودن انبار جدید از این صفحه ممکن نیست.",
  inventoryProvisionNote: "موقعیت‌های انبار پس از ذخیره پیش‌نویس به‌صورت واقعی ایجاد می‌شوند.",
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
  ...shippingMessagesFa,
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
  roundingNearest: "نزدیک‌ترین مقدار",
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
  historyActor: "کاربر",
  historyKindCreated: "ایجاد",
  historyKindCommercial: "تغییر تجاری",
  historyKindPrice: "تغییر قیمت",
  historyKindInventory: "تغییر موجودی",
  historyKindAllocation: "تغییر مقدار عرضه",
  historyKindUnknown: "تغییر ثبت‌شده",
  historyKindFulfillment: "تغییر انجام ارسال",
  historyDesc: "تغییرات واقعی این آفر به ترتیب زمان.",
  historyEmpty: "تاریخچه‌ای برای این آفر در دسترس نیست.",
  perTabError: "ذخیره این بخش ناموفق بود.",
  dirtyTab: "تغییر ذخیره‌نشده",
  denied: "دسترسی مجاز نیست",
  deniedDetail: "این آفر متعلق به فروشنده دیگری است یا مجوز کافی وجود ندارد.",
  loadFailed: "خواندن آفر انجام نشد",
  channelMarketplace: "بازارگاه",
  channelDirect: "فروش مستقیم",
  channelAgency: "نمایندگی",
  channelCorporate: "سازمانی",
  channelAffiliate: "همکار فروش",
  channelApi: "API",
  sellerSession: "فروشنده نشست",
  imageAlt: "تصویر محصول",
  noImage: "بدون تصویر",
  productMetaSeparator: "·",
  variantCodeLabel: "کد تنوع",
  skuNone: "بدون کد",
  select: "انتخاب",
  selectSellerTitle: "انتخاب فروشنده",
  selectProductTitle: "انتخاب محصول",
  selectVariantTitle: "انتخاب تنوع محصول",
  changeProduct: "تغییر محصول",
  changeVariant: "تغییر تنوع",
  chooseProductFirst: "ابتدا محصول را انتخاب کنید.",
  productsEmpty: "محصول منتشرشده‌ای پیدا نشد.",
  variantsEmpty: "این محصول تنوع قابل عرضه ندارد.",
  singleVariantAuto: "این محصول یک تنوع قابل عرضه دارد و به‌صورت خودکار انتخاب شد.",
  productPickerError: "خواندن محصولات ممکن نشد.",
  variantPickerError: "خواندن تنوع‌ها ممکن نشد.",
  variantUnnamed: "تنوع بدون مشخصه",
  productResultCount: "محصول",
  variantCount: "تنوع",
  notSelected: "انتخاب نشده",
  search: "جست‌وجو",
  nextPage: "صفحه بعد",
  prevPage: "صفحه قبل",
  pageInfo: "صفحه",
  noResults: "نتیجه‌ای پیدا نشد.",
  pickerError: "خواندن فهرست ممکن نشد.",
  createNeedsIdentity: "ابتدا فروشنده، محصول و تنوع را انتخاب کنید.",
  duplicateActiveTitle: "آفر موجود است",
  duplicateActive: "برای این فروشنده، تنوع و کانال قبلاً آفر ثبت شده است. آفر موجود را باز کنید یا انتخاب را تغییر دهید.",
  viewExistingOffer: "مشاهده آفر موجود",
  conflictChecking: "بررسی آفر موجود…",
  validationAmount: "مبلغ نامعتبر است.",
  priceRequired: "برای ذخیره، مبلغ را وارد کنید یا تغییر قیمت را لغو کنید. مبلغ خالی صفر ثبت نمی‌شود.",
  validationDecimals: "تعداد اعشار مبلغ با ارز انتخابی سازگار نیست.",
  validationInventory: "موجودی نامعتبر است.",
  validationBelowReserved: "موجودی فیزیکی نباید کمتر از رزرو باشد.",
  validationReason: "دلیل اصلاح الزامی است.",
  validationQty: "مقدار خرید نامعتبر است.",
  fulfillmentProtected: "تغییر مسئولیت ارسال تا زمانی که تعهدات یا مقدار عرضه وجود دارد مجاز نیست.",
  validationStep: "مقدار با گام و دقت واحد محصول هم‌تراز نیست.",
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
  close: "Close",
  retry: "Retry",
  loading: "Loading…",
  unsaved: "Unsaved changes",
  saveErrorTitle: "Save failed",
  partialTitle: "Partial save",
  dirtyExitTitle: "Unsaved changes",
  dirtyExitText: "You have unsaved changes. Leaving will discard this session draft.",
  dirtyExitConfirm: "Leave without saving",
  dirtyExitCancel: "Return to form",
  saveError: "Your changes were kept; try again.",
  saved: "Changes saved.",
  noChanges: "Nothing to save.",
  partialSaved: "Some changes were saved; the remaining values are kept.",
  saveUnknown: "The connection dropped and the result is uncertain. Reload the state before retrying.",
  createUnknown: "The connection dropped and draft creation is uncertain. Reload the offer list; your selections are kept.",
  reloadState: "Reload state",
  concurrentChange: "Another session changed this offer. Reload the state first; your changes are kept.",
  draftRestored: "Local draft and unsaved selections were restored.",
  reloaded: "Server state reloaded; your unsaved changes are kept.",
  seller: "Seller",
  status: "Offer status",
  buy: "Purchase",
  buyable: "Buyable",
  buyDisplay: "Buyability (display)",
  buyabilityHint: "This indicator uses active status, price and stock; final confirmation happens in the cart.",
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
  channelPolicyPending: "Current system channel; the owning domain has not yet decided a selectable channel policy.",
  sellerHint: "Search by seller name or code with server paging.",
  sellerSessionHint: "The seller identity comes from your session.",
  offerCreatePermissionRequired: "The current seller account does not have permission to create offers.",
  productAdditionalInformation: "Additional product information",
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
  pricingRetired: "Retired",
  pricingRemove: "Remove price",
  pricingAddCurrency: "Add another currency",
  pricingRemoveConfirm: "Remove this currency price from sale? Its history will be retained.",
  historyKindPriceRetired: "Price removed",
  conflictUnavailable: "Could not check for an existing offer. Retry.",
  pricingValidity: "Validity",
  pricingCurrent: "Current price",
  pricingStoreCurrency: "Store default currency",
  pricingNoDecimals: "This currency has no decimals.",
  pricingLegacyDecimals: "This price has decimals; the stored value stays untouched until you change it.",
  pricingUnavailable: "The authorized currency list is unavailable.",
  pricingSelectCurrency: "Select currency",
  pricingSaveBeforeSwitch: "Save or revert the current price before switching currencies.",
  pricingNoPrices: "No price has been recorded for this offer yet.",
  pricingPrimaryPrefix: "Primary price",
  discountsTitle: "Discounts",
  discountsDesc: "Discounts related to this offer and their conditions.",
  discountsEmpty: "No discount is recorded for this offer.",
  discountsUnavailable: "Defining a discount for one offer is not available on this screen yet.",
  inventoryTitle: "Inventory",
  inventoryDesc: "Record each warehouse separately. Reserved and available are read-only.",
  inventoryPhysical: "Physical",
  inventoryReserved: "Reserved",
  inventoryAvailable: "Available",
  inventoryOfferedQuantity: "Offered quantity",
  inventoryOfferedHint: "Offered quantity includes held units and does not change physical stock.",
  inventoryAllocationExceedsPhysical: "Offered quantity must not exceed physical stock.",
  inventoryLocation: "Warehouse",
  inventoryOnHand: "On hand",
  inventoryReason: "Adjustment reason",
  inventoryReasonHint: "Required for every inventory adjustment.",
  inventoryEmpty: "No inventory is recorded for this offer yet.",
  inventoryAddLocation: "Add warehouse",
  inventoryRemoveLocation: "Remove warehouse",
  inventoryNoEligible: "No warehouse is available to select.",
  inventoryUnavailable: "Adding a new warehouse is not available on this screen.",
  inventoryProvisionNote: "Warehouse positions are created for real after the draft is saved.",
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
  ...shippingMessagesEn,
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
  roundingNearest: "Nearest",
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
  historyActor: "Actor",
  historyKindCreated: "Created",
  historyKindCommercial: "Commercial change",
  historyKindPrice: "Price change",
  historyKindInventory: "Inventory change",
  historyKindAllocation: "Offered quantity change",
  historyKindUnknown: "Recorded change",
  historyKindFulfillment: "Fulfillment change",
  historyDesc: "Real changes to this offer in time order.",
  historyEmpty: "No history is available for this offer.",
  perTabError: "This section failed to save.",
  dirtyTab: "Unsaved change",
  denied: "Access denied",
  deniedDetail: "This offer belongs to another seller or the actor lacks permission.",
  loadFailed: "Offer could not be read",
  channelMarketplace: "Marketplace",
  channelDirect: "Direct",
  channelAgency: "Agency",
  channelCorporate: "Corporate",
  channelAffiliate: "Affiliate",
  channelApi: "API",
  sellerSession: "Session seller",
  imageAlt: "Product image",
  noImage: "No image",
  productMetaSeparator: "·",
  variantCodeLabel: "Variant code",
  skuNone: "No code",
  select: "Select",
  selectSellerTitle: "Choose seller",
  selectProductTitle: "Choose product",
  selectVariantTitle: "Choose product variant",
  changeProduct: "Change product",
  changeVariant: "Change variant",
  chooseProductFirst: "Choose a product first.",
  productsEmpty: "No published product found.",
  variantsEmpty: "This product has no sellable variant.",
  singleVariantAuto: "This product has one sellable variant; it was selected automatically.",
  productPickerError: "Products could not be read.",
  variantPickerError: "Variants could not be read.",
  variantUnnamed: "Variant without attributes",
  productResultCount: "products",
  variantCount: "variants",
  notSelected: "Not selected",
  search: "Search",
  nextPage: "Next page",
  prevPage: "Previous page",
  pageInfo: "Page",
  noResults: "No result found.",
  pickerError: "The list could not be read.",
  createNeedsIdentity: "Choose seller, product and variant first.",
  duplicateActiveTitle: "An offer already exists",
  duplicateActive: "An offer already exists for this seller, variant and channel. Open it or change the selection.",
  viewExistingOffer: "View existing offer",
  conflictChecking: "Checking existing offer…",
  validationAmount: "Invalid amount.",
  priceRequired: "Enter an amount, or undo the price change. An empty amount is never saved as zero.",
  validationDecimals: "The amount exceeds the selected currency precision.",
  validationInventory: "Invalid stock value.",
  validationBelowReserved: "On-hand must not be below reserved.",
  validationReason: "Adjustment reason is required.",
  validationQty: "Invalid purchase quantity.",
  fulfillmentProtected: "Shipping responsibility cannot change while commitments or offered quantity exist.",
  validationStep: "Quantity does not match the product unit step and precision.",
  validationQtyOrder: "Minimum quantity must not exceed maximum.",
  validationReturnWindow: "Invalid custom window.",
  validationReturnRange: "Custom window is outside the allowed range.",
  sellerLocked: "Identity locked",
  variantLocked: "Variant locked",
  variantSelected: "Selected",
  dataUnavailable: "This section's data is unavailable.",
};

export const offerWorkspaceMessages: Record<"fa" | "en", OfferWorkspaceMessages> = { fa, en };

/** Maps validation/server codes to user-facing copy; never renders a raw machine code. */
export function offerErrorText(t: OfferWorkspaceMessages, code: string): string {
  switch (code) {
    case "offer.price.amount_required":
    case "offer.price.required":
      return t.priceRequired;
    case "pricing.amount.invalid":
    case "offer.price.invalid":
      return t.validationAmount;
    case "pricing.currency.invalid":
    case "pricing.currency.display_unit":
      return t.pricingUnavailable;
    case "offer.price.decimals_not_allowed":
      return t.validationDecimals;
    case "offer.inventory.invalid":
      return t.validationInventory;
    case "inventory.allocation.exceeds_physical":
      return t.inventoryAllocationExceedsPhysical;
    case "inventory.allocation.below_held":
    case "offer.inventory.below_reserved":
      return t.validationBelowReserved;
    case "offer.inventory.reason_required":
      return t.validationReason;
    case "offer.qty.min_invalid":
    case "offer.qty.step_invalid":
    case "offer.inventory.step_invalid":
      return t.validationStep;
    case "offer.qty.max_invalid":
      return t.validationQty;
    case "offer.qty.min_above_max":
      return t.validationQtyOrder;
    case "offer.return.window_invalid":
      return t.validationReturnWindow;
    case "offer.return.window_out_of_range":
      return t.validationReturnRange;
    case "offer.fulfillment.change_protected":
      return t.fulfillmentProtected;
    case "inventory.allocation.revision_conflict":
    case "inventory.position.revision_conflict":
    case "concurrency.conflict":
      return t.concurrentChange;
    case "offer.buyable.no_price":
      return t.buyableReasonNoPrice;
    case "offer.buyable.no_stock":
      return t.buyableReasonNoStock;
    case "offer.buyable.inactive":
      return t.buyableReasonInactive;
    case "offer.lifecycle.archived_locked":
      return t.archivedLocked;
    case "seller.identity.missing":
      return t.sellerSession;
    case "host-unreachable":
      return t.dataUnavailable;
    case "seller.authorization.denied":
    case "admin.authorization.denied":
    case "denied":
      return t.denied;
    default:
      return t.saveError;
  }
}
