/**
 * TB-SHIPPING-METHOD-FINANCIAL-EDIT-156 — pure fail-closed gate for editing the monetary policy of
 * ONE canonical shipping method.
 *
 * Why a separate gate: the existing Shipping-owned rate API can already store a method+option-scoped
 * fixed rule, but "can write" is not the same as "may write safely". This model consumes the verified
 * TB-155 financial status and decides whether an in-place edit or a new owned rule is safe, and when
 * it is not, it states exactly why in Persian instead of silently choosing/overwriting a commercial
 * value. It is React-free so the decision is testable without a server.
 */
import type { ShippingMethodFinancialStatus } from "./shipping-method-financial-model.ts";

export type FinancialEditGate =
  /** No owned rule yet: creating this method's first owned fixed rule is the intended fix. */
  | { kind: "create-allowed"; reasonFa: string }
  /** One owned rule set in a single currency: editing/adding scoped rules is safe. */
  | { kind: "edit-allowed"; reasonFa: string }
  /**
   * Only provider/service-level rows exist. They are NOT this method's tariff, so they are never
   * edited or inherited here; the operator must author an owned rule for this method instead.
   */
  | { kind: "legacy-shared"; reasonFa: string }
  /** Two owned rules claim the same scope+condition, or more than one currency is claimed. */
  | { kind: "blocked-conflict"; reasonFa: string }
  /** The status could not be verified server-side: nothing may be written on an unproven basis. */
  | { kind: "blocked-unverified"; reasonFa: string };

/** Whether this gate permits writing at all (create or edit). */
export function gateBlocksWrites(gate: FinancialEditGate): boolean {
  return gate.kind === "blocked-conflict" || gate.kind === "blocked-unverified";
}

/** Whether an existing owned rule may be edited in place under this gate. */
export function gateAllowsInPlaceEdit(gate: FinancialEditGate): boolean {
  return gate.kind === "edit-allowed";
}

/** Whether a NEW owned rule for this method may be created under this gate. */
export function gateAllowsCreate(gate: FinancialEditGate): boolean {
  return gate.kind === "create-allowed" || gate.kind === "edit-allowed" || gate.kind === "legacy-shared";
}

/**
 * Decides the edit gate from the verified status. `status === null` means the read did not verify
 * anything (unreachable, denied, invalid) and is treated as fail-closed, never as "free to write".
 */
export function resolveFinancialEditGate(status: ShippingMethodFinancialStatus | null): FinancialEditGate {
  if (!status) {
    return {
      kind: "blocked-unverified",
      reasonFa:
        "وضعیت مالی این روش از سرور تأیید نشد؛ تا زمانی که وضعیت اثبات‌شده خوانده نشود، هیچ مبلغی ذخیره یا بازنویسی نمی‌شود.",
    };
  }

  switch (status.state) {
    case "financially-configured":
      return {
        kind: "edit-allowed",
        reasonFa:
          status.ruleCount === 1
            ? "این روش یک قاعدهٔ نرخ اختصاصی و تأییدشده دارد؛ ویرایش همان قاعده امن است."
            : "قاعده‌های نرخ این روش در یک ارز و بدون ابهام دامنه/شرط هستند؛ ویرایش و افزودن دامنهٔ تازه امن است.",
      };
    case "needs-configuration":
      return {
        kind: "create-allowed",
        reasonFa:
          "این روش هیچ قاعدهٔ نرخ اختصاصی ندارد. تا وقتی نرخ ثبت نشود قیمت عملیاتی ندارد و رایگان محسوب نمی‌شود؛ ثبت نرخ اختصاصی همین روش کار درست است.",
      };
    case "legacy-shared-unresolved":
      return {
        kind: "legacy-shared",
        reasonFa:
          `برای این سرویس ${status.activeServiceLevelRuleCount.toLocaleString("fa-IR")} قاعدهٔ نرخ در سطح سرویس (بدون گزینه) وجود دارد که تعرفهٔ مشترک و قدیمی است و مالکیت این روش را اثبات نمی‌کند. آن مبلغ نه پیش‌فرض این فرم می‌شود و نه اینجا ویرایش/بازنویسی می‌شود؛ برای این روش یک قاعدهٔ اختصاصی تازه ثبت کنید.`,
      };
    case "conflicting":
      return {
        kind: "blocked-conflict",
        reasonFa:
          status.reasonCode === "shipping.financial.mixed_currency"
            ? "قاعده‌های فعال این روش بیش از یک ارز اعلام می‌کنند؛ تا وقتی این تناقض حل نشود هیچ ویرایش یا ثبت تازه‌ای انجام نمی‌شود تا یک تصمیم تجاری ناخواسته ساخته نشود."
            : `برای این روش ${status.ruleCount.toLocaleString("fa-IR")} قاعدهٔ فعال با دامنه و شرط یکسان وجود دارد و انتخاب خودکار یکی از آن‌ها یک تصمیم تجاری حل‌نشده است. ابتدا یکی از آن ردیف‌ها را غیرفعال یا حذف کنید، سپس اینجا ویرایش کنید.`,
      };
    default:
      return {
        kind: "blocked-unverified",
        reasonFa:
          "وضعیت مالی این روش از سرور قابل‌تأیید نبود؛ تا زمانی که وضعیت اثبات‌شده خوانده نشود، هیچ مبلغی ذخیره یا بازنویسی نمی‌شود.",
      };
  }
}

/** Explains a legitimately stored zero amount so it is never read as an invented free mode. */
export const ZERO_AMOUNT_NOTE_FA =
  "مبلغ صفر یک مبلغ ذخیره‌شده است، نه حالت «رایگان به مشتری»؛ اسکیمای فعلی حالت رایگان صریح ندارد.";

/** Whether a raw amount string is a legitimate, non-negative stored zero (not an empty/invalid field). */
export function isStoredZeroAmount(raw: string): boolean {
  const trimmed = raw.trim();
  if (!trimmed) return false;
  const parsed = Number(trimmed.replace(/[٫,]/g, ""));
  return Number.isFinite(parsed) && parsed === 0;
}
