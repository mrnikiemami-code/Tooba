"use client";

/**
 * زبانهٔ «عمومی» یک روش یک‌سطحی — فشرده، RTL و مبتنی بر همان دو نوشتن مجاز قفل‌شده.
 *
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
 * TB-SHIPPING-ADMIN-MULTILINGUAL-UX-148
 * TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155
 *
 * قواعد سخت:
 * - تنها دو نوشتن مجاز این زبانه همان دو نوشتن مجاز قفل‌شدهٔ کاتالوگ یک‌سطحی است:
 *   ۱) نام نمایشی روش در یک زبان (۰۹۴)؛ ۲) فعال/غیرفعال‌سازی روش گزینه‌دار (۰۹۶).
 *   هیچ نوشتن دیگری روی سرویس/گزینه/نرخ/پوشش/برنامه/بسته‌بندی از این زبانه انجام نمی‌شود.
 * - هویت ارائه‌دهنده **فرادادهٔ مشترک** است: در یک بخش تاشوی کوچک و برچسب‌دار نمایش داده می‌شود و
 *   هیچ سیاست مشترکی به‌عنوان سیاست اختصاصی این روش نوشته نمی‌شود.
 * - وضعیت «فعال/غیرفعال بودن روش» و «آماده‌بودن مالی» دو وضعیت جداگانه‌اند. از TB-155 وضعیت مالی
 *   از یک read model فقط‌خواندنیِ **سرور** خوانده می‌شود که مالک همان ردیف‌های نرخ/پوشش است. اگر
 *   آن خواندن ممکن نباشد (پیوند تأییدنشده، مجوز، شبکه یا پاسخ نامعتبر) این زبانه هیچ وضعیت
 *   «آماده»، هیچ مبلغ و هیچ فرض صفر نمی‌سازد و فقط نبود خواندن صریح را نشان می‌دهد.
 * - هویت مالی از پیوند authoritative سرور می‌آید؛ کد روش هرگز در مرورگر تفکیک نمی‌شود.
 */

