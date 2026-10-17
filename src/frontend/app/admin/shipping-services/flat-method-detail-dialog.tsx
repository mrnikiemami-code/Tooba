"use client";

/**
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146 — نمای جزئیات یک روش با چهار زبانهٔ غیرتودرتو.
 * TB-SHIPPING-ADMIN-MULTILINGUAL-UX-148 — هدر فشرده و ویرایش چندزبانهٔ نام با زبان‌های واقعی سرور.
 *
 * زبانه‌ها: عمومی · تعرفه و پوشش · برنامه کاری حمل · بسته‌بندی و کرایه.
 * هیچ زبانهٔ «انواع سرویس» و هیچ زبانهٔ سراسری روی کل فهرست وجود ندارد؛ همهٔ زبانه‌ها روی **همان روش
 * انتخاب‌شده** عمل می‌کنند.
 *
 * دامنهٔ روش از تنها منبع مجاز می‌آید: پیوند authoritative سرور
 * (`GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding`).
 * `serviceCode`/`optionCode` هرگز در مرورگر از متن `methodCode` ساخته نمی‌شوند؛ تا وقتی پیوند حل
 * نشده باشد، زبانه‌های تخصصی هیچ سیاستی نمی‌خوانند و هیچ نوشتنی انجام نمی‌دهند.
 *
 * فهرست زبان‌ها هم فقط از رجیستری authoritative سرور خوانده می‌شود (`loadAdminLanguages`) و فقط یک‌بار
 * برای همین دیالوگ؛ هیچ زبان یا ترجمه‌ای در کلاینت ساخته نمی‌شود.
 *
 * اجزای سه زبانهٔ تخصصی همان کامپوننت‌های موجودند و منطق تجاری‌شان دوباره نوشته نشده است.
 */

