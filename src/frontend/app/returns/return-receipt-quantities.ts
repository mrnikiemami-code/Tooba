/**
 * TB-277-R1 — the ONE authority for "may this line's inspected quantity be submitted?"
 *
 * It is deliberately free of React/DOM so the bounded arithmetic can be exercised directly, while the workspace's
 * confirm button and its confirm handler both read exactly these functions (a refusal can never be merely decorative).
 *
 * Every quantity is an EXACT integer count of millionths (`numeric(18,6)` units), so:
 *  - a whole-piece unit refuses `0.5` instead of rounding it,
 *  - `0.1 + 0.2` is exactly `0.3` (never `0.30000000000000004`), and
 *  - usable + damaged share ONE line ceiling that neither portion may fund.
 *
 * The server's stored cumulative coverage stays the final authority; nothing here is sent as a float.
 */
import { formatQuantityDisplayFa } from "../../lib/quantity-display.ts";
import { toPersianDigits } from "../../lib/persian-digits.ts";
import { formatUnitLabel } from "./return-api.ts";
import {
  addUnits,
  compareUnits,
  isAlignedToUnit,
  normalizeQuantityInput,
  parsePositiveExactUnits,
  unitsToWireNumber,
} from "./return-quantity.ts";

export type ReceiptDraftLine = {
  returnItemId: string;
  productLabel: string | null;
  unitLabel: string | null;
  /**
   * TB-277: decimals the ORDERING UNIT accepts (0 means whole pieces only). `null` means the owning read did not
   * answer a precision — the line is then honestly BLOCKED instead of being guessed as six decimals.
   */
  decimalPlaces: number | null;
  /**
   * TB-277-R1: the line's remaining covered quantity as EXACT integer millionths (`numeric(18,6)` units), derived by
   * integer subtraction from the authoritative requested/received values. It is the ONE ceiling both condition fields
   * share, and it is never a floating-point `requested - received`. `null` means the authoritative values could not be
   * reconstructed exactly — the line is then blocked instead of being given a guessed ceiling.
   */
  ceilingUnits: number | null;
  usable: string;
  damaged: string;
};

export type QuantityProblem =
  | "empty"
  | "malformed"
  | "precision"
  | "over_ceiling"
  | "unit_unknown"
  | "ceiling_unknown";

/**
 * TB-277-R1: ONE line entry is bounded by the unit's precision AND by the line's own remaining coverage, using EXACT
 * integer arithmetic. A piece-count unit therefore refuses `0.5`, `0.1 + 0.2` is exactly `0.3`, and the ceiling
 * comparison never depends on binary floating point. The server's cumulative ceiling stays the final authority.
 */
export function evaluateQuantityEntry(
  raw: string,
  decimalPlaces: number | null,
  ceilingUnits: number | null,
): { units: number | null; value: number | null; problem: QuantityProblem | null } {
  const normalized = normalizeQuantityInput(raw);
  if (normalized === "") return { units: null, value: null, problem: "empty" };
  const units = parsePositiveExactUnits(normalized);
  if (units === null) return { units: null, value: null, problem: "malformed" };
  if (decimalPlaces === null) return { units: null, value: null, problem: "unit_unknown" };
  if (ceilingUnits === null) return { units: null, value: null, problem: "ceiling_unknown" };
  if (!isAlignedToUnit(units, decimalPlaces)) return { units: null, value: null, problem: "precision" };
  if (compareUnits(units, ceilingUnits) > 0) return { units: null, value: null, problem: "over_ceiling" };
  return { units, value: unitsToWireNumber(units), problem: null };
}

/** The line's authoritative remaining coverage, localized for the field hint. */
export function formatCeilingFa(line: ReceiptDraftLine): string {
  return line.ceilingUnits === null ? "نامشخص" : formatQuantityDisplayFa(unitsToWireNumber(line.ceilingUnits));
}

