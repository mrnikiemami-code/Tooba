/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — مدل نابِ پیش‌نمایش طراحی سرویس ارسال.
 *
 * این فایل **قرارداد تولید نیست**:
 * - هیچ DTO سرور، هیچ پورت/آداپتور API، هیچ موتور قیمت‌گذاری و هیچ قاعدهٔ مالی در آن ساخته نمی‌شود؛
 * - همهٔ داده‌ها «نمونه» و در حافظه‌اند و در هیچ مرز ذخیره‌سازی یا شبکه‌ای استفاده نمی‌شوند؛
 * - فقط یک view model تایپ‌شده با شناسه‌های پایدار برای بازبینی ظاهر است.
 *
 * قواعد ذاتی مدل هدف که همین‌جا نمایانده می‌شوند (بدون پیاده‌سازی رفتار تجاری):
 * - جغرافیا یک‌بار در «گروه مناطق» انتخاب می‌شود و ردیف‌های شرط/هزینه جغرافیا را تکرار نمی‌کنند؛
 * - شرط‌های یک ردیف با هم (AND) و ردیف‌ها جایگزین یکدیگرند؛
 * - کرانهٔ آغاز شامل و کرانهٔ پایان غیرشامل است؛
 * - والد/فرزند و تکرار در همان مالک/روش غیرمجاز است و حذف/جایگزینی خودکار انجام نمی‌شود.
 *
 * هیچ عبارت فارسی قابل‌نمایشی در این فایل نیست؛ متن‌ها در `preview-i18n.ts` هستند.
 */

/** زبان رابط پیش‌نمایش. همان دو locale پیکربندی‌شدهٔ مسیر موجود؛ صرفاً fixture برچسب‌دار. */
export const PREVIEW_INTERFACE_LOCALES = ["fa", "en"] as const;
export type PreviewLocale = (typeof PREVIEW_INTERFACE_LOCALES)[number];

/** جهت نوشتار از زبان رابط؛ مستقل از ارز و بازار. */
export function previewDirection(locale: PreviewLocale): "rtl" | "ltr" {
  return locale === "fa" ? "rtl" : "ltr";
}

/** ترتیب نمایش زبان‌های محتوا؛ fixture صریح پیش‌نمایش، نه تنظیمات زبان فروشگاه. */
export const PREVIEW_CONTENT_LANGUAGE_IDS = ["fa", "en"] as const;
export type PreviewContentLanguageId = (typeof PREVIEW_CONTENT_LANGUAGE_IDS)[number];

/** متن چندزبانهٔ محتوایی؛ کلید = شناسهٔ زبان، مقدار = متن همان زبان. */
export type PreviewLocalizedText = Readonly<Record<string, string>>;

/** ارز نمونه از context فروشگاه؛ در پیش‌نمایش قابل ویرایش نیست و با زبان رابط تغییر نمی‌کند. */
export const PREVIEW_STORE_CURRENCY = "IRR";

export type PreviewShippingTypeId = "ordinary" | "heavy" | "extra-heavy" | "grocery";

export const PREVIEW_SHIPPING_TYPE_IDS: readonly PreviewShippingTypeId[] = [
  "ordinary",
  "heavy",
  "extra-heavy",
  "grocery",
];

export type PreviewFreightPayerMode = "prepaid" | "collect";
export type PreviewCalculationMode = "manual" | "carrier" | "free";
export type PreviewMethodStatus = "active" | "inactive";
/** مالکِ نمونهٔ روش؛ تعارض مناطق فقط داخل همین مالک و همان روش بررسی می‌شود. */
export type PreviewOwnerKind = "marketplace" | "seller";

export type PreviewCarrier = {
  id: string;
  labels: PreviewLocalizedText;
};

/** شرکت‌های حمل نمونه؛ فقط برای نمایش انتخاب‌گر حالت خودکار. */
export const PREVIEW_CARRIERS: readonly PreviewCarrier[] = [
  { id: "carrier-post", labels: { fa: "پست پیشتاز", en: "Post Express" } },
  { id: "carrier-tipax", labels: { fa: "تیپاکس", en: "Tipax" } },
  { id: "carrier-mahex", labels: { fa: "ماهکس", en: "Mahex" } },
];

