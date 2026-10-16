/**
 * TB-SHIPPING-ONE-LEVEL-TABS-135 — گروه‌بندی انتخاب ارسال به‌تفکیک **هر خط سبد**.
 *
 * قرارداد گروه‌بندی (docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md):
 * انتخاب مشتری یک انتخاب «هر قلم» است، نه یک انتخاب سطح سبد. یک فروشنده می‌تواند چند گروه ارسال
 * داشته باشد، اما دو فروشنده هرگز در یک گروه ادغام نمی‌شوند — حتی با روش یکسان.
 *
 * این ماژول هیچ `fetch`، هیچ React و هیچ محاسبهٔ مبلغ/تاریخ ندارد؛ فقط منطق خالص گروه‌بندی و
 * بازیابی است تا قطعی و قابل‌سنجش بماند:
 * - کلید گروه = (فروشندهٔ واقعی خط، کد روش authoritative)؛ خطوط هم‌فروشنده و هم‌روش در **یک** گروه
 *   ادغام می‌شوند (ناوردایی Order M1: یک گروه برای هر (فروشنده، روش)).
 * - فروشنده همیشه از خود خط سبد خوانده می‌شود؛ هیچ فروشنده/انبار/مبلغی ساخته نمی‌شود.
 * - روش ناشناخته (خارج از فهرست authoritative سرور) هرگز گروه نمی‌سازد.
 */
import type { StorefrontCartLine } from "./storefront-cart-api.ts";
import type { StorefrontShippingMethod } from "./storefront-shipping-api.ts";

/** یک گروه ارسال واقعی: فروشندهٔ خطوط + یک روش authoritative + خطوط سبد عضو. */
export interface StorefrontShippingItemGroup {
  /** هویت پایدار گروه در کلاینت: فروشنده + کد روش canonical. */
  key: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  methodCode: string;
  method: StorefrontShippingMethod;
  lines: StorefrontCartLine[];
}

export interface StorefrontShippingItemGrouping {
  groups: StorefrontShippingItemGroup[];
  /** خطوطی که هنوز روش معتبر ندارند؛ ادامهٔ سفارش تا پر شدن همه «بسته» است. */
  unassignedLineIds: string[];
}

/** گروه پایدار ذخیره‌شده در پیش‌نویس (بدون هیچ مقدار مشتق‌شده). */
export interface PersistedShippingGroupAssignment {
  sellerPartyId: string;
  methodCode: string;
  /** شناسهٔ واقعی خطوط سبد همان گروه؛ اگر سرور اعلام نکند خالی است. */
  cartLineIds: readonly string[];
}

function methodLookup(methods: readonly StorefrontShippingMethod[]): Map<string, StorefrontShippingMethod> {
  const map = new Map<string, StorefrontShippingMethod>();
  for (const method of methods) {
    if (method.methodCode) map.set(method.methodCode, method);
  }
  return map;
}

function resolveMethod(
  lookup: ReadonlyMap<string, StorefrontShippingMethod>,
  code: string,
): StorefrontShippingMethod | null {
  if (!code) return null;
  const exact = lookup.get(code);
  if (exact) return exact;
  const lowered = code.toLowerCase();
  for (const [key, method] of lookup) {
    if (key.toLowerCase() === lowered) return method;
  }
  return null;
}

/**
 * گروه‌های ارسال را از انتخاب هر خط می‌سازد.
 * ترتیب گروه‌ها ترتیب اولین‌ظهور در سبد است تا نمایش پایدار بماند؛ سرور خودش ترتیب canonical را
 * دوباره اعمال می‌کند (فروشنده سپس کد روش).
 */
