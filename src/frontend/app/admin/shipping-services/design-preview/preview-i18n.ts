/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — واژه‌نامهٔ پیش‌نمایش (fa/en) + قالب‌بندی locale-aware.
 *
 * محدودهٔ واژه‌نامه فقط کلیدهای همین پیش‌نمایش است و هیچ فرهنگ لغت مشترک/سراسری، سیاست ارز یا
 * پیام Backend را تغییر نمی‌دهد. زبان رابط و زبان محتوای در حال ترجمه دو چیز مستقل‌اند.
 *
 * عدد/پول/وزن/تاریخ با قواعد locale فرمت می‌شوند؛ هیچ ارز یا واحد هاردکدِ مستقل از context نیست.
 */

import {
  PREVIEW_STORE_CURRENCY,
  type PreviewLocale,
  type PreviewRange,
} from "./preview-model.ts";

export const previewMessagesFa = {
  /* ————————————————— shell ————————————————— */
  pageTitle: "سرویس ارسال — پیش‌نمایش طراحی",
  previewBadge: "پیش‌نمایش طراحی",
  sampleBadge: "دادهٔ نمونه",
  previewBanner:
    "این صفحه یک پیش‌نمایش طراحی است. همهٔ روش‌ها، مناطق و مبالغ نمونه‌اند، فقط در حافظهٔ همین صفحه نگه داشته می‌شوند و هیچ تغییری در سرور ذخیره نمی‌شود.",
  boundaryNote:
    "مرز پیش‌نمایش: اعتبارسنجی سرور، مجوز فروشنده، محاسبهٔ واقعی کرایه/مالیات و پوشش جغرافیایی سرور در این مرحله پیاده نشده‌اند.",
  interfaceLanguage: "زبان رابط",
  currencyLabel: "ارز",
  currencyNote: "ارز از بافت فروشگاه خوانده می‌شود و مستقل از زبان رابط است.",
  ownerLabel: "مالک ارسال",
  ownerMarketplace: "مارکت‌پلیس همین فروشگاه",
  ownerSeller: "فروشنده (نمونه)",
  ownerNote: "تعارض مناطق فقط داخل مناطق همان مالک و همان روش بررسی می‌شود.",

  /* ————————————————— list ————————————————— */
  listTitle: "روش‌های ارسال",
  listSubtitle:
    "هر روش یک ردیف مستقل است: نوع، نحوهٔ محاسبه، زمان پرداخت کرایه و وضعیت آن یک‌جا دیده می‌شود.",
  addMethod: "افزودن روش",
  colName: "نام روش",
  colType: "نوع ارسال",
  colCalculation: "نحوهٔ محاسبه",
  colFreightPayer: "پرداخت کرایه",
  colStatus: "وضعیت",
  colActions: "عملیات",
  statusActive: "فعال",
  statusInactive: "غیرفعال",
  actionView: "مشاهده",
  actionEdit: "ویرایش",
  groupCount: "گروه مناطق",
  rowCount: "ردیف نرخ",
  noMethods: "روشی وجود ندارد.",

  /* ————————————————— editor shell ————————————————— */
  backToList: "بازگشت به فهرست",
  editorNewTitle: "روش ارسال تازه",
  tabGeneral: "اطلاعات روش",
  tabRegions: "مناطق و هزینه‌ها",
  saveDraft: "اعمال در پیش‌نمایش",
  resetDraft: "بازگردانی نمونه",
  appliedToPreview: "فقط در پیش‌نمایش همین صفحه اعمال شد؛ چیزی به سرور ارسال نشد.",
  resetDone: "پیش‌نمایش به دادهٔ نمونهٔ اولیه بازگشت.",
  unsavedChanges: "تغییر اعمال‌نشده دارید.",
  discardAndGoBack: "بازگشت بدون اعمال تغییرات؟",
  discardAndChangeOwner: "با تغییر مالک، پیش‌نویس اعمال‌نشده و ورودی‌های ردیفِ باز کنار گذاشته می‌شود. ادامه می‌دهید؟",
  viewReadOnly: "حالت مشاهده — فیلدها قابل ویرایش نیستند.",
  sampleNotice: "دادهٔ نمونه",

  /* ————————————————— general tab ————————————————— */
  shippingType: "نوع ارسال",
  shippingTypeHint: "نوع عملیاتی روش است؛ نه بازهٔ قیمت و نه نام شرکت حمل.",
  typeOrdinary: "عادی",
  typeHeavy: "سنگین",
  typeExtraHeavy: "فوق‌سنگین",
  typeGrocery: "سوپرمارکتی",
  methodName: "نام روش ارسال",
  methodDescription: "توضیح روش",
  contentLanguage: "زبان محتوا",
  contentLanguageHint: "ویرایش هر زبان فقط همان زبان را تغییر می‌دهد.",
  translated: "ترجمه‌شده",
  notTranslated: "بدون ترجمه",
  referenceLanguage: "زبان مرجع",
  translationSummary: "{translated} از {total} زبان ترجمه شده",
  textRequiredError: "متن این زبان نمی‌تواند خالی بماند.",
  freightPayerTitle: "زمان پرداخت کرایه",
  freightPayerPrepaid: "پیش‌کرایه",
  freightPayerCollect: "پس‌کرایه",
  freightPayerHint: "پس‌کرایه یعنی پرداخت کرایه در زمان تحویل؛ نه پرداخت بهای کالا در محل.",
  calculationTitle: "نحوهٔ محاسبهٔ هزینه",
  calculationManual: "دستی",
  calculationCarrier: "خودکار از API حامل",
  calculationFree: "رایگان",
  calculationHint: "مستقل از زمان پرداخت کرایه انتخاب می‌شود.",
  manualTitle: "پیکربندی دستی",
  manualNote: "کرایه و بسته‌بندی در ردیف‌های مناطق و هزینه‌ها وارد می‌شود.",
  manualRowsHint: "{count} ردیف در {groups} گروه مناطق ثبت شده است.",
  carrierTitle: "انتخاب حامل",
  carrierSelect: "شرکت حمل",
  carrierStatus: "وضعیت اتصال",
  carrierStatusSample: "نمونه — اتصال واقعی بررسی نشده",
  carrierCoordinates:
    "برای محاسبهٔ خودکار، مختصات معتبر مبدأ و نقطهٔ انتخاب‌شدهٔ مقصد روی نقشه لازم است؛ نقشه و کلید API در این پیش‌نمایش پیاده نشده است.",
  freeTitle: "ارسال رایگان",
  freeNote: "کرایه و بسته‌بندی برای مشتری صفر می‌شود.",
  freeFunding: "تأمین‌کنندهٔ هزینه (نمونه)",
  freeFundingMarketplace: "مارکت‌پلیس",
  freeFundingSeller: "فروشنده",
  freePromotionNote:
    "آفر «ارسال رایگان» موضوع Promotion است و با حالت پایهٔ «رایگان» یکی نیست؛ این پیش‌نمایش هیچ آستانهٔ کمپین نمی‌سازد.",
  scheduleTitle: "برنامهٔ کاری مشترک",
  scheduleSummary: "{days} · {from} تا {to} · مهلت سفارش {cutoff} ساعت",
  scheduleExpand: "جزئیات برنامه",
  scheduleCollapse: "بستن جزئیات",
  scheduleDays: "روزهای کاری",
  scheduleFrom: "از ساعت",
  scheduleTo: "تا ساعت",
  scheduleCutoff: "مهلت سفارش پیش از شروع (ساعت)",
  scheduleSharedNote: "این برنامه برای همهٔ مناطق و ردیف‌های همین روش مشترک است و در ردیف تکرار نمی‌شود.",
  preparationNote: "زمان آماده‌سازی فروشنده از مدت حمل جداست و در این پیش‌نمایش مقدار ندارد.",
  weekdaySat: "شنبه",
  weekdaySun: "یکشنبه",
  weekdayMon: "دوشنبه",
  weekdayTue: "سه‌شنبه",
  weekdayWed: "چهارشنبه",
  weekdayThu: "پنجشنبه",
  weekdayFri: "جمعه",

  /* ————————————————— regions tab ————————————————— */
  regionGroupsTitle: "گروه‌های مناطق",
  regionGroupsHint:
    "جغرافیا یک‌بار در گروه انتخاب می‌شود؛ ردیف‌های شرط و هزینه زیر همان گروه، منطقه را تکرار نمی‌کنند.",
  addRegionGroup: "افزودن گروه مناطق",
  removeRegionGroup: "حذف گروه",
  regionGroupTitleLabel: "عنوان گروه (اختیاری)",
  groupPlaceCount: "{count} منطقه",
  editRegions: "ویرایش مناطق",
  pickerTitle: "انتخاب منطقه",
  pickerHint:
    "می‌توانید در هر سطح توقف کنید؛ افزودن یک والد همهٔ زیرمجموعه‌هایش را پوشش می‌دهد. انتخاب والد فقط برای مرور است و چیزی اضافه نمی‌کند؛ گزینه‌های «فقط مرور» برای رسیدن به زیرمجموعه‌های آزاد باز می‌مانند و فقط افزودن آن‌ها بسته است.",
  pickerLevelCountry: "کشور",
  pickerLevelProvince: "استان",
  pickerLevelCity: "شهر",
  pickerLevelArea: "ناحیهٔ خدمت",
  pickerAdd: "افزودن",
  pickerAdded: "افزوده‌شده",
  pickerBlockedDuplicate: "قبلاً اضافه شده است.",
  pickerBlockedCovers: "زیرمجموعهٔ «{place}» است که قبلاً اضافه شده؛ ابتدا آن را حذف کنید.",
  pickerBlockedCovered: "والدِ «{place}» است که قبلاً اضافه شده است.",
  pickerRemove: "حذف",
  pickerSelectedTitle: "مناطق انتخاب‌شده",
  pickerNoPlaces: "هنوز منطقه‌ای انتخاب نشده است.",
  pickerNoChildren: "زیرمجموعه‌ای در این سطح ثبت نشده است.",
  pickerBrowseOnly: "فقط مرور",
  pickerCandidateAddable: "این منطقه قابل افزودن است.",
  pickerExplanation: "حذف یا جایگزینی خودکار انجام نمی‌شود؛ فقط انتخاب‌های متعارض غیرفعال می‌شوند.",

  ratesTitle: "ردیف‌های شرط و هزینه",
  ratesAndNote: "شرط‌های یک ردیف با هم (AND) بررسی می‌شوند؛ ردیف‌ها جایگزین یکدیگرند و جمع نمی‌شوند.",
  ratesBoundsNote: "کرانهٔ آغاز شامل و کرانهٔ پایان غیرشامل است. محور خالی یعنی آن محور شرطی ندارد.",
  addRateRow: "افزودن ردیف",
  noRows: "برای این گروه هنوز ردیفی ثبت نشده است.",
  colConditionWeight: "وزن",
  colConditionCount: "تعداد اقلام",
  colConditionAmount: "مبلغ کالا",
  colFreight: "کرایهٔ حمل",
  colPackaging: "هزینهٔ بسته‌بندی",
  colDelivery: "مدت تحویل",
  colRowActions: "عملیات ردیف",
  rowEditorTitle: "ردیف شرط و هزینه",
  rowEditorSameGeography: "این ردیف از همان مناطق همین گروه استفاده می‌کند.",
  weightFrom: "وزن از (کیلوگرم)",
  weightTo: "وزن تا (غیرشامل، کیلوگرم)",
  countFrom: "تعداد از",
  countTo: "تعداد تا (غیرشامل)",
  amountFrom: "مبلغ کالا از",
  amountTo: "مبلغ کالا تا (غیرشامل)",
  freightField: "کرایهٔ حمل",
  packagingField: "هزینهٔ بسته‌بندی",
  deliveryMin: "کمترین روز تحویل",
  deliveryMax: "بیشترین روز تحویل",
  rowApply: "ثبت ردیف",
  rowCancel: "انصراف",
  rowClose: "بستن",
  rowRemove: "حذف ردیف",
  rowRemoveConfirm: "این ردیف نمونه از پیش‌نمایش حذف شود؟",
  rowErrorNegative: "مقدار نمی‌تواند منفی باشد.",
  rowErrorInverted: "کرانهٔ پایان باید بزرگ‌تر از کرانهٔ آغاز باشد.",
  rowErrorNotInteger: "این فیلد باید عدد صحیح باشد.",
  rowErrorNotNumber: "مقدار وارد‌شده عدد معتبر نیست.",
  rowErrorDelivery: "بیشترین روز تحویل نباید کمتر از کمترین باشد.",
  deliveryValue: "{min} تا {max} روز",

  /* ————————————————— units / empty ————————————————— */
  unitKilogram: "کیلوگرم",
  unitGram: "گرم",
  unitItem: "عدد",
  unitItemOne: "عدد",
  unitDay: "روز",
  unitDayOne: "روز",
  dash: "—",
  rangeLessThan: "کمتر از {to}",
  rangeAtLeast: "از {from} و بیشتر",
  rangeBetween: "{from} تا کمتر از {to}",
  noCondition: "بدون شرط",
} as const;