/** برنامهٔ کاری مشترک روش؛ یک‌بار در «اطلاعات روش» و به‌صورت جمع‌شده نمایش داده می‌شود. */
export type PreviewSchedule = {
  /** روزهای فعال هفته؛ 0 = شنبه … 6 = جمعه. */
  weekdays: readonly number[];
  fromTime: string;
  toTime: string;
  /** آخرین مهلت سفارش برای همان روز کاری، به ساعت. */
  cutoffHours: number;
};

/** بازهٔ عددی: `from` شامل، `to` غیرشامل؛ هر دو تهی یعنی «این محور شرطی ندارد». */
export type PreviewRange = {
  from: number | null;
  to: number | null;
};

export type PreviewDelivery = {
  minDays: number;
  maxDays: number;
};

export type PreviewRateRow = {
  id: string;
  /** گرم؛ واحد ذخیرهٔ وزن گرم است و نمایش آن کیلوگرم/گرم با زبان رابط. */
  weightGrams: PreviewRange | null;
  itemCount: PreviewRange | null;
  /** مبلغ کالاهای همین گروه، با ارز context فروشگاه. */
  merchandiseAmount: PreviewRange | null;
  freight: number;
  packaging: number;
  delivery: PreviewDelivery;
};

/**
 * گروه مناطق: یک انتخاب جغرافیایی مشترک + چند ردیف شرط/هزینه.
 * عنوان گروه اختیاری و ترجمه‌پذیر است.
 */
export type PreviewRegionGroup = {
  id: string;
  titles: PreviewLocalizedText;
  places: readonly string[];
  rows: readonly PreviewRateRow[];
};

export type PreviewMethod = {
  id: string;
  /** شناسهٔ فنی پایدار نمونه — هرگز نام اصلی ردیف نیست. */
  code: string;
  ownerKind: PreviewOwnerKind;
  shippingTypeId: PreviewShippingTypeId;
  names: PreviewLocalizedText;
  descriptions: PreviewLocalizedText;
  freightPayerMode: PreviewFreightPayerMode;
  calculationMode: PreviewCalculationMode;
  status: PreviewMethodStatus;
  carrierId: string | null;
  schedule: PreviewSchedule;
  regionGroups: readonly PreviewRegionGroup[];
};

export type PreviewPlaceLevel = "country" | "province" | "city" | "area";

export type PreviewPlace = {
  id: string;
  level: PreviewPlaceLevel;
  parentId: string | null;
  labels: PreviewLocalizedText;
};

/**
 * شجره‌نامهٔ صریح و کوچک نمونه. هیچ‌جا از برچسب یا split کد فنی استنتاج نمی‌شود.
 */
export const PREVIEW_PLACES: readonly PreviewPlace[] = [
  { id: "place-ir", level: "country", parentId: null, labels: { fa: "ایران", en: "Iran" } },
  { id: "place-ir-23", level: "province", parentId: "place-ir", labels: { fa: "تهران", en: "Tehran" } },
  { id: "place-ir-25", level: "province", parentId: "place-ir", labels: { fa: "قم", en: "Qom" } },
  { id: "place-ir-10", level: "province", parentId: "place-ir", labels: { fa: "اصفهان", en: "Isfahan" } },
  { id: "place-ir-23-thr", level: "city", parentId: "place-ir-23", labels: { fa: "تهران", en: "Tehran" } },
  { id: "place-ir-25-qom", level: "city", parentId: "place-ir-25", labels: { fa: "قم", en: "Qom" } },
  { id: "place-ir-10-glp", level: "city", parentId: "place-ir-10", labels: { fa: "گلپایگان", en: "Golpayegan" } },
  { id: "place-ir-23-thr-1", level: "area", parentId: "place-ir-23-thr", labels: { fa: "ناحیه ۱ تهران", en: "Tehran Area 1" } },
  { id: "place-ir-23-thr-2", level: "area", parentId: "place-ir-23-thr", labels: { fa: "ناحیه ۲ تهران", en: "Tehran Area 2" } },
];

const PLACE_INDEX = new Map(PREVIEW_PLACES.map((place) => [place.id, place]));

export function previewPlace(placeId: string): PreviewPlace | undefined {
  return PLACE_INDEX.get(placeId);
}