import { useEffect, useState } from "react";
import { Badge, Button } from "../../../design-system";
import { FlatMethodLanguageEditor, type MethodLanguagesState } from "./flat-method-language-editor.tsx";
import type { MethodLanguageSource } from "./shipping-method-language-model.ts";
import {
  ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA,
  activationActionFa,
  activationConfirmFa,
  activationErrorFa,
  activationImpactFa,
  activationStatusFa,
  activationUnavailableFa,
  resolveActivationEligibility,
  setOneLevelMethodActivation,
} from "./shipping-one-level-activation-model.ts";
import type { OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
import { loadShippingMethodFinancialStatus } from "./shipping-method-financial-api.ts";
import {
  FINANCIAL_MODE_TABLE_FA,
  financialCaveatsFa,
  financialMoneyFa,
  financialReasonFa,
  financialStateLabelFa,
  financialStateTone,
  type ShippingMethodFinancialStatus,
} from "./shipping-method-financial-model.ts";

/** متن صریح نبود خواندن وضعیت مالی؛ هیچ وضعیت «آماده» یا مبلغی ساخته نمی‌شود. */
export const FINANCIAL_STATUS_UNAVAILABLE_FA =
  "وضعیت مالی این روش از API فعلی قابل خواندن نیست؛ هیچ مبلغ، حالت مالی یا فرض صفر ساخته نمی‌شود.";

export const FINANCIAL_STATUS_DISTINCTNESS_FA =
  "«فعال بودن روش» با «آمادهٔ استعلام بودن قیمت» یکسان نیست: روش فعال بدون تعرفه، رایگان نیست.";

type FinancialReadState =
  | { state: "loading" }
  | { state: "unavailable" }
  | { state: "denied" }
  | { state: "error" }
  | { state: "ok"; status: ShippingMethodFinancialStatus };

export function FlatGeneralTab({
  methodCode,
  label,
  isActive,
  translations,
  readOnly,
  activeLanguageId,
  binding,
  languages,
  languagesState,
  onRetryLanguages,
  onNotice,
  onSavedInPlace,
  onSavedAndClose,
}: {
  methodCode: string;
  label: string;
  isActive: boolean;
  translations: readonly { languageId: string; name: string }[];
  readOnly: boolean;
  activeLanguageId: string;
  binding: OneLevelBindingState;
  languages: readonly MethodLanguageSource[];
  languagesState: MethodLanguagesState;
  onRetryLanguages: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /** پس از ذخیرهٔ موفق، منبع حقیقت دوباره خوانده می‌شود و همین دیالوگ باز می‌ماند. */
  onSavedInPlace: (text: string) => Promise<void>;
  /** پس از تغییر فعال‌بودن، فهرست دوباره خوانده می‌شود و دیالوگ بسته می‌شود. */
  onSavedAndClose: (text: string) => Promise<void>;
}) {
  const [activationBusy, setActivationBusy] = useState(false);
  const [financial, setFinancial] = useState<FinancialReadState>({ state: "loading" });

  // روش عوض شده است ⇒ هیچ وضعیت در پرواز قبلی روی روش تازه نمی‌نشیند.
  useEffect(() => setActivationBusy(false), [methodCode]);

  const eligibility = resolveActivationEligibility(binding, methodCode);
  const resolved = binding.state === "resolved" ? binding.binding : null;
  const displayName = label.trim() || methodCode;

  // وضعیت مالی فقط با هویت تأییدشدهٔ سرور خوانده می‌شود؛ بدون پیوند resolved هیچ درخواستی
  // فرستاده نمی‌شود و هیچ وضعیت مالی جعلی ساخته نمی‌شود (fail-closed).
  const serviceCode = resolved?.serviceCode ?? null;
  const optionCode = resolved?.optionCode ?? null;
  useEffect(() => {
    let current = true;
    if (!resolved || !serviceCode) {
      setFinancial({ state: "unavailable" });
      return () => {
        current = false;
      };
    }

    setFinancial({ state: "loading" });
    void (async () => {
      const result = await loadShippingMethodFinancialStatus(serviceCode, optionCode);
      if (!current) return;
      if (result.state === "ok" && result.data) {
        setFinancial({ state: "ok", status: result.data });
        return;
      }
      setFinancial(result.state === "denied" ? { state: "denied" } : { state: "error" });
    })();

    return () => {
      current = false;
    };
  }, [methodCode, serviceCode, optionCode, resolved]);

  async function applyActivation() {
    if (eligibility.state !== "supported") return;
    const next = !eligibility.currentIsActive;
    setActivationBusy(true);
    const result = await setOneLevelMethodActivation(methodCode, next);
    setActivationBusy(false);
    if (result.state !== "ok" || !result.data) {
      onNotice({ tone: "error", text: activationErrorFa(result.message ?? "") });
      return;
    }
    await onSavedAndClose(
      `روش «${displayName}» ${activationStatusFa(result.data.isActive)} شد.`,
    );
  }

  return (
    <div className="space-y-2" data-testid="admin-shipping-method-general">
      <header
        className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 rounded-xl border border-gray-200 bg-gray-50 px-3 py-2"
        data-testid="admin-shipping-method-header"
      >
        <div className="min-w-0">
          <h2 className="truncate text-base font-black text-gray-900" data-testid="admin-shipping-method-header-name">
            {displayName}
          </h2>
          <p className="text-[11px] text-gray-500">
            کد روش (تغییرناپذیر): <span dir="ltr" className="font-mono">{methodCode}</span>
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Badge tone={isActive ? "success" : "neutral"}>{activationStatusFa(isActive)}</Badge>
          <Badge tone={financial.state === "ok" ? financialStateTone(financial.status.state) : "warning"}>
            {financial.state === "ok" ? financialStateLabelFa(financial.status.state) : "وضعیت مالی: نامشخص"}
          </Badge>
        </div>
      </header>

      <details
        className="rounded-xl border border-gray-200 px-3 py-2"
        data-testid="admin-shipping-method-shared-metadata"
      >
        <summary className="cursor-pointer text-xs font-bold text-gray-700">
          فرادادهٔ مشترک ارائه‌دهنده (خارج از دامنهٔ این روش)
        </summary>
        <dl className="mt-2 grid gap-1 text-[11px] text-gray-600 sm:grid-cols-2">
          <div className="flex gap-1">
            <dt className="font-semibold">سرویس authoritative:</dt>
            <dd dir="ltr" className="font-mono">{resolved ? resolved.serviceCode : "—"}</dd>
          </div>
          <div className="flex gap-1">
            <dt className="font-semibold">گزینه authoritative:</dt>
            <dd dir="ltr" className="font-mono">
              {resolved ? (resolved.optionCode ?? "سطح سرویس (بدون گزینه)") : "—"}
            </dd>
          </div>
        </dl>
        <p className="mt-2 text-[11px] text-gray-500">
          این هویت بین همهٔ روش‌های یک ارائه‌دهنده مشترک است و سیاست اختصاصی این روش نیست؛ در این صفحه
          ویرایش نمی‌شود تا هیچ سیاست مشترکی به‌جای سیاست این روش نوشته نشود.
          {binding.state === "untrusted" ? " پیوند این روش از سرور تأیید نشد." : null}
        </p>
      </details>

      <FlatMethodLanguageEditor
        methodCode={methodCode}
        translations={translations}
        languages={languages}
        languagesState={languagesState}
        defaultLanguageId={activeLanguageId}
        readOnly={readOnly}
        onRetryLanguages={onRetryLanguages}
        onNotice={onNotice}
        onSaved={onSavedInPlace}
      />

      <section className="space-y-2 rounded-xl border border-gray-200 p-3" data-testid="admin-shipping-method-activation">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-sm font-bold text-gray-900">فعال‌بودن روش در فروشگاه</h2>
          <Badge tone={isActive ? "success" : "neutral"}>{activationStatusFa(isActive)}</Badge>
        </div>
        {eligibility.state === "supported" ? (
          <>
            <p className="text-xs text-gray-600">{activationImpactFa(eligibility.currentIsActive)}</p>
            <p className="text-[11px] text-gray-500">
              وضعیت تأییدشدهٔ سرور: <span className="font-bold">{activationStatusFa(eligibility.currentIsActive)}</span>
            </p>
            {readOnly ? (
              <p className="text-xs text-gray-500">برای تغییر وضعیت، روش را در حالت «ویرایش» باز کنید.</p>
            ) : (
              <div className="flex justify-end">
                <Button
                  type="button"
                  tone="secondary"
                  data-testid="admin-shipping-method-activation-toggle"
                  disabled={activationBusy}
                  onClick={() => {
                    if (!window.confirm(activationConfirmFa(eligibility.currentIsActive))) return;
                    void applyActivation();
                  }}
                >
                  {activationBusy ? "در حال اعمال…" : activationActionFa(eligibility.currentIsActive)}
                </Button>
              </div>
            )}
          </>
        ) : eligibility.state === "optionless" ? (
          <p className="text-xs text-amber-800" data-testid="admin-shipping-method-activation-unsupported">
            {ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA}
          </p>
        ) : (
          <p className="text-xs text-amber-800" data-testid="admin-shipping-method-activation-unavailable">
            {activationUnavailableFa(eligibility.reason)}
          </p>
        )}
      </section>

      <section
        className="space-y-1 rounded-xl border border-amber-200 bg-amber-50 p-3"
        data-testid="admin-shipping-method-financial-status"
      >
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-sm font-bold text-amber-900">وضعیت مالی مشتری</h2>
          <Badge tone={financial.state === "ok" ? financialStateTone(financial.status.state) : "warning"}>
            {financial.state === "ok" ? financialStateLabelFa(financial.status.state) : "قابل خواندن از API فعلی نیست"}
          </Badge>
        </div>
        {financial.state === "ok" ? (
          <div className="space-y-1" data-testid="admin-shipping-method-financial-verified">
            <p className="text-xs font-semibold text-emerald-900" data-testid="admin-shipping-method-financial-reason">
              {financialReasonFa(financial.status)}
            </p>
            {financialMoneyFa(financial.status) ? (
              <p className="text-xs text-emerald-900" data-testid="admin-shipping-method-financial-money" dir="rtl">
                {financialMoneyFa(financial.status)}
              </p>
            ) : null}
            {financialCaveatsFa(financial.status).map((caveat) => (
              <p key={caveat} className="text-[11px] text-amber-900" data-testid="admin-shipping-method-financial-caveat">
                {caveat}
              </p>
            ))}
            <p className="text-[11px] text-amber-800">{FINANCIAL_STATUS_DISTINCTNESS_FA}</p>
            <details className="text-[11px] text-amber-800" data-testid="admin-shipping-method-financial-modes">
              <summary className="cursor-pointer font-bold">حالت‌های مالی اثبات‌پذیر در اسکیمای فعلی</summary>
              <ul className="mt-1 list-disc space-y-0.5 pr-4">
                {FINANCIAL_MODE_TABLE_FA.map((row) => (
                  <li key={row}>{row}</li>
                ))}
              </ul>
            </details>
            <p className="text-[11px] text-amber-800">
              منبع این وضعیت، همان ردیف‌های واقعی نرخ/پوشش سرویس است و از فعال‌بودن روش یا برچسب آن
              استنتاج نمی‌شود؛ پیوند خدمات سطح سرویس هرگز به‌عنوان تعرفهٔ این روش نمایش داده نمی‌شود.
            </p>
          </div>
        ) : (
          <div className="space-y-1" data-testid="admin-shipping-method-financial-unavailable">
            <p className="text-xs text-amber-900">{FINANCIAL_STATUS_UNAVAILABLE_FA}</p>
            <p className="text-[11px] text-amber-800">{FINANCIAL_STATUS_DISTINCTNESS_FA}</p>
            <p className="text-[11px] text-amber-800">
              حالت‌های مالی معتبر بر اساس قرارداد قفل‌شده (رایگان / مبلغ ثابت / قاعده‌محور / پرداخت در محل /
              استعلام ارائه‌دهنده) فقط از دادهٔ واقعی سرور ادعا می‌شوند؛ تا آن زمان این صفحه نه مبلغی می‌سازد و نه
              روشی را «آمادهٔ قیمت» اعلام می‌کند.
            </p>
          </div>
        )}
      </section>
    </div>
  );
}
