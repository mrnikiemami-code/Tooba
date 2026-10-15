"use client";

/**
 * شل پنج‌زبانهٔ سرویس ارسال (کاتالوگ یک‌سطحی) — پشت دروازهٔ خاموش.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-084 · TB-SHIPPING-ONE-LEVEL-TABS-094
 *
 * قواعد سخت این کامپوننت:
 * - تنها نوشتن مجاز این شل، «نام نمایشی» یک روش در یک زبان است (TB-094). هیچ نوشتن دیگری
 *   (کد روش، فعال/غیرفعال‌سازی، حذف، قیمت) وجود ندارد.
 * - هیچ «سطح دوم» (provider → option) و هیچ ستون تعداد گزینه نمایش نمی‌دهد.
 *   `providerKind` فقط فرادادهٔ داخلی است و هرگز گزینهٔ قابل انتخاب نیست.
 * - روش غیرفعال غیرفعال می‌ماند؛ نه پنهان می‌شود و نه فعال.
 * - در نبود داده، وضعیت صادقانه نشان می‌دهد و هرگز به درخت دوسطحی قدیمی fallback نمی‌کند.
 * - هر زبانهٔ سیم‌کشی‌نشده صریحاً می‌گوید در کدام تسک سیم‌کشی می‌شود.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button, EmptyState, ErrorState } from "../../../design-system";
import { loadAdminLanguages } from "../../../features/admin-languages";
import { ShippingOneLevelRatesTab } from "./shipping-one-level-rates-tab.tsx";
import { ShippingOneLevelPackagingTab } from "./shipping-one-level-packaging-tab.tsx";
import { ShippingOneLevelScheduleTab } from "./shipping-one-level-schedule-tab.tsx";
import { ShippingOneLevelQuoteTab } from "./shipping-one-level-quote-tab.tsx";
import {
  ShippingOneLevelMethodNameDialog,
  type OneLevelLanguageOption,
} from "./shipping-one-level-method-name-dialog.tsx";
import { applySavedMethodName, findMethodRow } from "./shipping-one-level-method-name-model.ts";
import {
  ShippingOneLevelActivationControl,
} from "./shipping-one-level-activation-control.tsx";
import {
  createActivationGenerationGuard,
  loadOneLevelMethodBinding,
} from "./shipping-one-level-activation-model.ts";
import type { OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
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
import { sanitizeLanguageId } from "./shipping-one-level-tabs-model.ts";
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
  const [editOpen, setEditOpen] = useState(false);

  /**
   * TB-SHIPPING-ONE-LEVEL-TABS-097: پیوند حل‌شدهٔ سرور برای روش انتخاب‌شده. تنها منبع مجاز
   * «گزینه‌دار بودن» است؛ بدون آن هیچ سوئیچ فعال‌سازی نشان داده نمی‌شود.
   */
  const [bindingState, setBindingState] = useState<OneLevelBindingState | null>(null);
  const [bindingLoading, setBindingLoading] = useState(false);
  const bindingGuard = useRef(createActivationGenerationGuard());

  /**
   * زبان‌های واقعی رجیستری از همان درز مشترک Admin خوانده می‌شوند (`GET /v1/admin/languages`).
   * نبود/ردمجوز/خطا ⇒ فهرست خالی ⇒ ویرایش نام غیرفعال می‌ماند (fail-closed)، نه اینکه زبان جعلی بسازیم.
   */
  const [editableLanguages, setEditableLanguages] = useState<readonly OneLevelLanguageOption[]>([]);

  const tabRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  useEffect(() => {
    let cancelled = false;
    void loadAdminLanguages().then((result) => {
      if (cancelled) return;
      const rows = result.state === "ok" && result.data ? result.data : [];
      setEditableLanguages(
        rows
          .filter((row) => row.active && sanitizeLanguageId(row.languageId))
          .map((row) => ({
            languageId: sanitizeLanguageId(row.languageId),
            label: row.nativeName?.trim() || row.displayName?.trim() || row.code,
          })),
      );
    });
    return () => {
      cancelled = true;
    };
  }, []);

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

  /**
   * TB-SHIPPING-ONE-LEVEL-TABS-097: خواندن فقط-خواندنی پیوند روش انتخاب‌شده.
   *
   * نگهبان نسل تضمین می‌کند نتیجهٔ در راهِ روش قبلی هرگز روی روش انتخاب‌شدهٔ تازه ننشیند؛ اگر
   * کاربر وسط خواندن روش را عوض کند، پاسخ باطل می‌شود و هیچ پیوند اشتباهی نمایش داده نمی‌شود.
   */
  const loadBinding = useCallback(async () => {
    const code = selection.methodCode;
    const stillCurrent = bindingGuard.current.begin();
    if (!code) {
      setBindingState(null);
      setBindingLoading(false);
      return;
    }
    setBindingLoading(true);
    const state = await loadOneLevelMethodBinding(code);
    if (!stillCurrent()) return;
    setBindingState(state);
    setBindingLoading(false);
  }, [selection.methodCode]);

  useEffect(() => {
    void loadBinding();
  }, [loadBinding]);

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

  /**
   * پس از ذخیرهٔ تأییدشدهٔ سرور، منبع حقیقت دوباره خوانده می‌شود تا برچسب فهرست و زمینهٔ روش
   * انتخاب‌شده با هم سازگار بمانند. اگر خوانندهٔ کلاینتی نباشد، مقدار تأییدشدهٔ سرور روی همان
   * ردیف/زبان اعمال می‌شود (بدون ساختن ردیف یا تغییر هویت) و سپس refresh سرور اجرا می‌شود.
   */
  const onSaved = useCallback(
    (saved: { methodCode: string; languageId: string; name: string }) => {
      setMethods((current) => applySavedMethodName(current, saved.methodCode, saved.languageId, saved.name));
      void load();
      router.refresh();
    },
    [load, router],
  );

  /**
   * TB-SHIPPING-ONE-LEVEL-TABS-097: پس از پاسخ تأییدشدهٔ سرور برای فعال‌سازی، هیچ وضعیت خوش‌بینانهٔ
   * محلی ساخته نمی‌شود؛ کاتالوگ و پیوند دوباره از سرور خوانده می‌شوند تا وضعیت نمایش‌داده‌شده همان
   * حقیقت سرور باشد. اگر سرور مقدار دیگری برگردانده باشد، همان دیده می‌شود.
   */
  const onActivationCompleted = useCallback(() => {
    void load();
    void loadBinding();
    router.refresh();
  }, [load, loadBinding, router]);

  return (
    <section className="space-y-4" data-testid="admin-shipping-one-level-shell" dir="rtl">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold">سرویس ارسال — کاتالوگ یک‌سطحی</h2>
          <p className="mt-1 text-sm text-muted">
            هر روش، یک ردیف مستقل است. ویرایش‌های این صفحه: «نام نمایشی» هر روش و فعال/غیرفعال‌سازی
            روش‌های گزینه‌دار. هیچ تغییر قیمت یا رزروی ایجاد نمی‌شود و فعال‌سازی فقط از طریق سرور اعمال
            می‌شود.
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
          <>
            <ServicesTab
              state={dataState}
              rows={rows}
              selectedMethodCode={selection.methodCode}
              canEditName={editableLanguages.length > 0}
              onRetry={() => void load()}
              onSelect={(code) => replaceSelection({ methodCode: code })}
              onEditName={(code) => {
                // انتخاب همان روش پیش از باز کردن ویرایش، تا زمینهٔ روش و دیالوگ یکی باشند.
                replaceSelection({ methodCode: code });
                setEditOpen(true);
              }}
            />
            {/* TB-SHIPPING-ONE-LEVEL-TABS-097: تنها عملیات فعال‌سازی، فقط برای روش گزینه‌دار و
                فقط از روی پیوند حل‌شدهٔ سرور؛ روش بدون گزینه هیچ سوئیچ کاربردی ندارد. */}
            <ShippingOneLevelActivationControl
              methodCode={selection.methodCode}
              bindingState={bindingState}
              bindingLoading={bindingLoading}
              onNotice={onNotice}
              onCompleted={onActivationCompleted}
            />
          </>
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

      {/* TB-SHIPPING-ONE-LEVEL-TABS-094: تنها ویرایشگر این شل — نام نمایشی یک روش در یک زبان. */}
      <ShippingOneLevelMethodNameDialog
        open={editOpen && selection.methodCode.length > 0}
        methodCode={selection.methodCode}
        method={findMethodRow(methods, selection.methodCode)}
        languages={editableLanguages}
        defaultLanguageId={activeLanguageId}
        onClose={() => setEditOpen(false)}
        onNotice={onNotice}
        onSaved={onSaved}
      />
    </section>
  );
}

function ServicesTab({
  state,
  rows,
  selectedMethodCode,
  canEditName,
  onRetry,
  onSelect,
  onEditName,
}: {
  state: ReturnType<typeof resolveDataState>;
  rows: ReturnType<typeof projectMethodRows>;
  selectedMethodCode: string;
  canEditName: boolean;
  onRetry: () => void;
  onSelect: (code: string) => void;
  onEditName: (code: string) => void;
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
            <th scope="col" className="px-3 py-2 text-start">اقدام</th>
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
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      tone="secondary"
                      aria-pressed={selected}
                      data-testid={`admin-shipping-one-level-select-${row.methodCode}`}
                      onClick={() => onSelect(row.methodCode)}
                    >
                      {selected ? "انتخاب‌شده" : "انتخاب"}
                    </Button>
                    <Button
                      type="button"
                      tone="ghost"
                      data-testid={`admin-shipping-one-level-edit-${row.methodCode}`}
                      disabled={!canEditName}
                      onClick={() => onEditName(row.methodCode)}
                    >
                      ویرایش نام
                    </Button>
                  </div>
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
