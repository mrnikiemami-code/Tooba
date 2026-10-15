"use client";

/**
 * شل پنج‌زبانهٔ سرویس ارسال (کاتالوگ یک‌سطحی) — پشت دروازهٔ خاموش.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-084
 *
 * قواعد سخت این کامپوننت:
 * - فقط-خواندنی است. هیچ دکمهٔ ذخیره، هیچ دیالوگ ویرایش و هیچ نوشتنی ندارد.
 * - هیچ «سطح دوم» (provider → option) و هیچ ستون تعداد گزینه نمایش نمی‌دهد.
 *   `providerKind` فقط فرادادهٔ داخلی است و هرگز گزینهٔ قابل انتخاب نیست.
 * - روش غیرفعال غیرفعال می‌ماند؛ نه پنهان می‌شود و نه فعال.
 * - در نبود داده، وضعیت صادقانه نشان می‌دهد و هرگز به درخت دوسطحی قدیمی fallback نمی‌کند.
 * - هر زبانهٔ سیم‌کشی‌نشده صریحاً می‌گوید در کدام تسک سیم‌کشی می‌شود.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button, EmptyState, ErrorState } from "../../../design-system";
import { ShippingOneLevelRatesTab } from "./shipping-one-level-rates-tab.tsx";
import { ShippingOneLevelPackagingTab } from "./shipping-one-level-packaging-tab.tsx";
import { ShippingOneLevelScheduleTab } from "./shipping-one-level-schedule-tab.tsx";
import { ShippingOneLevelQuoteTab } from "./shipping-one-level-quote-tab.tsx";
import {
  ONE_LEVEL_TAB_IDS,
  ONE_LEVEL_TAB_LABELS,
  buildOneLevelSearch,
  nextTabIndex,
  parseOneLevelSelection,
  projectMethodRows,
  resolveDataState,
  wiringNoticeFor,
  type OneLevelMethodRow,
  type OneLevelTabId,
} from "./shipping-one-level-tabs-model.ts";
import type { OneLevelCatalogGate } from "./shipping-one-level-gate.ts";

export interface ShippingOneLevelTabsProps {
  /** وضعیت دروازهٔ سمت سرور. دروازهٔ خاموش ⇒ این کامپوننت اصلاً رندر نمی‌شود. */
  gate: OneLevelCatalogGate;
  /** زبان فعال برای برچسب‌ها؛ خالی یعنی هیچ زبان فعالی شناخته نشده. */
  activeLanguageId?: string;
  /**
   * TB-SHIPPING-ONE-LEVEL-TABS-085: ردیف‌های خوانده‌شده در Server Component.
   * وقتی ارائه شود، همان دادهٔ سرور رندر می‌شود و هیچ درخواست مجوزدار از مرورگر زده نمی‌شود.
   */
  initialMethods?: readonly OneLevelMethodRow[];
  /** خوانندهٔ روش‌ها. نبود آن ⇒ وضعیت «در دسترس نیست»، نه fallback. */
  loadMethods?: () => Promise<readonly OneLevelMethodRow[]>;
  /**
   * TB-SHIPPING-ONE-LEVEL-TABS-086: بازخورد موفق/خطای عملیات زبانهٔ تعرفه و پوشش.
   * در نبود آن، عملیات همچنان اجرا می‌شود و فقط اعلان نمایش داده نمی‌شود.
   */
  onNotice?: (notice: { tone: "success" | "error"; text: string }) => void;
}