export type PreviewMessageKey = keyof typeof previewMessagesFa;

export const previewMessagesEn: Record<PreviewMessageKey, string> = {
  pageTitle: "Shipping — design preview",
  previewBadge: "Design preview",
  sampleBadge: "Sample data",
  previewBanner:
    "This screen is a design preview. Every method, region and amount is sample data held in this page only; nothing is saved to the server.",
  boundaryNote:
    "Preview boundary: server validation, seller authorization, real freight/tax calculation and server-side geographic coverage are not implemented in this step.",
  interfaceLanguage: "Interface language",
  currencyLabel: "Currency",
  currencyNote: "Currency comes from store context and stays independent of the interface language.",
  ownerLabel: "Shipping owner",
  ownerMarketplace: "This store's marketplace",
  ownerSeller: "Seller (sample)",
  ownerNote: "Region conflicts are checked only within the same owner and same method.",

  listTitle: "Shipping methods",
  listSubtitle:
    "Each method is an independent row: type, calculation mode, freight payment timing and status in one place.",
  addMethod: "Add method",
  colName: "Method name",
  colType: "Shipping type",
  colCalculation: "Calculation",
  colFreightPayer: "Freight payment",
  colStatus: "Status",
  colActions: "Actions",
  statusActive: "Active",
  statusInactive: "Inactive",
  actionView: "View",
  actionEdit: "Edit",
  groupCount: "Region groups",
  rowCount: "Rate rows",
  noMethods: "No methods yet.",

  backToList: "Back to list",
  editorNewTitle: "New shipping method",
  tabGeneral: "Method information",
  tabRegions: "Regions and costs",
  saveDraft: "Apply to preview",
  resetDraft: "Restore sample",
  appliedToPreview: "Applied to this page's preview only; nothing was sent to the server.",
  resetDone: "The preview is back to its original sample data.",
  unsavedChanges: "You have changes that were not applied.",
  discardAndGoBack: "Go back without applying your changes?",
  discardAndChangeOwner: "Changing the owner discards the unapplied draft and any open row input. Continue?",
  viewReadOnly: "View mode — fields are read-only.",
  sampleNotice: "Sample data",

  shippingType: "Shipping type",
  shippingTypeHint: "The method's operational type; not a price band and not a carrier name.",
  typeOrdinary: "Ordinary",
  typeHeavy: "Heavy",
  typeExtraHeavy: "Extra heavy",
  typeGrocery: "Grocery",
  methodName: "Method name",
  methodDescription: "Method description",
  contentLanguage: "Content language",
  contentLanguageHint: "Editing one language changes only that language.",
  translated: "Translated",
  notTranslated: "Not translated",
  referenceLanguage: "Reference language",
  translationSummary: "{translated} of {total} languages translated",
  textRequiredError: "This language's text cannot be empty.",
  freightPayerTitle: "Freight payment timing",
  freightPayerPrepaid: "Prepaid",
  freightPayerCollect: "Collect",
  freightPayerHint: "Collect means freight is paid on delivery; it is not cash payment for the goods.",
  calculationTitle: "Cost calculation",
  calculationManual: "Manual",
  calculationCarrier: "Automatic from carrier API",
  calculationFree: "Free",
  calculationHint: "Chosen independently of the freight payment timing.",
  manualTitle: "Manual configuration",
  manualNote: "Freight and packaging are entered in the region and cost rows.",
  manualRowsHint: "{count} rows across {groups} region groups.",
  carrierTitle: "Carrier selection",
  carrierSelect: "Carrier",
  carrierStatus: "Integration status",
  carrierStatusSample: "Sample — no real integration checked",
  carrierCoordinates:
    "Automatic calculation needs valid origin coordinates and the customer's picked destination point on a map; the map and API key are not implemented in this preview.",
  freeTitle: "Free shipping",
  freeNote: "Freight and packaging are zero for the customer.",
  freeFunding: "Cost funded by (sample)",
  freeFundingMarketplace: "Marketplace",
  freeFundingSeller: "Seller",
  freePromotionNote:
    "A free-shipping offer is a Promotion and is not the same as the base free mode; this preview creates no campaign threshold.",
  scheduleTitle: "Shared operating schedule",
  scheduleSummary: "{days} · {from} to {to} · order cutoff {cutoff} h",
  scheduleExpand: "Schedule details",
  scheduleCollapse: "Hide details",
  scheduleDays: "Working days",
  scheduleFrom: "From",
  scheduleTo: "To",
  scheduleCutoff: "Order cutoff before start (hours)",
  scheduleSharedNote: "This schedule is shared by every region and row of this method and is not repeated per row.",
  preparationNote: "Seller preparation time is separate from transit time and has no value in this preview.",
  weekdaySat: "Saturday",
  weekdaySun: "Sunday",
  weekdayMon: "Monday",
  weekdayTue: "Tuesday",
  weekdayWed: "Wednesday",
  weekdayThu: "Thursday",
  weekdayFri: "Friday",

  regionGroupsTitle: "Region groups",
  regionGroupsHint:
    "Geography is selected once per group; the condition and cost rows below never repeat the region.",
  addRegionGroup: "Add region group",
  removeRegionGroup: "Remove group",
  regionGroupTitleLabel: "Group title (optional)",
  groupPlaceCount: "{count} regions",
  editRegions: "Edit regions",
  pickerTitle: "Select regions",
  pickerHint:
    "You may stop at any level; adding a parent covers all of its descendants. Browsing a parent adds nothing by itself; options marked “browse only” stay reachable so you can get to free descendants, and only adding them is blocked.",
  pickerLevelCountry: "Country",
  pickerLevelProvince: "Province",
  pickerLevelCity: "City",
  pickerLevelArea: "Service area",
  pickerAdd: "Add",
  pickerAdded: "Added",
  pickerBlockedDuplicate: "Already added.",
  pickerBlockedCovers: "It is a descendant of “{place}”, which is already added; remove that first.",
  pickerBlockedCovered: "It is the parent of “{place}”, which is already added.",
  pickerRemove: "Remove",
  pickerSelectedTitle: "Selected regions",
  pickerNoPlaces: "No region selected yet.",
  pickerNoChildren: "No descendant registered at this level.",
  pickerBrowseOnly: "browse only",
  pickerCandidateAddable: "This region can be added.",
  pickerExplanation: "Nothing is removed or replaced automatically; only conflicting choices are disabled.",

  ratesTitle: "Condition and cost rows",
  ratesAndNote: "Conditions within one row are combined with AND; rows are alternatives and are never summed.",
  ratesBoundsNote: "The start bound is inclusive and the end bound is exclusive. An empty axis means no condition on it.",
  addRateRow: "Add row",
  noRows: "This group has no rows yet.",
  colConditionWeight: "Weight",
  colConditionCount: "Item count",
  colConditionAmount: "Merchandise amount",
  colFreight: "Freight charge",
  colPackaging: "Packaging charge",
  colDelivery: "Delivery duration",
  colRowActions: "Row actions",
  rowEditorTitle: "Condition and cost row",
  rowEditorSameGeography: "This row uses the same regions as this group.",
  weightFrom: "Weight from (kg)",
  weightTo: "Weight to (exclusive, kg)",
  countFrom: "Count from",
  countTo: "Count to (exclusive)",
  amountFrom: "Merchandise amount from",
  amountTo: "Merchandise amount to (exclusive)",
  freightField: "Freight charge",
  packagingField: "Packaging charge",
  deliveryMin: "Minimum delivery days",
  deliveryMax: "Maximum delivery days",
  rowApply: "Save row",
  rowCancel: "Cancel",
  rowClose: "Close",
  rowRemove: "Remove row",
  rowRemoveConfirm: "Remove this sample row from the preview?",
  rowErrorNegative: "Value cannot be negative.",
  rowErrorInverted: "The end bound must be greater than the start bound.",
  rowErrorNotInteger: "This field must be a whole number.",
  rowErrorNotNumber: "The entered value is not a valid number.",
  rowErrorDelivery: "Maximum delivery days cannot be less than the minimum.",
  deliveryValue: "{min}–{max} days",

  unitKilogram: "kg",
  unitGram: "g",
  unitItem: "items",
  unitItemOne: "item",
  unitDay: "days",
  unitDayOne: "day",
  dash: "—",
  rangeLessThan: "under {to}",
  rangeAtLeast: "{from} and above",
  rangeBetween: "{from} to under {to}",
  noCondition: "No condition",
};