export function buildShippingItemGroups(input: {
  lines: readonly StorefrontCartLine[];
  methodCodeByLine: Readonly<Record<string, string>>;
  methods: readonly StorefrontShippingMethod[];
}): StorefrontShippingItemGrouping {
  const lookup = methodLookup(input.methods);
  const byKey = new Map<string, StorefrontShippingItemGroup>();
  const unassignedLineIds: string[] = [];

  for (const line of input.lines) {
    const method = resolveMethod(lookup, (input.methodCodeByLine[line.lineId] ?? "").trim());
    if (!method) {
      unassignedLineIds.push(line.lineId);
      continue;
    }

    const key = `${line.sellerPartyId}\u0000${method.methodCode}`;
    const existing = byKey.get(key);
    if (existing) {
      existing.lines.push(line);
      continue;
    }

    byKey.set(key, {
      key,
      sellerPartyId: line.sellerPartyId,
      sellerDisplayName: line.sellerDisplayName,
      methodCode: method.methodCode,
      method,
      lines: [line],
    });
  }

  return { groups: [...byKey.values()], unassignedLineIds };
}

/** هر خط سبد دقیقاً یک بار عضو یک گروه معتبر است. */
export function isItemGroupingComplete(
  grouping: StorefrontShippingItemGrouping,
  lines: readonly StorefrontCartLine[],
): boolean {
  if (lines.length === 0) return false;
  const covered = new Set<string>();
  for (const group of grouping.groups) {
    for (const line of group.lines) {
      if (covered.has(line.lineId)) return false;
      covered.add(line.lineId);
    }
  }
  return covered.size === lines.length && lines.every((line) => covered.has(line.lineId));
}

/**
 * بازیابی قطعی انتخاب هر خط از گروه‌های ذخیره‌شدهٔ پیش‌نویس.
 *
 * قواعد fail-closed:
 * - روش گروه ذخیره‌شده اگر امروز واجد شرایط نباشد، آن گروه **بازیابی نمی‌شود** و روش دیگری جایش
 *   حدس زده نمی‌شود.
 * - خطی که دیگر در سبد نیست نادیده گرفته می‌شود؛ خط ناشناس/بیگانه هرگز به گروهی نسبت داده نمی‌شود.
 * - اگر گروه ذخیره‌شده شناسهٔ خط نداشته باشد، فقط زمانی بازیابی می‌شود که فروشندهٔ آن دقیقاً یک خط
 *   زنده در همین سبد داشته باشد (قطعی، بدون حدس)؛ وگرنه آن فروشنده بدون انتخاب می‌ماند.
 * - هیچ خطی دو بار تخصیص نمی‌یابد (اولین گروه برنده است) و ترتیب خطوط سبد حفظ می‌شود.
 */
export function restoreLineMethodSelections(input: {
  lines: readonly StorefrontCartLine[];
  persistedGroups: readonly PersistedShippingGroupAssignment[];
  methods: readonly StorefrontShippingMethod[];
}): Record<string, string> {
  const lookup = methodLookup(input.methods);
  const lineById = new Map(input.lines.map((line) => [line.lineId, line]));
  const linesBySeller = new Map<string, StorefrontCartLine[]>();
  for (const line of input.lines) {
    const bucket = linesBySeller.get(line.sellerPartyId);
    if (bucket) bucket.push(line);
    else linesBySeller.set(line.sellerPartyId, [line]);
  }

  const restored: Record<string, string> = {};
  for (const persisted of input.persistedGroups) {
    const method = resolveMethod(lookup, (persisted.methodCode ?? "").trim());
    if (!method) continue;

    const declared = (persisted.cartLineIds ?? [])
      .map((id) => lineById.get(id))
      .filter((line): line is StorefrontCartLine => Boolean(line))
      .filter((line) => line.sellerPartyId === persisted.sellerPartyId);

    const targets = declared.length > 0
      ? declared
      : (linesBySeller.get(persisted.sellerPartyId) ?? []).length === 1
        ? linesBySeller.get(persisted.sellerPartyId)!
        : [];

    for (const line of targets) {
      if (restored[line.lineId]) continue;
      restored[line.lineId] = method.methodCode;
    }
  }

  return restored;
}
