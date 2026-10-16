"use client";

/**
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146 — نمای جزئیات یک روش با چهار زبانهٔ غیرتودرتو.
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
 * اجزای سه زبانهٔ تخصصی همان کامپوننت‌های موجودند و منطق تجاری‌شان دوباره نوشته نشده است.
 */

import { useEffect, useMemo, useState } from "react";
import { Dialog } from "../../../design-system";
import { FlatGeneralTab } from "./flat-general-tab.tsx";
import { ShippingOneLevelRatesTab } from "./shipping-one-level-rates-tab.tsx";
import { ShippingOneLevelScheduleTab } from "./shipping-one-level-schedule-tab.tsx";
import { ShippingOneLevelPackagingTab } from "./shipping-one-level-packaging-tab.tsx";
import { loadOneLevelMethodBinding } from "./shipping-one-level-activation-model.ts";
import type { OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
import type { FlatMethodRow } from "./flat-method-rows.ts";

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
  mode,
  activeTab,
  onTabChange,
  activeLanguageId,
  onClose,
  onNotice,
  onSaved,
}: {
  row: FlatMethodRow;
  mode: "view" | "edit";
  activeTab: FlatDetailTabId;
  onTabChange: (tab: FlatDetailTabId) => void;
  activeLanguageId: string;
  onClose: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  onSaved: (text: string) => Promise<void>;
}) {
  const readOnly = mode === "view";
  const [binding, setBinding] = useState<OneLevelBindingState | null>(null);

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

  return (
    <Dialog
      open
      onClose={onClose}
      title={readOnly ? `مشاهدهٔ روش «${row.label || row.methodCode}»` : `ویرایش روش «${row.label || row.methodCode}»`}
      showCloseButton
      size="xl"
    >
      <div className="max-h-[78vh] space-y-4 overflow-y-auto" data-testid={`admin-shipping-method-detail-${row.id}`}>
        <div className="rounded-xl border border-gray-100 bg-gray-50 p-3">
          <p className="text-sm font-bold text-gray-900" data-testid="admin-shipping-method-detail-title">
            {row.label || row.methodCode}
          </p>
          <p className="mt-1 text-[11px] text-gray-500">
            کد روش: <span dir="ltr" className="font-mono">{row.methodCode}</span>
          </p>
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
                    ? "inline-flex min-h-10 items-center rounded-lg bg-[#2563EB] px-3 text-sm font-bold text-white"
                    : "inline-flex min-h-10 items-center rounded-lg border border-gray-200 px-3 text-sm font-semibold text-gray-600"
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
              row={row}
              readOnly={readOnly}
              activeLanguageId={activeLanguageId}
              binding={binding ?? UNRESOLVED_BINDING}
              onNotice={onNotice}
              onSaved={onSaved}
            />
          ) : activeTab === "rates" ? (
            <ShippingOneLevelRatesTab methodCode={row.methodCode} onNotice={onNotice} bindingOverride={bindingOverride} />
          ) : activeTab === "schedule" ? (
            <ShippingOneLevelScheduleTab methodCode={row.methodCode} onNotice={onNotice} bindingOverride={bindingOverride} />
          ) : (
            <ShippingOneLevelPackagingTab methodCode={row.methodCode} onNotice={onNotice} bindingOverride={bindingOverride} />
          )}
        </div>
      </div>
    </Dialog>
  );
}