export type PreviewMessages = Record<PreviewMessageKey, string>;

export function previewMessages(locale: PreviewLocale): PreviewMessages {
  return locale === "en" ? previewMessagesEn : previewMessagesFa;
}

/** جای‌گذاری امن {name}؛ مقدار نبود ⇒ همان placeholder حفظ نمی‌شود و رشتهٔ خالی می‌نشیند. */
export function previewFormat(template: string, values: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (_match, key: string) => {
    const value = values[key];
    return value === undefined || value === null ? "" : String(value);
  });
}

const numberFormatters = new Map<string, Intl.NumberFormat>();

function numberFormatter(locale: PreviewLocale, digits: number): Intl.NumberFormat {
  const tag = locale === "fa" ? "fa-IR" : "en-US";
  const key = `${tag}:${digits}`;
  let formatter = numberFormatters.get(key);
  if (!formatter) {
    formatter = new Intl.NumberFormat(tag, { maximumFractionDigits: digits, minimumFractionDigits: 0 });
    numberFormatters.set(key, formatter);
  }
  return formatter;
}

export function formatPreviewNumber(value: number, locale: PreviewLocale, digits = 0): string {
  if (!Number.isFinite(value)) return "";
  return numberFormatter(locale, digits).format(value);
}

/** مبلغ با ارز context فروشگاه؛ واحد نمایش فقط برچسب است و تبدیل FX انجام نمی‌شود. */
export function formatPreviewMoney(
  amount: number,
  locale: PreviewLocale,
  currency: string = PREVIEW_STORE_CURRENCY,
): string {
  const digits = formatPreviewNumber(amount, locale, 0);
  const unit = locale === "fa" ? currencyUnitFa(currency) : currency;
  return locale === "fa" ? `${digits} ${unit}` : `${currency} ${digits}`;
}

