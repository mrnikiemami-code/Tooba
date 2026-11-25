/**
 * Offer locale/formatter layer (TB-296-R8).
 *
 * The active locale and direction come from the project language registry. UI chrome uses the
 * component message catalog; numbers, money, dates and currency names use the active culture.
 * A third language can be supplied by an isolated fixture without changing the live registry.
 */

export interface OfferLocaleDefinition {
  code: string;
  culture: string;
  language: string;
  direction: "rtl" | "ltr";
  calendar: "jalali" | "gregorian";
}

export interface OfferFormatters {
  locale: OfferLocaleDefinition;
  integer: (value: number | null | undefined) => string;
  number: (value: number | null | undefined, maximumFractionDigits?: number) => string;
  digitsIn: (value: string | number) => string;
  money: (amount: number | null | undefined, currency: string) => string;
  currencyName: (currency: string) => string;
  regionName: (region: string) => string;
  date: (iso: string | null | undefined) => string;
  dateTime: (iso: string | null | undefined) => string;
  amountInput: (raw: string) => string;
}

export const OFFER_LOCALE_FA: OfferLocaleDefinition = {
  code: "fa-IR",
  culture: "fa-IR",
  language: "fa",
  direction: "rtl",
  calendar: "jalali",
};

export const OFFER_LOCALE_EN: OfferLocaleDefinition = {
  code: "en-US",
  culture: "en-US",
  language: "en",
  direction: "ltr",
  calendar: "gregorian",
};

export function offerLocaleFromAppLocale(locale: "fa" | "en"): OfferLocaleDefinition {
  return locale === "en" ? OFFER_LOCALE_EN : OFFER_LOCALE_FA;
}

export function offerLocaleFromRegistry(row: {
  code: string;
  culture: string;
  direction: "rtl" | "ltr";
  calendarDisplay: "jalali" | "gregorian";
}): OfferLocaleDefinition {
  return {
    code: row.code,
    culture: row.culture || row.code,
    language: (row.culture || row.code).split("-")[0] ?? row.code,
    direction: row.direction,
    calendar: row.calendarDisplay,
  };
}

export function createOfferFormatters(locale: OfferLocaleDefinition): OfferFormatters {
  const numberFormatter = (maximumFractionDigits: number, useGrouping = true) => {
    try {
      return new Intl.NumberFormat(locale.culture, { maximumFractionDigits, useGrouping });
    } catch {
      return new Intl.NumberFormat("en-US", { maximumFractionDigits, useGrouping });
    }
  };
  const digitFormatters = Array.from({ length: 10 }, (_, digit) => numberFormatter(0, false).format(digit));
  const regionNames = (() => {
    try {
      return new Intl.DisplayNames([locale.culture], { type: "region", fallback: "code" });
    } catch {
      return null;
    }
  })();
  const currencyNames = (() => {
    try {
      return new Intl.DisplayNames([locale.culture], { type: "currency", fallback: "code" });
    } catch {
      return null;
    }
  })();
  const currencyLabel = (currency: string): string => {
    const code = (currency || "IRR").toUpperCase();
    try {
      return currencyNames?.of(code) ?? code;
    } catch {
      return code;
    }
  };
  const dateFormatter = (withTime: boolean) => {
    const options: Intl.DateTimeFormatOptions = {
      calendar: locale.calendar === "jalali" ? "persian" : "gregory",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      ...(withTime ? { hour: "2-digit", minute: "2-digit" } : {}),
    };
    try {
      return new Intl.DateTimeFormat(locale.culture, options);
    } catch {
      return new Intl.DateTimeFormat("en-US", options);
    }
  };
  const decimalSeparator = (() => {
    try {
      return new Intl.NumberFormat(locale.culture).formatToParts(1.1).find((part) => part.type === "decimal")?.value ?? ".";
    } catch {
      return ".";
    }
  })();
  const dateFmt = dateFormatter(false);
  const dateTimeFmt = dateFormatter(true);

  return {
    locale,
    integer: (value) => (value == null || !Number.isFinite(value) ? "—" : numberFormatter(0).format(value)),
    number: (value, maximumFractionDigits = 3) =>
      value == null || !Number.isFinite(value) ? "—" : numberFormatter(maximumFractionDigits).format(value),
    digitsIn: (value) => String(value).replace(/\d/g, (digit) => digitFormatters[Number(digit)] ?? digit),
    money: (amount, currency) =>
      amount == null || !Number.isFinite(amount)
        ? "—"
        : `${numberFormatter(4).format(amount)} ${currencyLabel(currency)}`.trim(),
    currencyName: currencyLabel,
    regionName: (region) => {
      const code = (region || "IR").toUpperCase();
      try {
        return regionNames?.of(code) ?? code;
      } catch {
        return code;
      }
    },
    date: (iso) => (iso ? dateFmt.format(new Date(iso)) : "—"),
    dateTime: (iso) => (iso ? dateTimeFmt.format(new Date(iso)) : "—"),
    amountInput: (raw) => {
      const normalized = raw
        .replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)))
        .replace(/[٠-٩]/g, (digit) => String("٠١٢٣٤٥٦٧٨٩".indexOf(digit)))
        .replace(/[٬,\s]/g, "")
        .replace(/٫/g, ".");
      if (normalized.length === 0) return "";
      const match = /^(-?)(\d*)(?:\.(\d*))?$/.exec(normalized);
      if (!match) return raw;
      const sign = match[1] ?? "";
      const integer = match[2] || "0";
      const fraction = match[3];
      const grouped = numberFormatter(0).format(Number(integer));
      const localizedFraction = fraction == null ? "" : fraction.replace(/\d/g, (digit) => digitFormatters[Number(digit)] ?? digit);
      return `${sign}${grouped}${fraction == null ? "" : `${decimalSeparator}${localizedFraction}`}`;
    },
  };
}
export interface OfferListMessages {
  title: string;
  subtitle: string;
  breadcrumb: string;
  create: string;
  product: string;
  seller: string;
  sellerSku: string;
  channel: string;
  price: string;
  available: string;
  status: string;
  lastUpdated: string;
  actions: string;
  view: string;
  unavailable: string;
  offersNoun: string;
  statusDraft: string;
  statusActive: string;
  statusSuspended: string;
  statusArchived: string;
  channelDirect: string;
  channelMarketplace: string;
  channelAgency: string;
  channelCorporate: string;
  channelAffiliate: string;
  channelApi: string;
  sellerLoadDenied: string;
  sellerLoadDeniedDetail: string;
  hostUnavailable: string;
  retry: string;
}