export function ShippingOneLevelTabs({
  gate,
  activeLanguageId = "",
  initialMethods,
  loadMethods,
  onNotice = () => {},
}: ShippingOneLevelTabsProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const search = searchParams?.toString() ?? "";

  const selection = useMemo(() => parseOneLevelSelection(search), [search]);
  const activeTab = selection.tab;

  const [methods, setMethods] = useState<readonly OneLevelMethodRow[]>(initialMethods ?? []);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState(false);

  const tabRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  const load = useCallback(async () => {
    if (!loadMethods) {
      // ردیف‌های سرور کافی‌اند؛ نبود خوانندهٔ کلاینتی خطا نیست.
      setLoadError(initialMethods === undefined);
      return;
    }
    setLoading(true);
    setLoadError(false);
    try {
      setMethods(await loadMethods());
    } catch {
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [initialMethods, loadMethods]);

  useEffect(() => {
    void load();
  }, [load]);

  /** جایگزینی آدرس با مقادیر امن و بدون افزودن تاریخچه. */
  const replaceSelection = useCallback(
    (next: Partial<{ tab: OneLevelTabId; methodCode: string }>) => {
      const target = {
        tab: next.tab ?? activeTab,
        methodCode: next.methodCode ?? selection.methodCode,
        languageId: selection.languageId,
      };
      router.replace(`/admin/shipping-services${buildOneLevelSearch(target)}`, { scroll: false });
    },
    [activeTab, router, selection.languageId, selection.methodCode],
  );

  const onTabKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLButtonElement>, index: number) => {
      const target = nextTabIndex(index, event.key, ONE_LEVEL_TAB_IDS.length);
      if (target === null) return;
      event.preventDefault();
      const id = ONE_LEVEL_TAB_IDS[target];
      tabRefs.current[id]?.focus();
      replaceSelection({ tab: id });
    },
    [replaceSelection],
  );

  const dataState = resolveDataState({ gateEnabled: gate.state === "enabled", loading, error: loadError });
  const rows = useMemo(() => projectMethodRows(methods, activeLanguageId), [methods, activeLanguageId]);

  return (
    <section className="space-y-4" data-testid="admin-shipping-one-level-shell" dir="rtl">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold">سرویس ارسال — کاتالوگ یک‌سطحی</h2>
          <p className="mt-1 text-sm text-muted">
            هر روش، یک ردیف مستقل است. این صفحه فقط-خواندنی است و هیچ تغییری در قیمت یا رزرو ایجاد نمی‌کند.
          </p>
        </div>
        {selection.methodCode ? (
          <p className="text-sm text-muted" data-testid="admin-shipping-one-level-context">
            روش انتخاب‌شده: <span dir="ltr" className="font-mono">{selection.methodCode}</span>
          </p>
        ) : null}
      </div>

      <div className="flex flex-wrap gap-2 border-b border-border pb-2" role="tablist" aria-label="بخش‌های سرویس ارسال">
        {ONE_LEVEL_TAB_IDS.map((id, index) => {
          const selected = id === activeTab;
          return (
            <button
              key={id}
              ref={(node) => {
                tabRefs.current[id] = node;
              }}
              type="button"
              role="tab"
              id={`shipping-one-level-tab-${id}`}
              aria-selected={selected}
              aria-controls={`shipping-one-level-panel-${id}`}
              tabIndex={selected ? 0 : -1}
              data-testid={`admin-shipping-one-level-tab-${id}`}
              className={
                selected
                  ? "inline-flex min-h-11 items-center rounded-ds bg-primary px-3 text-sm font-medium text-primary-foreground"
                  : "inline-flex min-h-11 items-center rounded-ds bg-secondary px-3 text-sm text-secondary-foreground"
              }
              onClick={() => replaceSelection({ tab: id })}
              onKeyDown={(event) => onTabKeyDown(event, index)}
            >
              {ONE_LEVEL_TAB_LABELS[id]}
            </button>
          );
        })}
      </div>

      <div
        role="tabpanel"
        id={`shipping-one-level-panel-${activeTab}`}
        aria-labelledby={`shipping-one-level-tab-${activeTab}`}
        tabIndex={0}
        data-testid={`admin-shipping-one-level-panel-${activeTab}`}
      >
        {activeTab === "services" ? (
          <ServicesTab
            state={dataState}
            rows={rows}
            selectedMethodCode={selection.methodCode}
            onRetry={() => void load()}
            onSelect={(code) => replaceSelection({ methodCode: code })}
          />
        ) : activeTab === "rates-coverage" ? (
          // TB-SHIPPING-ONE-LEVEL-TABS-086: زبانهٔ ۲ روی همان روش انتخاب‌شدهٔ زبانهٔ ۱ عمل می‌کند.
          <ShippingOneLevelRatesTab methodCode={selection.methodCode} onNotice={onNotice} />
        ) : activeTab === "packaging-fare" ? (
          // TB-SHIPPING-ONE-LEVEL-TABS-087: زبانهٔ ۳ با همان پیوند سرور و همان مدل قدیمی بسته‌بندی.
          <ShippingOneLevelPackagingTab methodCode={selection.methodCode} onNotice={onNotice} />
        ) : activeTab === "schedule-capacity" ? (
          // TB-SHIPPING-ONE-LEVEL-TABS-088: زبانهٔ ۴ با همان پیوند سرور و همان مدل قدیمی برنامهٔ کاری.
          <ShippingOneLevelScheduleTab methodCode={selection.methodCode} onNotice={onNotice} />
        ) : (
          // TB-SHIPPING-ONE-LEVEL-TABS-089: زبانهٔ ۵ فقط تشخیص خواندنی هزینه است، نه قیمت مشتری.
          <ShippingOneLevelQuoteTab methodCode={selection.methodCode} onNotice={onNotice} />
        )}
      </div>
    </section>
  );
}