export function previewPlaceLabel(placeId: string, locale: PreviewLocale): string {
  const place = PLACE_INDEX.get(placeId);
  if (!place) return "";
  return place.labels[locale] ?? place.labels.fa ?? "";
}

/** زنجیرهٔ نیاکان از نزدیک‌ترین والد تا کشور؛ خالی برای ریشه. */
export function previewPlaceAncestors(placeId: string): string[] {
  const chain: string[] = [];
  let current = PLACE_INDEX.get(placeId)?.parentId ?? null;
  let guard = 0;
  while (current && guard < 16) {
    chain.push(current);
    current = PLACE_INDEX.get(current)?.parentId ?? null;
    guard += 1;
  }
  return chain;
}

/** مسیر خوانای کامل (کشور › استان › شهر › ناحیه) برای نمایش به‌جای کد فنی. */
export function previewPlacePath(placeId: string, locale: PreviewLocale): string[] {
  const chain = [...previewPlaceAncestors(placeId)].reverse().map((id) => previewPlaceLabel(id, locale));
  return [...chain, previewPlaceLabel(placeId, locale)].filter((part) => part.length > 0);
}

export function previewChildPlaces(parentId: string): PreviewPlace[] {
  return PREVIEW_PLACES.filter((place) => place.parentId === parentId);
}

export function previewPlacesByLevel(level: PreviewPlaceLevel): PreviewPlace[] {
  return PREVIEW_PLACES.filter((place) => place.level === level);
}

/** نوع تعارض انتخاب جغرافیایی در همان روش/مالک. */
export type PreviewPlaceConflictKind = "duplicate" | "covers" | "covered";

export type PreviewPlaceConflict = {
  kind: PreviewPlaceConflictKind;
  /** منطقهٔ انتخاب‌شده‌ای که تعارض را ایجاد کرده است. */
  placeId: string;
};

/**
 * آیا افزودن `candidateId` به مجموعهٔ انتخاب‌شدهٔ همان روش مجاز است.
 * - تکرار ممنوع؛
 * - منطقهٔ والدِ یک انتخاب موجود ممنوع (همهٔ زیرمجموعه‌ها پوشش داده شده‌اند)؛
 * - منطقهٔ فرزندِ یک انتخاب موجود ممنوع.
 * هیچ حذف/تبدیل خودکاری پیشنهاد نمی‌شود؛ فقط علت برگردانده می‌شود.
 */
export function previewPlaceConflict(
  selectedIds: readonly string[],
  candidateId: string,
  places: readonly PreviewPlace[] = PREVIEW_PLACES,
): PreviewPlaceConflict | null {
  if (!places.some((place) => place.id === candidateId)) return null;
  const selected = new Set(selectedIds);
  if (selected.has(candidateId)) return { kind: "duplicate", placeId: candidateId };

  const ancestors = new Set<string>();
  const index = new Map(places.map((place) => [place.id, place]));
  let cursor = index.get(candidateId)?.parentId ?? null;
  while (cursor) {
    ancestors.add(cursor);
    cursor = index.get(cursor)?.parentId ?? null;
  }

  for (const placeId of selected) {
    // انتخاب موجود، والدِ کاندید است ⇒ کاندید قبلاً پوشش داده شده است.
    if (ancestors.has(placeId)) return { kind: "covers", placeId };
    // کاندید، والدِ انتخاب موجود است ⇒ کاندید کل زیرمجموعه را پوشش می‌دهد.
    let walker = index.get(placeId)?.parentId ?? null;
    while (walker) {
      if (walker === candidateId) return { kind: "covered", placeId };
      walker = index.get(walker)?.parentId ?? null;
    }
  }
  return null;
}

/** همهٔ مناطق انتخاب‌شدهٔ یک روش، بدون توجه به گروه (دامنهٔ منع تعارض). */
export function previewMethodSelectedPlaces(method: PreviewMethod): string[] {
  return method.regionGroups.flatMap((group) => group.places);
}

const cloneRange = (range: PreviewRange | null): PreviewRange | null =>
  range ? { from: range.from, to: range.to } : null;

const cloneRow = (row: PreviewRateRow): PreviewRateRow => ({
  id: row.id,
  weightGrams: cloneRange(row.weightGrams),
  itemCount: cloneRange(row.itemCount),
  merchandiseAmount: cloneRange(row.merchandiseAmount),
  freight: row.freight,
  packaging: row.packaging,
  delivery: { minDays: row.delivery.minDays, maxDays: row.delivery.maxDays },
});