const faList: OfferListMessages = {
  title: "پیشنهادهای فروشندگان",
  subtitle: "ایجاد و مدیریت آفر فروشندگان",
  breadcrumb: "خانه / پیشنهادهای فروشندگان",
  create: "ایجاد آفر",
  product: "محصول",
  seller: "فروشنده",
  sellerSku: "کد کالا",
  channel: "کانال",
  price: "قیمت",
  available: "موجودی",
  status: "وضعیت",
  lastUpdated: "آخرین تغییر",
  actions: "عملیات",
  view: "مشاهده",
  unavailable: "ناموجود",
  offersNoun: "آفر",
  statusDraft: "پیش‌نویس",
  statusActive: "فعال",
  statusSuspended: "معلق",
  statusArchived: "بایگانی‌شده",
  channelDirect: "فروش مستقیم",
  channelMarketplace: "بازارگاه",
  channelAgency: "نمایندگی",
  channelCorporate: "سازمانی",
  channelAffiliate: "همکار فروش",
  channelApi: "API",
  sellerLoadDenied: "دسترسی مجاز نیست",
  sellerLoadDeniedDetail: "این حساب اجازه مشاهده این فروشگاه را ندارد.",
  hostUnavailable: "فروشگاه در دسترس نیست",
  retry: "تلاش دوباره",
};

const enList: OfferListMessages = {
  title: "Seller offers",
  subtitle: "Create and manage seller offers",
  breadcrumb: "Home / Seller offers",
  create: "Create offer",
  product: "Product",
  seller: "Seller",
  sellerSku: "Item code",
  channel: "Channel",
  price: "Price",
  available: "Stock",
  status: "Status",
  lastUpdated: "Last updated",
  actions: "Actions",
  view: "View",
  unavailable: "Unavailable",
  offersNoun: "offers",
  statusDraft: "Draft",
  statusActive: "Active",
  statusSuspended: "Suspended",
  statusArchived: "Archived",
  channelDirect: "Direct",
  channelMarketplace: "Marketplace",
  channelAgency: "Agency",
  channelCorporate: "Corporate",
  channelAffiliate: "Affiliate",
  channelApi: "API",
  sellerLoadDenied: "Access denied",
  sellerLoadDeniedDetail: "This actor cannot view this seller.",
  hostUnavailable: "Store is unavailable",
  retry: "Retry",
};

export const offerListMessages: Record<"fa" | "en", OfferListMessages> = { fa: faList, en: enList };

export function offerListMessagesFor(
  locale: "fa" | "en" | string,
  override?: Partial<OfferListMessages>,
): OfferListMessages {
  const base = offerListMessages[locale as "fa" | "en"] ?? faList;
  return override ? { ...base, ...override } : base;
}

export function channelLabel(messages: OfferListMessages, channel: string | null | undefined): string {
  switch ((channel || "").toLowerCase()) {
    case "direct":
      return messages.channelDirect;
    case "marketplace":
      return messages.channelMarketplace;
    case "agency":
      return messages.channelAgency;
    case "corporate":
      return messages.channelCorporate;
    case "affiliate":
      return messages.channelAffiliate;
    case "api":
      return messages.channelApi;
    default:
      return channel || "—";
  }
}

export function offerStatusLabel(messages: OfferListMessages, status: string): string {
  switch (status) {
    case "Draft":
      return messages.statusDraft;
    case "Active":
      return messages.statusActive;
    case "Suspended":
      return messages.statusSuspended;
    case "Archived":
      return messages.statusArchived;
    default:
      return status;
  }
}