function currencyUnitFa(currency: string): string {
  // واحد نمایش فقط برای ارزی که context فروشگاه اعلام کرده است؛ هیچ ارزی استنتاج نمی‌شود.
  return currency === "IRR" ? "ریال" : currency;
}

/** برچسب واحد پول برای زبان رابط؛ فقط نمایش، بدون تبدیل FX. */
export function previewCurrencyUnitLabel(
  locale: PreviewLocale,
  currency: string = PREVIEW_STORE_CURRENCY,
): string {
  return locale === "fa" ? currencyUnitFa(currency) : currency;
}

/** نام خوانای زبان محتوا برای زبانه‌ها؛ کد فنی زبان به‌تنهایی نمایش داده نمی‌شود. */
export function previewContentLanguageLabel(languageId: string, locale: PreviewLocale): string {
  if (languageId === "fa") return locale === "fa" ? "فارسی" : "Persian";
  if (languageId === "en") return locale === "fa" ? "انگلیسی" : "English";
  return languageId;
}

/** گرم (واحد ذخیره) → نمایش کیلوگرم/گرم با زبان رابط. */
export function formatPreviewWeight(grams: number, locale: PreviewLocale): string {
  const messages = previewMessages(locale);
  if (!Number.isFinite(grams)) return messages.dash;
  if (grams >= 1000) {
    return `${formatPreviewNumber(grams / 1000, locale, 2)} ${messages.unitKilogram}`;
  }
  return `${formatPreviewNumber(grams, locale, 0)} ${messages.unitGram}`;
}