/** کپی مستقل و قابل‌ویرایش از یک روش؛ پیش‌نویس هرگز به fixture مشترک اشاره نمی‌کند. */
export function clonePreviewMethod(method: PreviewMethod): PreviewMethod {
  return {
    id: method.id,
    code: method.code,
    ownerKind: method.ownerKind,
    shippingTypeId: method.shippingTypeId,
    names: { ...method.names },
    descriptions: { ...method.descriptions },
    freightPayerMode: method.freightPayerMode,
    calculationMode: method.calculationMode,
    status: method.status,
    carrierId: method.carrierId,
    schedule: { ...method.schedule, weekdays: [...method.schedule.weekdays] },
    regionGroups: method.regionGroups.map((group) => ({
      id: group.id,
      titles: { ...group.titles },
      places: [...group.places],
      rows: group.rows.map(cloneRow),
    })),
  };
}

const RANGE_NONE: PreviewRange = { from: null, to: null };

export const PREVIEW_EMPTY_RANGE: PreviewRange = RANGE_NONE;

export function previewRangeIsEmpty(range: PreviewRange | null): boolean {
  return !range || (range.from === null && range.to === null);
}

/**
 * ناسازگاری ساختاری بازه: آغاز منفی، پایان ≤ آغاز، یا مقدار غیرعدد.
 * این اعتبارسنجی «شکل بازه» است، نه موتور قیمت‌گذاری.
 */
export function previewRangeError(
  range: PreviewRange | null,
  options: { integer?: boolean; allowZeroFrom?: boolean } = {},
): "negative" | "inverted" | "notInteger" | null {
  if (previewRangeIsEmpty(range)) return null;
  const { from, to } = range as PreviewRange;
  const values = [from, to].filter((value): value is number => value !== null);
  if (values.some((value) => !Number.isFinite(value))) return "negative";
  if (values.some((value) => value < 0)) return "negative";
  if (options.integer && values.some((value) => !Number.isInteger(value))) return "notInteger";
  if (!options.allowZeroFrom && from !== null && to !== null && to <= from) return "inverted";
  if (from !== null && to !== null && to < from) return "inverted";
  return null;
}

/**
 * ارزیابی یک کاندید در انتخاب‌گر جغرافیا.
 *
 * «مرور» و «افزودن» دو حالت جدا هستند: والدِ یک انتخاب موجود فقط «افزودنی نیست»،
 * اما پیمایش از آن برای رسیدن به زیرمجموعهٔ بی‌تعارض همچنان مجاز است.
 */
export type PreviewPlaceCandidateState = {
  placeId: string;
  /** فقط وقتی تعارضی نیست؛ تعارض مانع افزودن است، نه مانع پیمایش. */
  addable: boolean;
  conflict: PreviewPlaceConflict | null;
};

export function previewPlaceCandidateState(
  selectedIds: readonly string[],
  candidateId: string,
  places: readonly PreviewPlace[] = PREVIEW_PLACES,
): PreviewPlaceCandidateState {
  const conflict = previewPlaceConflict(selectedIds, candidateId, places);
  return { placeId: candidateId, addable: conflict === null, conflict };
}

/** ورودی خام فرم ردیف؛ وزن با واحد نمایش کیلوگرم و بقیه با واحد خودشان. */
export type PreviewRowRawInputs = {
  weightFromKg: string;
  weightToKg: string;
  countFrom: string;
  countTo: string;
  amountFrom: string;
  amountTo: string;
  freight: string;
  packaging: string;
  deliveryMin: string;
  deliveryMax: string;
};

export type PreviewRowInputIssue = "notNumber" | "negative" | "notInteger" | "inverted";

export type PreviewRowInputErrors = {
  weight: PreviewRowInputIssue | null;
  count: PreviewRowInputIssue | null;
  amount: PreviewRowInputIssue | null;
  delivery: PreviewRowInputIssue | null;
  freight: PreviewRowInputIssue | null;
  packaging: PreviewRowInputIssue | null;
};

type PreviewRawNumber = { state: "empty" | "value" | "invalid"; value: number | null };