import { useCallback, useEffect, useMemo, useState } from "react";
import { Dialog } from "../../../design-system";
import { loadAdminLanguages } from "../../../features/admin-languages";
import type { SupportedLocaleDefinition } from "../../../lib/i18n/supported-locales.ts";
import { FlatGeneralTab } from "./flat-general-tab.tsx";
import type { MethodLanguagesState } from "./flat-method-language-editor.tsx";
import { ShippingOneLevelRatesTab } from "./shipping-one-level-rates-tab.tsx";
import { ShippingOneLevelScheduleTab } from "./shipping-one-level-schedule-tab.tsx";
import { ShippingOneLevelPackagingTab } from "./shipping-one-level-packaging-tab.tsx";
import { loadOneLevelMethodBinding } from "./shipping-one-level-activation-model.ts";
import type { OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
import type { FlatMethodRow } from "./flat-method-rows.ts";
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

export const FLAT_DETAIL_TAB_IDS = ["general", "rates", "schedule", "packaging"] as const;
export type FlatDetailTabId = (typeof FLAT_DETAIL_TAB_IDS)[number];

export const FLAT_DETAIL_TAB_LABELS: Record<FlatDetailTabId, string> = {
  general: "عمومی",
  rates: "تعرفه و پوشش",
  schedule: "برنامه کاری حمل",
  packaging: "بسته‌بندی و کرایه",
};

const UNRESOLVED_BINDING: OneLevelBindingState = { state: "unresolved" };

export function FlatMethodDetailDialog({
  row,
  method,
  mode,
  activeTab,
  onTabChange,
  activeLanguageId,
  onClose,
  onNotice,
  onSaved,
  onRefreshed,
}: {
  row: FlatMethodRow;
  /** ردیف canonical تازهٔ همان روش از فهرست سرور؛ منبع نام چندزبانه است. */
  method: OneLevelMethodRow | undefined;
  mode: "view" | "edit";
  activeTab: FlatDetailTabId;
  onTabChange: (tab: FlatDetailTabId) => void;
  activeLanguageId: string;
  onClose: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /** پس از ذخیرهٔ موفق، فهرست دوباره خوانده می‌شود و دیالوگ بسته می‌شود. */
  onSaved: (text: string) => Promise<void>;
  /** پس از ذخیرهٔ موفق، فهرست دوباره خوانده می‌شود و همین دیالوگ باز می‌ماند. */
  onRefreshed: (text: string) => Promise<void>;
}) {
  const readOnly = mode === "view";
  const [binding, setBinding] = useState<OneLevelBindingState | null>(null);
  const [languages, setLanguages] = useState<readonly SupportedLocaleDefinition[]>([]);
  const [languagesState, setLanguagesState] = useState<MethodLanguagesState>("loading");

  // پیوند authoritative فقط یک‌بار برای همین روش خوانده می‌شود؛ هیچ تقسیم متنی کد روش رخ نمی‌دهد.
  useEffect(() => {
    let current = true;
    setBinding(null);
    void (async () => {
      const next = await loadOneLevelMethodBinding(row.methodCode);
      if (current) setBinding(next);
    })();
    return () => {
      current = false;
    };
  }, [row.methodCode]);

  // زبان‌های واقعی رجیستری از همان درز مشترک Admin؛ یک‌بار برای همین دیالوگ.
  const loadLanguages = useCallback(async () => {
    setLanguagesState("loading");
    const result = await loadAdminLanguages();
    if (result.state === "ok" && result.data) {
      setLanguages(result.data);
      setLanguagesState("ok");
      return;
    }
    setLanguages([]);
    setLanguagesState(result.state === "denied" ? "denied" : "error");
  }, []);

  useEffect(() => {
    void loadLanguages();
  }, [loadLanguages]);

  const bindingOverride = useMemo(
    () => (binding?.state === "resolved"
      ? {
        serviceCode: binding.binding.serviceCode,
        optionCode: binding.binding.optionCode,
        isActive: binding.binding.isActive,
      }
      : null),
    [binding],
  );

  // نام و وضعیت از دادهٔ تازهٔ سرور می‌آید تا پس از هر ذخیره بدون بستن دیالوگ به‌روز باشد.
  const translations = method?.translations ?? [];
  const effectiveLabel = (
    translations.find((t) => t.languageId === activeLanguageId)?.name ?? row.label
  ).trim();
  const effectiveIsActive = method ? method.isActive === true : row.isActive;

  return (
    <Dialog
      open
      onClose={onClose}
      title={readOnly ? `مشاهدهٔ روش «${effectiveLabel || row.methodCode}»` : `ویرایش روش «${effectiveLabel || row.methodCode}»`}
      showCloseButton
      size="xl"
    >
      <div className="max-h-[78vh] space-y-3 overflow-y-auto" data-testid={`admin-shipping-method-detail-${row.id}`}>
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px] text-gray-500">
          <span data-testid="admin-shipping-method-detail-title" className="text-sm font-bold text-gray-900">
            {effectiveLabel || row.methodCode}
          </span>
          <span>
            کد روش: <span dir="ltr" className="font-mono">{row.methodCode}</span>
          </span>
          <span>{method ? (method.isActive ? "فعال" : "غیرفعال") : ""}</span>
        </div>

        <div className="flex flex-wrap gap-2 border-b border-gray-200 pb-2" role="tablist" aria-label="بخش‌های این روش">
          {FLAT_DETAIL_TAB_IDS.map((id) => {
            const selected = id === activeTab;
            return (
              <button
                key={id}
                type="button"
                role="tab"
                aria-selected={selected}
                aria-controls={`method-panel-${id}`}
                id={`method-tab-${id}`}
                tabIndex={selected ? 0 : -1}
                data-testid={`admin-shipping-method-tab-${id}`}
                className={
                  selected
                    ? "inline-flex min-h-9 items-center rounded-lg bg-[#2563EB] px-3 text-xs font-bold text-white"
                    : "inline-flex min-h-9 items-center rounded-lg border border-gray-200 px-3 text-xs font-semibold text-gray-600"
                }
                onClick={() => onTabChange(id)}
              >
                {FLAT_DETAIL_TAB_LABELS[id]}
              </button>
            );
          })}
        </div>

        <div role="tabpanel" id={`method-panel-${activeTab}`} aria-labelledby={`method-tab-${activeTab}`} tabIndex={0}>
          {activeTab === "general" ? (
            <FlatGeneralTab
              methodCode={row.methodCode}
              label={effectiveLabel}
              isActive={effectiveIsActive}
              translations={translations}
              readOnly={readOnly}
              activeLanguageId={activeLanguageId}
              binding={binding ?? UNRESOLVED_BINDING}
              languages={languages}
              languagesState={languagesState}
              onRetryLanguages={() => void loadLanguages()}
              onNotice={onNotice}
              onSavedInPlace={onRefreshed}
              onSavedAndClose={onSaved}
            />
          ) : activeTab === "rates" ? (
            <ShippingOneLevelRatesTab methodCode={row.methodCode} onNotice={onNotice} bindingOverride={bindingOverride} readOnly={readOnly} />
          ) : activeTab === "schedule" ? (
            <ShippingOneLevelScheduleTab methodCode={row.methodCode} onNotice={onNotice} bindingOverride={bindingOverride} readOnly={readOnly} />
          ) : (
            <ShippingOneLevelPackagingTab methodCode={row.methodCode} onNotice={onNotice} bindingOverride={bindingOverride} />
          )}
        </div>
      </div>
    </Dialog>
  );
}