export type PreviewRangeUnit = "weight" | "count" | "amount" | "days";

/**
 * توصیف خوانای یک بازه با واحد درست و زبان رابط.
 * `null`/خالی ⇒ «بدون شرط» برای محور شرط، و «—» برای مدت تحویل.
 */
export function describePreviewRange(
  range: PreviewRange | null,
  locale: PreviewLocale,
  unit: PreviewRangeUnit,
): string {
  const messages = previewMessages(locale);
  if (!range || (range.from === null && range.to === null)) {
    return unit === "days" ? messages.dash : messages.noCondition;
  }
  const format = (value: number): string => {
    if (unit === "weight") return formatPreviewWeight(value, locale);
    if (unit === "amount") return formatPreviewMoney(value, locale);
    const singular = locale === "en" && Math.abs(value) === 1;
    if (unit === "days") return `${formatPreviewNumber(value, locale)} ${singular ? messages.unitDayOne : messages.unitDay}`;
    return `${formatPreviewNumber(value, locale)} ${singular ? messages.unitItemOne : messages.unitItem}`;
  };
  const { from, to } = range;
  if (from === null && to !== null) return previewFormat(messages.rangeLessThan, { to: format(to) });
  if (from !== null && to === null) return previewFormat(messages.rangeAtLeast, { from: format(from) });
  return previewFormat(messages.rangeBetween, { from: format(from ?? 0), to: format(to ?? 0) });
}