function ServicesTab({
  state,
  rows,
  selectedMethodCode,
  onRetry,
  onSelect,
}: {
  state: ReturnType<typeof resolveDataState>;
  rows: ReturnType<typeof projectMethodRows>;
  selectedMethodCode: string;
  onRetry: () => void;
  onSelect: (code: string) => void;
}) {
  if (state === "unavailable") {
    return (
      <EmptyState
        title="کاتالوگ یک‌سطحی در دسترس نیست"
        detail="دروازهٔ سمت سرور خاموش است یا بایند خواندن آن هنوز وصل نشده است."
      />
    );
  }
  if (state === "loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="admin-shipping-one-level-loading">
        در حال بارگذاری روش‌ها…
      </p>
    );
  }
  if (state === "error") {
    return (
      <ErrorState
        title="بارگذاری روش‌های ارسال ناموفق بود"
        detail="اتصال به خوانندهٔ کاتالوگ برقرار نشد."
        onRetry={onRetry}
        retryLabel="تلاش مجدد"
      />
    );
  }
  if (rows.length === 0) {
    return (
      <EmptyState
        title="روشی ثبت نشده است"
        detail="کاتالوگ یک‌سطحی خالی است. تا زمان مهاجرت مجاز، کاتالوگ قدیمی مرجع عملیاتی است."
      />
    );
  }

  return (
    <div className="overflow-hidden rounded-ds border border-border">
      <table className="w-full text-sm">
        <caption className="sr-only">روش‌های ارسال یک‌سطحی</caption>
        <thead className="bg-secondary text-secondary-foreground">
          <tr>
            <th scope="col" className="px-3 py-2 text-start">برچسب نمایشی</th>
            <th scope="col" className="px-3 py-2 text-start">کد روش</th>
            <th scope="col" className="px-3 py-2 text-start">وضعیت</th>
            <th scope="col" className="px-3 py-2 text-start">انتخاب</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const selected = row.methodCode === selectedMethodCode;
            return (
              <tr
                key={row.methodCode}
                data-testid={`admin-shipping-one-level-method-${row.methodCode}`}
                className={selected ? "bg-secondary" : undefined}
              >
                <td className="px-3 py-2">
                  {row.hasLabel ? row.label : <span className="text-muted">بدون برچسب</span>}
                </td>
                <td className="px-3 py-2 font-mono" dir="ltr">{row.methodCode}</td>
                <td className="px-3 py-2">
                  <span
                    data-testid={`admin-shipping-one-level-status-${row.methodCode}`}
                    className={row.isActive ? "text-emerald-700" : "text-muted"}
                  >
                    {row.isActive ? "فعال" : "غیرفعال"}
                  </span>
                </td>
                <td className="px-3 py-2">
                  <Button
                    type="button"
                    tone="secondary"
                    aria-pressed={selected}
                    data-testid={`admin-shipping-one-level-select-${row.methodCode}`}
                    onClick={() => onSelect(row.methodCode)}
                  >
                    {selected ? "انتخاب‌شده" : "انتخاب"}
                  </Button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function WiringShell({ tab }: { tab: OneLevelTabId }) {
  const notice = wiringNoticeFor(tab);
  return (
    <div className="rounded-ds border border-dashed border-border p-6" data-testid={`admin-shipping-one-level-wiring-${tab}`}>
      <p className="font-medium">{ONE_LEVEL_TAB_LABELS[tab]}</p>
      <p className="mt-1 text-sm text-muted">{notice}</p>
    </div>
  );
}
