"use client";

/**
 * TB-SHIPPING-FULL-OPERATIONS-078 — «بررسی هزینه و شرایط ارسال»: one bounded, discoverable
 * per-service inspector on the existing `/admin/shipping-services` screen, sitting next to the
 * existing rate/coverage, schedule and packaging actions.
 *
 * It is one dialog, not a new route, not a mega-form and not a writer. It calls exactly one
 * existing authorized read-only backend route
 * (`POST /v1/admin/shipping-services/{serviceCode}/operational-quote`) and renders the answer field
 * by field. The service code always comes from the clicked grid row.
 *
 * What the dialog deliberately does **not** do:
 * - it never computes a price, so no client-side pricing authority is created;
 * - it never renders a nullable amount as `0`;
 * - it never claims capacity is reserved or enforced;
 * - it never calls a carrier pickup window a customer delivery promise;
 * - it never writes a rate, coverage, packaging policy or schedule.
 */
import { useCallback, useEffect, useMemo, useState } from "react";
import { Dialog } from "../../../design-system";
import {
  evaluateAdminShippingOperationalQuote,
  type ShippingOperationalQuoteResult,
} from "./shipping-operational-quote-api.ts";
import {
  CAPACITY_CONFIG_ONLY_QUOTE_FA,
  COLLECT_ON_DELIVERY_EXPLANATION_FA,
  DEFAULT_QUOTE_CURRENCY,
  NOT_DECLARED_FA,
  NOT_EVALUATED_FA,
  PACKAGING_ALWAYS_CHECKOUT_FA,
  PAYER_SPLIT_IS_QUOTE_FACT_FA,
  PICKUP_WINDOW_IS_NOT_PROMISE_FA,
  PREPAID_EXPLANATION_FA,
  QUOTE_IS_DIAGNOSTIC_FA,
  availabilityLabelFa,
  conditionMatchFa,
  currencyMatchesRequest,
  emptyQuoteForm,
  formatAmountFa,
  isQuotedResult,
  mapQuoteErrorFa,
  normalizeCurrencyCode,
  outcomeLabelFa,
  parseOptionalWeightGrams,
  payerModeLabelFa,
  quoteFormToPayload,
  quoteReasonFa,
  resolvePickupToUtc,
  scheduleSummaryFa,
  validateQuoteForm,
  type ShippingOperationalQuoteFormValues,
} from "./shipping-operational-quote-model.ts";
import {
  buildScopeWrite,
  EMPTY_DESTINATION_SELECTION,
  scopeLevelOf,
  scopeSummary,
  validateDestinationSelection,
  type DestinationSelection,
  type ShippingServiceOptionChoice,
} from "./shipping-rates-coverage-model.ts";
import { SERVICE_LEVEL_OPTION_VALUE } from "./shipping-rates-coverage-model.ts";
import { ShippingDestinationPicker } from "./shipping-destination-picker.tsx";
import { loadShippingMethodTree } from "../shipping-services-api.ts";

type Props = {
  open: boolean;
  serviceCode: string;
  serviceName: string;
  onClose: () => void;
};

/** برچسب فارسی سطح دامنه برای نمایش خلاصه؛ همان معنای مسیرهای نوشتنی. */
const TIME_ZONE_SUGGESTIONS = ["Asia/Tehran", "UTC"];

