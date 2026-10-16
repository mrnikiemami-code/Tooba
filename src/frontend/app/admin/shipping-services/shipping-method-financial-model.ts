/**
 * TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155 — pure model for the SERVER-authoritative financial
 * status of ONE canonical shipping method.
 *
 * The status is computed by the Shipping module from real rate/coverage rows and is never derived
 * here from `isActive`, from a label, or from a client-side split of the method code. This module
 * only maps the wire shape to display text and refuses to invent a "ready", "free" or "zero"
 * meaning that the server did not state.
 */

export type ShippingMethodFinancialState =
  | "needs-configuration"
  | "financially-configured"
  | "legacy-shared-unresolved"
  | "conflicting"
  /** The server answered with a state this build does not know — never treated as ready. */
  | "unknown";

export type ShippingMethodFinancialRule = {
  ratePolicyId: string;
  scopeLevel: string;
  chargeAmount: number;
  currencyCode: string;
  preparationDays: number;
  leadDays: number;
  freeAboveThreshold: number | null;
  isConditioned: boolean;
};

export type ShippingMethodFinancialStatus = {
  serviceCode: string;
  optionCode: string | null;
  state: ShippingMethodFinancialState;
  /** Only `fixed-amount` is provable from the current schema; anything else stays `none`. */
  mode: "none" | "fixed-amount" | "unknown";
  currencyCode: string | null;
  ruleCount: number;
  conditionedRuleCount: number;
  hasZeroChargeRule: boolean;
  activeServiceLevelRuleCount: number;
  activeSiblingOptionRuleCount: number;
  activeCoverageGrantCount: number;
  rules: ShippingMethodFinancialRule[];
  reasonCode: string;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function pick(row: Record<string, unknown>, camel: string, pascal: string): unknown {
  return row[camel] ?? row[pascal];
}

function optionalText(value: unknown): string | null {
  const raw = text(value).trim();
  return raw ? raw : null;
}

function intOrZero(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function mapState(value: unknown): ShippingMethodFinancialState {
  switch (text(value).trim()) {
    case "NeedsConfiguration":
      return "needs-configuration";
    case "FinanciallyConfigured":
      return "financially-configured";
    case "LegacySharedUnresolved":
      return "legacy-shared-unresolved";
    case "Conflicting":
      return "conflicting";
    default:
      return "unknown";
  }
}

function mapMode(value: unknown): ShippingMethodFinancialStatus["mode"] {
  switch (text(value).trim()) {
    case "FixedAmount":
      return "fixed-amount";
    case "None":
      return "none";
    default:
      return "unknown";
  }
}

function mapRule(raw: unknown): ShippingMethodFinancialRule | null {
  const row = asRecord(raw);
  if (!row) return null;
  return {
    ratePolicyId: text(pick(row, "ratePolicyId", "RatePolicyId")),
    scopeLevel: text(pick(row, "scopeLevel", "ScopeLevel")),
    chargeAmount: intOrZero(pick(row, "chargeAmount", "ChargeAmount")),
    currencyCode: text(pick(row, "currencyCode", "CurrencyCode")),
    preparationDays: intOrZero(pick(row, "preparationDays", "PreparationDays")),
    leadDays: intOrZero(pick(row, "leadDays", "LeadDays")),
    freeAboveThreshold: Number.isFinite(Number(pick(row, "freeAboveThreshold", "FreeAboveThreshold")))
      ? Number(pick(row, "freeAboveThreshold", "FreeAboveThreshold"))
      : null,
    isConditioned: Boolean(pick(row, "isConditioned", "IsConditioned")),
  };
}

/**
 * Maps the server payload — fail-closed. A malformed answer maps to `null` so the caller can show
 * "not readable" instead of a fabricated ready state.
 */
export function mapShippingMethodFinancialStatus(payload: unknown): ShippingMethodFinancialStatus | null {
  const row = asRecord(payload);
  if (!row) return null;
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode")).trim();
  const reasonCode = text(pick(row, "reasonCode", "ReasonCode")).trim();
  if (!serviceCode || !reasonCode) return null;

  const rules = pick(row, "rules", "Rules");
  return {
    serviceCode,
    optionCode: optionalText(pick(row, "optionCode", "OptionCode")),
    state: mapState(pick(row, "state", "State")),
    mode: mapMode(pick(row, "mode", "Mode")),
    currencyCode: optionalText(pick(row, "currencyCode", "CurrencyCode")),
    ruleCount: intOrZero(pick(row, "ruleCount", "RuleCount")),
    conditionedRuleCount: intOrZero(pick(row, "conditionedRuleCount", "ConditionedRuleCount")),
    hasZeroChargeRule: Boolean(pick(row, "hasZeroChargeRule", "HasZeroChargeRule")),
    activeServiceLevelRuleCount: intOrZero(pick(row, "activeServiceLevelRuleCount", "ActiveServiceLevelRuleCount")),
    activeSiblingOptionRuleCount: intOrZero(pick(row, "activeSiblingOptionRuleCount", "ActiveSiblingOptionRuleCount")),
    activeCoverageGrantCount: intOrZero(pick(row, "activeCoverageGrantCount", "ActiveCoverageGrantCount")),
    rules: (Array.isArray(rules) ? rules : [])
      .map(mapRule)
      .filter((x): x is ShippingMethodFinancialRule => Boolean(x)),
    reasonCode,
  };
}

/** Whether this status proves a method-owned money representation that Admin may display. */
export function hasProvenOwnedMoney(status: ShippingMethodFinancialStatus): boolean {
  return status.state === "financially-configured"
    && status.mode === "fixed-amount"
    && status.ruleCount > 0
    && Boolean(status.currencyCode);
}

/**
 * The single amount Admin may show, or `null` when no single amount is authoritative. Several
 * scoped/tiered rules are a rule set, not one number, so no number is invented for them.
 */
export function provenOwnedAmount(status: ShippingMethodFinancialStatus): number | null {
  if (!hasProvenOwnedMoney(status) || status.rules.length !== 1) return null;
  return status.rules[0]?.chargeAmount ?? null;
}

export type FinancialBadgeTone = "success" | "warning" | "danger" | "neutral";

export function financialStateLabelFa(state: ShippingMethodFinancialState): string {
  switch (state) {
    case "financially-configured":
      return "مالی: تنظیم‌شده";
    case "needs-configuration":
      return "مالی: نیازمند تنظیم";
    case "legacy-shared-unresolved":
      return "مالی: تعرفهٔ مشترک قدیمی";
    case "conflicting":
      return "مالی: متناقض";
    default:
      return "مالی: نامشخص";
  }
}

export function financialStateTone(state: ShippingMethodFinancialState): FinancialBadgeTone {
  switch (state) {
    case "financially-configured":
      return "success";
    case "conflicting":
      return "danger";
    case "needs-configuration":
    case "legacy-shared-unresolved":
      return "warning";
    default:
      return "neutral";
  }
}

/** The precise reason, worded so no state can be mistaken for "free" or "ready". */
export function financialReasonFa(status: ShippingMethodFinancialStatus): string {
  switch (status.reasonCode) {
    case "shipping.financial.owned_rules_present":
      return "قاعدهٔ نرخ فعالِ همین روش با مبلغ و ارز اعلام‌شده وجود دارد.";
    case "shipping.financial.no_owned_rules":
      return "هیچ قاعدهٔ نرخ فعالی متعلق به همین روش وجود ندارد؛ فعال‌بودن روش به‌معنای رایگان نبودن است و هیچ مبلغی ساخته نمی‌شود.";
    case "shipping.financial.legacy_service_level_shared":
      return "تنها ردیف‌های نرخ سطح سرویس فعال‌اند و این روش گزینهٔ اختصاصی دارد؛ آن مبلغ مالکیت این روش را اثبات نمی‌کند و به آن نسبت داده نمی‌شود.";
    case "shipping.financial.ambiguous_owned_scope":
      return "دو قاعدهٔ فعال همین روش دامنه و شرط یکسان دارند؛ انتخاب خودکار یکی از آن‌ها یک تصمیم تجاری حل‌نشده است.";
    case "shipping.financial.mixed_currency":
      return "قاعده‌های فعال همین روش بیش از یک ارز اعلام می‌کنند؛ مبلغ یگانه‌ای قابل اثبات نیست.";
    default:
      return "وضعیت مالی این روش از سرور قابل‌اثبات نبود؛ هیچ مبلغ یا حالت مالی ساخته نمی‌شود.";
  }
}

/** The money line: only ever rendered from proven, server-sourced values. */
export function financialMoneyFa(status: ShippingMethodFinancialStatus): string | null {
  if (!hasProvenOwnedMoney(status)) return null;
  const currency = status.currencyCode ?? "";
  const single = provenOwnedAmount(status);
  if (single != null && single > 0) {
    return `مبلغ ثابت اعلام‌شده: ${single.toLocaleString("fa-IR")} ${currency}`;
  }
  if (status.ruleCount > 1) {
    return `مبلغ ثابت به‌ازای هر قاعده: ${status.ruleCount} قاعده (ارز ${currency})`;
  }
  return null;
}

/**
 * Operator-visible facts that must never be summarized away: shared legacy rows, sibling rows,
 * coverage absence and a stored zero amount.
 */
export function financialCaveatsFa(status: ShippingMethodFinancialStatus): string[] {
  const caveats: string[] = [];
  if (status.state === "legacy-shared-unresolved" && status.activeServiceLevelRuleCount > 0) {
    caveats.push(`تعرفهٔ سطح سرویس: ${status.activeServiceLevelRuleCount} ردیف فعال مشترک — قابل‌خواندن برای حسابرسی، بدون مالکیت این روش.`);
  }
  if (status.activeSiblingOptionRuleCount > 0) {
    caveats.push(`${status.activeSiblingOptionRuleCount} قاعدهٔ فعال برای گزینه‌های دیگر همین سرویس وجود دارد که به این روش نسبت داده نمی‌شود.`);
  }
  if (status.state === "financially-configured" && status.activeCoverageGrantCount === 0) {
    caveats.push("هیچ مجوز پوشش فعالی برای این سرویس ثبت نشده است؛ نرخ قابل‌اعلام برای هیچ مقصدی اثبات نمی‌شود.");
  }
  if (status.hasZeroChargeRule) {
    caveats.push("مبلغ صفر در دادهٔ همین روش ذخیره شده است؛ اسکیمای فعلی هیچ حالت «رایگان» صریحی ندارد و صفر به‌عنوان رایگان تفسیر نمی‌شود.");
  }
  if (status.conditionedRuleCount > 0) {
    caveats.push(`${status.conditionedRuleCount} قاعده وابسته به پلهٔ وزن/تعداد است و فقط داخل بازهٔ خودش قیمت می‌دهد.`);
  }
  return caveats;
}

/** Modes the current schema can and cannot prove; stated so no unsupported mode is implied. */
export const FINANCIAL_MODE_TABLE_FA = [
  "مبلغ ثابت اعلام‌شده: اثبات‌شده از ردیف‌های نرخ واقعی.",
  "رایگان به‌مشتری: اسکیمای فعلی حالت صریح رایگان ندارد؛ صفر ذخیره‌شده رایگان نیست.",
  "قاعده‌محور محاسباتی: موتور بیان/فرمول وجود ندارد؛ فقط مبلغ ثابت به‌ازای دامنه/پله.",
  "پرداخت در محل: هیچ فیلد مسئولیت پرداخت/وصول وجود ندارد.",
  "استعلام ارائه‌دهنده: هیچ موجودیت/یکپارچگی استعلام حامل وجود ندارد.",
] as const;