/**
 * خواندن خام یک ورودی، پیش از هر گرد کردن/مقیاس‌بندی.
 * مقدار تهی «شرط ندارد» است، نه صفر.
 */
function previewRawNumber(text: string): PreviewRawNumber {
  const trimmed = text.trim();
  if (trimmed.length === 0) return { state: "empty", value: null };
  const parsed = Number(trimmed);
  if (!Number.isFinite(parsed)) return { state: "invalid", value: null };
  return { state: "value", value: parsed };
}

/** خطای یک بازهٔ خام؛ ابتدا عدد بودن، سپس علامت، سپس صحیح بودن و در پایان ترتیب کرانه‌ها. */
function previewRawRangeIssue(fromText: string, toText: string, integer: boolean): PreviewRowInputIssue | null {
  const from = previewRawNumber(fromText);
  const to = previewRawNumber(toText);
  if (from.state === "invalid" || to.state === "invalid") return "notNumber";
  const values = [from.value, to.value].filter((value): value is number => value !== null);
  if (values.some((value) => value < 0)) return "negative";
  if (integer && values.some((value) => !Number.isInteger(value))) return "notInteger";
  if (from.value !== null && to.value !== null && to.value <= from.value) return "inverted";
  return null;
}

function previewRawScalarIssue(text: string, integer: boolean): PreviewRowInputIssue | null {
  const parsed = previewRawNumber(text);
  if (parsed.state === "invalid") return "notNumber";
  if (parsed.value === null) return null;
  if (parsed.value < 0) return "negative";
  if (integer && !Number.isInteger(parsed.value)) return "notInteger";
  return null;
}

/**
 * اعتبارسنجی همگام ورودی خام ردیف — مستقل از اینکه کاربر خطا را دیده باشد.
 * اولین «ثبت ردیف» با مقادیر نامعتبر باید همان لحظه رد شود، نه بر اساس رندر قبلی.
 */
export function previewRowInputErrors(raw: PreviewRowRawInputs): PreviewRowInputErrors {
  const min = previewRawNumber(raw.deliveryMin);
  const max = previewRawNumber(raw.deliveryMax);
  let delivery: PreviewRowInputIssue | null = null;
  if (min.state === "invalid" || max.state === "invalid") {
    delivery = "notNumber";
  } else {
    const days = [min.value, max.value].filter((value): value is number => value !== null);
    if (days.some((value) => value < 0)) delivery = "negative";
    else if (days.some((value) => !Number.isInteger(value))) delivery = "notInteger";
    else if (min.value !== null && max.value !== null && max.value < min.value) delivery = "inverted";
  }
  return {
    weight: previewRawRangeIssue(raw.weightFromKg, raw.weightToKg, false),
    count: previewRawRangeIssue(raw.countFrom, raw.countTo, true),
    amount: previewRawRangeIssue(raw.amountFrom, raw.amountTo, true),
    delivery,
    freight: previewRawScalarIssue(raw.freight, true),
    packaging: previewRawScalarIssue(raw.packaging, true),
  };
}

export function previewRowInputsAreValid(errors: PreviewRowInputErrors): boolean {
  return Object.values(errors).every((issue) => issue === null);
}

function previewRawRange(fromText: string, toText: string, scale: number): PreviewRange | null {
  const from = previewRawNumber(fromText).value;
  const to = previewRawNumber(toText).value;
  if (from === null && to === null) return null;
  return { from: from === null ? null : Math.round(from * scale), to: to === null ? null : Math.round(to * scale) };
}

/**
 * ساخت ردیف از ورودی خام؛ فقط پس از اعتبارسنجی کامل صدا زده می‌شود.
 * تبدیل کیلوگرم ← گرم در همین‌جا با همان دقت پشتیبانی‌شده انجام می‌شود و هیچ قاعدهٔ مالی تعریف نمی‌کند.
 */
export function previewRowFromRawInputs(raw: PreviewRowRawInputs, id: string): PreviewRateRow {
  const min = previewRawNumber(raw.deliveryMin).value;
  const max = previewRawNumber(raw.deliveryMax).value;
  const minDays = min ?? max ?? 1;
  const maxDays = Math.max(max ?? min ?? 1, minDays);
  return {
    id,
    weightGrams: previewRawRange(raw.weightFromKg, raw.weightToKg, 1000) ?? previewEmptyRange(),
    itemCount: previewRawRange(raw.countFrom, raw.countTo, 1) ?? previewEmptyRange(),
    merchandiseAmount: previewRawRange(raw.amountFrom, raw.amountTo, 1) ?? previewEmptyRange(),
    freight: previewRawNumber(raw.freight).value ?? 0,
    packaging: previewRawNumber(raw.packaging).value ?? 0,
    delivery: { minDays, maxDays },
  };
}