export function ShippingOperationalQuoteDialog({ open, serviceCode, serviceName, onClose }: Props) {
  const [form, setForm] = useState<ShippingOperationalQuoteFormValues>(emptyQuoteForm());
  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [options, setOptions] = useState<ShippingServiceOptionChoice[]>([]);
  const [issues, setIssues] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<ShippingOperationalQuoteResult | null>(null);
  const [failure, setFailure] = useState<string | null>(null);

  const loadOptions = useCallback(async () => {
    const tree = await loadShippingMethodTree();
    if (tree.state !== "ok" || !tree.data) {
      setOptions([]);
      return;
    }
    const mine = tree.data.find((item) => item.code === serviceCode);
    setOptions(
      (mine?.options ?? [])
        .filter((option) => option.code.trim().length > 0)
        .map((option) => ({ code: option.code, labelFa: option.labelFa || option.name || option.code })),
    );
  }, [serviceCode]);

  useEffect(() => {
    if (open) {
      setForm(emptyQuoteForm());
      setSelection(EMPTY_DESTINATION_SELECTION);
      setIssues([]);
      setResult(null);
      setFailure(null);
      void loadOptions();
    }
  }, [open, loadOptions]);

  const weightPreview = useMemo(() => {
    const parsed = parseOptionalWeightGrams(form.weightKilograms);
    if (parsed.value == null) return "";
    return `${parsed.value.toLocaleString("fa-IR")} گرم`;
  }, [form.weightKilograms]);

  const pickupPreview = useMemo(() => {
    const resolved = resolvePickupToUtc(form.pickupLocal, form.timeZoneId);
    if (resolved.kind === "resolved") {
      return `${resolved.utcIso} (${resolved.offsetLabel} → UTC)`;
    }
    if (resolved.kind === "empty") return NOT_EVALUATED_FA;
    return resolved.messageFa;
  }, [form.pickupLocal, form.timeZoneId]);

  async function submit() {
    const scopeProblems = validateDestinationSelection(selection);
    const formProblems = validateQuoteForm(form).map((issue) => issue.messageFa);
    if (scopeProblems.length > 0 || formProblems.length > 0) {
      setIssues([...scopeProblems, ...formProblems]);
      setResult(null);
      setFailure(null);
      return;
    }

    setIssues([]);
    setBusy(true);
    setResult(null);
    setFailure(null);
    const response = await evaluateAdminShippingOperationalQuote(
      serviceCode,
      quoteFormToPayload(form, buildScopeWrite(selection)),
    );
    setBusy(false);
    if (response.state !== "ok" || !response.data) {
      setFailure(
        response.state === "denied"
          ? "دسترسی لازم برای ارزیابی عملیاتی را ندارید (۴۰۱/۴۰۳)."
          : mapQuoteErrorFa(response.message, "ارزیابی عملیاتی ناموفق بود."),
      );
      return;
    }
    setResult(response.data);
  }

  const optionsMissing = options.length === 0;

  return (
    <Dialog open={open} onClose={onClose} title={`بررسی هزینه و شرایط ارسال — ${serviceName}`} size="xl">
      <div className="max-h-[78vh] space-y-4 overflow-y-auto" data-testid="shipping-operational-quote-dialog">
        <p className="text-xs text-gray-600">
          کد خدمت: <span dir="ltr" className="font-mono">{serviceCode}</span>
        </p>

        <p className="rounded-lg bg-amber-50 p-3 text-[11px] font-semibold text-amber-900" data-testid="shipping-quote-diagnostic-note">
          {QUOTE_IS_DIAGNOSTIC_FA}
        </p>
        <p className="rounded-lg bg-gray-50 p-3 text-[11px] text-gray-600" data-testid="shipping-quote-promise-note">
          {PICKUP_WINDOW_IS_NOT_PROMISE_FA}
        </p>

        <section className="space-y-3 rounded-lg border border-gray-200 p-3">
          <h3 className="text-sm font-bold text-gray-800">ورودی‌های ارزیابی</h3>

          <ShippingDestinationPicker selection={selection} onChange={setSelection} disabled={busy} />

          <div className="grid gap-2 sm:grid-cols-3">
            <label className="text-[11px] text-gray-600">
              گزینهٔ خدمت
              <select
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                value={form.optionCode ?? SERVICE_LEVEL_OPTION_VALUE}
                onChange={(e) => {
                  const raw = e.target.value;
                  setForm({ ...form, optionCode: raw === SERVICE_LEVEL_OPTION_VALUE ? null : raw });
                  setIssues([]);
                }}
                disabled={busy}
                data-testid="shipping-quote-option"
              >
                <option value={SERVICE_LEVEL_OPTION_VALUE}>سطح خدمت (بدون گزینهٔ اختصاصی)</option>
                {options.map((option) => (
                  <option key={option.code} value={option.code}>
                    {option.labelFa} ({option.code})
                  </option>
                ))}
              </select>
            </label>

            <label className="text-[11px] text-gray-600">
              ارز
              <input
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                dir="ltr"
                value={form.currencyCode}
                onChange={(e) => { setForm({ ...form, currencyCode: e.target.value }); setIssues([]); }}
                disabled={busy}
                data-testid="shipping-quote-currency"
              />
            </label>

            <label className="text-[11px] text-gray-600">
              جمع سبد (IRR، بدون هزینهٔ ارسال)
              <input
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                dir="ltr"
                inputMode="decimal"
                placeholder="500000"
                value={form.cartSubtotal}
                onChange={(e) => { setForm({ ...form, cartSubtotal: e.target.value }); setIssues([]); }}
                disabled={busy}
                data-testid="shipping-quote-subtotal"
              />
            </label>

            <label className="text-[11px] text-gray-600">
              وزن اندازه‌گیری‌شدهٔ مرسوله (کیلوگرم)
              <input
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                dir="ltr"
                inputMode="decimal"
                placeholder="1.999"
                value={form.weightKilograms}
                onChange={(e) => { setForm({ ...form, weightKilograms: e.target.value }); setIssues([]); }}
                disabled={busy}
                data-testid="shipping-quote-weight"
              />
              <span className="mt-1 block text-[10px] text-gray-500" data-testid="shipping-quote-weight-preview">
                {weightPreview ? `معادل دقیق: ${weightPreview} (بدون گرد کردن)` : "خالی = وزن اعلام نشده"}
              </span>
            </label>

            <label className="text-[11px] text-gray-600">
              تعداد اقلام فیزیکی مرسوله
              <input
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                dir="ltr"
                inputMode="numeric"
                placeholder="2"
                value={form.itemCount}
                onChange={(e) => { setForm({ ...form, itemCount: e.target.value }); setIssues([]); }}
                disabled={busy}
                data-testid="shipping-quote-item-count"
              />
              <span className="mt-1 block text-[10px] text-gray-500">تعداد اقلام مرسوله، نه خطوط سبد</span>
            </label>

            <label className="text-[11px] text-gray-600">
              لحظهٔ تحویل به حامل (ساعت محلی)
              <input
                type="datetime-local"
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                dir="ltr"
                value={form.pickupLocal}
                onChange={(e) => { setForm({ ...form, pickupLocal: e.target.value }); setIssues([]); }}
                disabled={busy}
                data-testid="shipping-quote-pickup-local"
              />
              <span className="mt-1 block text-[10px] text-gray-500">
                این ورودی «ساعت محلی» در منطقهٔ زمانی زیر است، نه ساعت مرورگر.
              </span>
            </label>

            <label className="text-[11px] text-gray-600">
              منطقهٔ زمانی (IANA)
              <input
                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                dir="ltr"
                list="shipping-quote-timezones"
                value={form.timeZoneId}
                onChange={(e) => { setForm({ ...form, timeZoneId: e.target.value }); setIssues([]); }}
                disabled={busy}
                data-testid="shipping-quote-timezone"
              />
              <datalist id="shipping-quote-timezones">
                {TIME_ZONE_SUGGESTIONS.map((zone) => <option key={zone} value={zone} />)}
              </datalist>
              <span className="mt-1 block text-[10px] text-gray-500" data-testid="shipping-quote-pickup-preview">
                {pickupPreview}
              </span>
            </label>
          </div>

          {optionsMissing && (
            <p className="text-[11px] text-gray-500" data-testid="shipping-quote-no-options">
              این خدمت هیچ گزینهٔ اختصاصی ندارد؛ ارزیابی در سطح خدمت انجام می‌شود.
            </p>
          )}

          {issues.length > 0 && (
            <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-quote-issues">
              {issues.map((issue) => <li key={issue}>{issue}</li>)}
            </ul>
          )}

          <button
            type="button"
            className="rounded bg-blue-600 px-3 py-1 text-xs font-bold text-white disabled:opacity-50"
            disabled={busy}
            onClick={() => void submit()}
            data-testid="shipping-quote-submit"
          >
            {busy ? "در حال ارزیابی…" : "ارزیابی هزینه و شرایط"}
          </button>
        </section>

        {failure && (
          <p className="rounded-lg bg-red-50 p-3 text-[11px] font-semibold text-red-700" data-testid="shipping-quote-failure">
            {failure}
          </p>
        )}

        {result && (
          <section className="space-y-3 rounded-lg border border-gray-200 p-3" data-testid="shipping-quote-result">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="text-sm font-bold text-gray-800">نتیجهٔ ارزیابی</h3>
              <span
                className={isQuotedResult(result) ? "text-[11px] font-bold text-emerald-700" : "text-[11px] font-bold text-amber-700"}
                data-testid="shipping-quote-outcome"
              >
                {outcomeLabelFa(result.outcome)} — {quoteReasonFa(result.reasonCode)}
              </span>
            </div>

            {!currencyMatchesRequest(form.currencyCode, result) && (
              <p className="rounded-lg bg-red-50 p-2 text-[11px] font-semibold text-red-700" data-testid="shipping-quote-currency-mismatch">
                ارز پاسخ ({result.currencyCode}) با ارز درخواست ({normalizeCurrencyCode(form.currencyCode)}) یکسان نیست؛
                مبالغ اعلام‌شده با ارز پاسخ نمایش داده می‌شوند و هیچ تبدیل خودکاری انجام نمی‌شود.
              </p>
            )}

            <dl className="grid gap-1 text-[11px] text-gray-700 sm:grid-cols-2">
              <div className="flex justify-between gap-2">
                <dt>کرایهٔ حامل</dt>
                <dd data-testid="shipping-quote-freight">{formatAmountFa(result.freightAmount, result.currencyCode)}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt>هزینهٔ بسته‌بندی</dt>
                <dd data-testid="shipping-quote-packaging">{formatAmountFa(result.packagingAmount, result.currencyCode)}</dd>
              </div>
              <div className="flex justify-between gap-2 font-bold">
                <dt>جمع هزینهٔ خدمت</dt>
                <dd data-testid="shipping-quote-total">{formatAmountFa(result.totalServiceExpense, result.currencyCode)}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt>قابل پرداخت هنگام تسویه</dt>
                <dd data-testid="shipping-quote-checkout">{formatAmountFa(result.amountPayableAtCheckout, result.currencyCode)}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt>وصول هنگام تحویل</dt>
                <dd data-testid="shipping-quote-cod">{formatAmountFa(result.amountCollectOnDelivery, result.currencyCode)}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt>طرف پرداخت کرایه</dt>
                <dd data-testid="shipping-quote-payer">{payerModeLabelFa(result.freightPayerMode)}</dd>
              </div>
            </dl>

            <div className="space-y-1 text-[11px] text-gray-600">
              <p data-testid="shipping-quote-payer-explanation">
                {result.freightPayerMode === "CollectOnDelivery" ? COLLECT_ON_DELIVERY_EXPLANATION_FA : PREPAID_EXPLANATION_FA}
              </p>
              <p>{PACKAGING_ALWAYS_CHECKOUT_FA}</p>
              <p className="font-semibold text-amber-900" data-testid="shipping-quote-payer-boundary">
                {PAYER_SPLIT_IS_QUOTE_FACT_FA}
              </p>
            </div>

            <div className="space-y-1 rounded-lg bg-gray-50 p-2 text-[11px] text-gray-700">
              <p data-testid="shipping-quote-schedule">وضعیت بازهٔ حامل: {scheduleSummaryFa(result.schedule)}</p>
              <p data-testid="shipping-quote-schedule-reason">
                کد دلیل بازه: {quoteReasonFa(result.schedule?.reasonCode)}
              </p>
              <p className="font-semibold" data-testid="shipping-quote-capacity">{CAPACITY_CONFIG_ONLY_QUOTE_FA}</p>
              {result.schedule?.availability === "NotEvaluated" && (
                <p className="font-semibold text-amber-900" data-testid="shipping-quote-not-evaluated">
                  {NOT_EVALUATED_FA} — در این حالت «قابل رزرو» ادعا نمی‌شود.
                </p>
              )}
            </div>

            <div className="space-y-1 text-[11px] text-gray-700">
              <p data-testid="shipping-quote-condition">پلهٔ اندازهٔ منطبق: {conditionMatchFa(result.matchedCondition)}</p>
              <p data-testid="shipping-quote-eligible">
                واجد شرایط عملیاتی: {isQuotedResult(result) && (result.schedule?.isConstraintSatisfied ?? false) ? "بله" : "خیر"}
              </p>
              <p data-testid="shipping-quote-freight-reason">
                کد دلیل کرایه: {result.carrierFreight ? quoteReasonFa(result.carrierFreight.reasonCode) : NOT_DECLARED_FA}
              </p>
              <p data-testid="shipping-quote-packaging-reason">
                کد دلیل بسته‌بندی: {result.packaging ? quoteReasonFa(result.packaging.reasonCode) : NOT_DECLARED_FA}
              </p>
              <p className="text-[10px] text-gray-500" data-testid="shipping-quote-scope-echo">
                دامنهٔ ارزیابی‌شده:{" "}
                {scopeSummary({
                  level: scopeLevelOf(selection) ?? "",
                  ...buildScopeWrite(selection),
                })}
              </p>
            </div>
          </section>
        )}

        <div className="flex justify-end border-t border-gray-100 pt-3">
          <button type="button" className="rounded border border-gray-300 px-3 py-1 text-xs" onClick={onClose}>
            بستن
          </button>
        </div>
      </div>
    </Dialog>
  );
}

/** ارز پیش‌فرض فرم برای تست‌های مدل و نمایش؛ همان IRR. */
export const DEFAULT_QUOTE_CURRENCY_CODE = DEFAULT_QUOTE_CURRENCY;
