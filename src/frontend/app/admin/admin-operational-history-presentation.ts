export type OperationalHistoryLocale = "fa" | "en";

const TECHNICAL_TOKEN = /^[A-Z][A-Z0-9_]{2,}$/;

const PAYMENT_FAILURE_LABELS: Record<string, { fa: string; en: string }> = {
  GATEWAY_REJECTED: { fa: "ردشده توسط درگاه پرداخت", en: "Rejected by payment gateway" },
  GATEWAY_TIMEOUT: { fa: "پایان مهلت پاسخ درگاه پرداخت", en: "Payment gateway timed out" },
  GATEWAY_UNAVAILABLE: { fa: "درگاه پرداخت در دسترس نیست", en: "Payment gateway is unavailable" },
  GATEWAY_RATE_LIMITED: { fa: "محدودیت تعداد درخواست درگاه پرداخت", en: "Payment gateway rate limited" },
  GATEWAY_PENDING: { fa: "در انتظار پاسخ درگاه پرداخت", en: "Awaiting payment gateway response" },
  GATEWAY_UNKNOWN: { fa: "نتیجه درگاه پرداخت نامشخص است", en: "Payment gateway result is unknown" },
  GATEWAY_MISCONFIGURED: { fa: "پیکربندی درگاه پرداخت ناقص است", en: "Payment gateway is misconfigured" },
  MANUAL_DEPOSIT_REJECTED: { fa: "رد واریز", en: "Deposit rejected" },
  MANUAL_DEPOSIT_PENDING: { fa: "در انتظار تأیید واریز", en: "Awaiting deposit confirmation" },
};

const UNKNOWN_FA = "جزئیات این رویداد برای نمایش آماده نیست.";
const UNKNOWN_EN = "This event detail is not available as a display label.";

export function presentOperationalHistoryText(
  value: string | null | undefined,
  locale: OperationalHistoryLocale,
): string {
  if (value == null) {
    return "";
  }
  const trimmed = value.trim();
  if (!trimmed) {
    return "";
  }
  const mapped = PAYMENT_FAILURE_LABELS[trimmed];
  if (mapped) {
    return locale === "en" ? mapped.en : mapped.fa;
  }
  if (TECHNICAL_TOKEN.test(trimmed)) {
    return locale === "en" ? UNKNOWN_EN : UNKNOWN_FA;
  }
  return trimmed;
}

export function presentOperationalHistoryEntry(
  entry: { labelFa: string; labelEn: string; summaryFa?: string | null; summaryEn?: string | null },
  locale: OperationalHistoryLocale,
): { label: string; summary: string } {
  const rawLabel = locale === "en" ? (entry.labelEn || entry.labelFa) : entry.labelFa;
  const rawSummary = locale === "en" ? (entry.summaryEn || entry.summaryFa) : entry.summaryFa;
  return {
    label: presentOperationalHistoryText(rawLabel, locale),
    summary: presentOperationalHistoryText(rawSummary, locale),
  };
}