/** آیا هر دو بازه در همان محور هم‌پوشانی دارند (فقط برای نمایش نمونهٔ تعارض، نه محاسبه). */
export function previewRangesOverlap(a: PreviewRange | null, b: PreviewRange | null): boolean {
  if (previewRangeIsEmpty(a) || previewRangeIsEmpty(b)) return false;
  const left = a as PreviewRange;
  const right = b as PreviewRange;
  const aFrom = left.from ?? Number.NEGATIVE_INFINITY;
  const aTo = left.to ?? Number.POSITIVE_INFINITY;
  const bFrom = right.from ?? Number.NEGATIVE_INFINITY;
  const bTo = right.to ?? Number.POSITIVE_INFINITY;
  return aFrom < bTo && bFrom < aTo;
}

/** ترتیب پایدار زبان‌های محتوا برای زبانه‌ها و برچسب وضعیت. */
export function previewContentLanguages(): readonly PreviewContentLanguageId[] {
  return PREVIEW_CONTENT_LANGUAGE_IDS;
}

/** متن یک زبان؛ نبود ترجمه «خالی صادقانه» است و با زبان دیگر پر نمی‌شود. */
export function previewTextFor(text: PreviewLocalizedText, languageId: string): string {
  const value = text[languageId];
  return typeof value === "string" ? value : "";
}

/**
 * وضعیت ترجمهٔ یک فیلد در زبان‌های محتوا.
 * زبان پیش‌فرض (نخستین زبان fixture) مرجع است و بقیه از آن fallback نمی‌گیرند.
 */
export type PreviewTranslationState = {
  languageId: PreviewContentLanguageId;
  value: string;
  translated: boolean;
  isReference: boolean;
};

export function previewTranslationStates(text: PreviewLocalizedText): PreviewTranslationState[] {
  const reference = PREVIEW_CONTENT_LANGUAGE_IDS[0];
  return PREVIEW_CONTENT_LANGUAGE_IDS.map((languageId) => {
    const value = previewTextFor(text, languageId).trim();
    return { languageId, value, translated: value.length > 0, isReference: languageId === reference };
  });
}

export function previewTranslationSummary(
  text: PreviewLocalizedText,
): { translated: number; missing: number; total: number } {
  const states = previewTranslationStates(text);
  const translated = states.filter((state) => state.translated).length;
  return { translated, missing: states.length - translated, total: states.length };
}

let previewSequence = 0;

/** شناسهٔ نمونهٔ یکتای درون‌حافظه‌ای برای ردیف/گروه تازه. */
export function previewNextId(prefix: string): string {
  previewSequence += 1;
  return `${prefix}-${previewSequence}`;
}

export function previewEmptyRange(): PreviewRange {
  return { from: null, to: null };
}

export function previewNewRateRow(): PreviewRateRow {
  return {
    id: previewNextId("row"),
    weightGrams: previewEmptyRange(),
    itemCount: previewEmptyRange(),
    merchandiseAmount: previewEmptyRange(),
    freight: 0,
    packaging: 0,
    delivery: { minDays: 1, maxDays: 3 },
  };
}

/** ساخت روش تازه در حافظه؛ وضعیت ابتدایی «غیرفعال» و بدون منطقه. */
export function previewNewMethod(id: string, ownerKind: PreviewOwnerKind = "marketplace"): PreviewMethod {
  return {
    id,
    code: "preview:sample",
    ownerKind,
    shippingTypeId: "ordinary",
    names: { fa: "", en: "" },
    descriptions: { fa: "", en: "" },
    freightPayerMode: "prepaid",
    calculationMode: "manual",
    status: "inactive",
    carrierId: null,
    schedule: { weekdays: [0, 1, 2, 3, 4], fromTime: "09:00", toTime: "18:00", cutoffHours: 4 },
    regionGroups: [],
  };
}