/** The concise field-level message for one refusal; an accepted value returns an empty string. */
export function describeQuantityProblem(problem: QuantityProblem, line: ReceiptDraftLine): string {
  switch (problem) {
    case "malformed":
      return "مقدار را با رقم وارد کنید؛ حداکثر ۶ رقم اعشار و بدون جداکنندهٔ هزارگان.";
    case "unit_unknown":
      return "دقت واحد این قلم از خط سفارش تأیید نشد؛ تا آن زمان مقدار این قلم ثبت نمی‌شود.";
    case "ceiling_unknown":
      return "باقی‌ماندهٔ این قلم از داده‌های ثبت‌شده قابل محاسبه نبود؛ برای جلوگیری از ثبت مقدار نامعتبر، دریافت این قلم مسدود است.";
    case "precision":
      return line.decimalPlaces === 0
        ? `واحد این قلم «${formatUnitLabel(line.unitLabel)}» است و فقط مقدار صحیح می‌پذیرد.`
        : `واحد این قلم حداکثر ${toPersianDigits(line.decimalPlaces ?? 0)} رقم اعشار می‌پذیرد.`;
    case "over_ceiling":
      return `مقدار واردشده از باقی‌ماندهٔ این قلم (${formatCeilingFa(line)}) بیشتر است.`;
    default:
      return "";
  }
}

export type ReceiptLineItems = { returnItemId: string; quantity: number; condition: "Usable" | "Damaged" };

/**
 * TB-277: ONE authority for "is this receipt dialog submittable?" — the confirm button's disabled state and the
 * confirm handler both read it, so an over-ceiling or mis-precision amount is blocked locally (never merely reported
 * after a send) and the server's own cumulative ceiling stays the final authority.
 * <para>
 * TB-277-R1: both portions are summed and compared as EXACT integer millionths, so `0.1 + 0.2` against a `0.3`
 * ceiling is lawful while `0.2 + 0.2` is refused — no floating-point tolerance and no rounding.
 * </para>
 * <para>
 * <c>problem</c> is the first refusal attributable to ONE field (the field itself also shows it inline);
 * <c>sumProblem</c> is the aggregate fact of a line whose two portions are individually valid but over the shared
 * ceiling — it has no single field to blame, so it is published once for the dialog.
 * </para>
 */
export function collectReceiptItems(lines: readonly ReceiptDraftLine[]): {
  items: ReceiptLineItems[];
  problem: string | null;
  sumProblem: string | null;
} {
  if (lines.length === 0) {
    return { items: [], problem: null, sumProblem: null };
  }
  const items: ReceiptLineItems[] = [];
  let sumProblem: string | null = null;
  for (const line of lines) {
    const usable = normalizeQuantityInput(line.usable);
    const damaged = normalizeQuantityInput(line.damaged);
    if (usable === "" && damaged === "") continue;

    const usableCheck = evaluateQuantityEntry(usable, line.decimalPlaces, line.ceilingUnits);
    const damagedCheck = evaluateQuantityEntry(damaged, line.decimalPlaces, line.ceilingUnits);
    for (const [condition, check, entered] of [
      ["Usable", usableCheck, usable] as const,
      ["Damaged", damagedCheck, damaged] as const,
    ]) {
      if (entered === "") continue;
      if (check.problem) {
        return {
          items: [],
          problem: `قلم «${line.productLabel ?? "کالای سفارش"}»: ${describeQuantityProblem(check.problem, line)}`,
          sumProblem: null,
        };
      }
      items.push({ returnItemId: line.returnItemId, quantity: check.value as number, condition });
    }

    // The two portions are ONE bounded fact about this line: neither may fund the other.
    const total = addUnits(usableCheck.units ?? 0, damagedCheck.units ?? 0);
    if (line.ceilingUnits === null || compareUnits(total, line.ceilingUnits) > 0) {
      sumProblem =
        `قلم «${line.productLabel ?? "کالای سفارش"}»: مجموع سالم و معیوب ` +
        `(${formatQuantityDisplayFa(unitsToWireNumber(total))}) از باقی‌ماندهٔ این قلم ` +
        `(${formatCeilingFa(line)}) بیشتر است.`;
      return { items: [], problem: null, sumProblem };
    }
  }
  return { items, problem: null, sumProblem };
}