export function describePreviewDelivery(
  delivery: { minDays: number; maxDays: number },
  locale: PreviewLocale,
): string {
  const messages = previewMessages(locale);
  return previewFormat(messages.deliveryValue, {
    min: formatPreviewNumber(delivery.minDays, locale),
    max: formatPreviewNumber(delivery.maxDays, locale),
  });
}

/** ساعت با ارقام locale؛ قالب ثابت HH:mm است و تاریخ/تقویم اختراع نمی‌شود. */
export function formatPreviewTime(value: string, locale: PreviewLocale): string {
  const [hours, minutes] = value.split(":");
  const h = Number(hours);
  const m = Number(minutes);
  if (!Number.isFinite(h) || !Number.isFinite(m)) return value;
  const pad = (input: number) => String(input).padStart(2, "0");
  return `${pad(h)}:${pad(m)}`.replace(/\d/g, (digit) =>
    locale === "fa" ? String.fromCharCode(0x06f0 + Number(digit)) : digit,
  );
}

export const PREVIEW_WEEKDAY_KEYS = [
  "weekdaySat",
  "weekdaySun",
  "weekdayMon",
  "weekdayTue",
  "weekdayWed",
  "weekdayThu",
  "weekdayFri",
] as const satisfies readonly PreviewMessageKey[];

export function previewWeekdayLabel(day: number, locale: PreviewLocale): string {
  const key = PREVIEW_WEEKDAY_KEYS[day];
  return key ? previewMessages(locale)[key] : "";
}
