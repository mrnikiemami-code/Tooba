"use client";

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-089 — tab 5 «بررسی هزینه» of the one-level shipping shell.
 *
 * This is the last tab and it is deliberately the most conservative one: a **read-only diagnostic
 * inspector** for the selected method, not a writer, not a price authority and not a customer-facing
 * charge.
 *
 * Scope rules honored here:
 *
 * - **One selected one-level method is the whole context.** There is no option selector and no
 *   provider sublevel; the `optionCode` sent to the endpoint comes only from the server-resolved
 *   binding (including `null` for optionless methods).
 * - **The binding comes only from the server** (086 resolver). `method_code` is never split in the
 *   browser and a user-supplied service/option pair can never substitute for the binding.
 * - **The quotation is reused, not reimplemented.** Money parsing, grams conversion, local-time →
 *   UTC resolution (with its ambiguous/nonexistent refusal), currency handling, labels and reason
 *   codes come unchanged from `shipping-operational-quote-model.ts`. The backend answer is rendered
 *   field by field and never recomputed into checkout pricing.
 * - **The existing POST verb is read-only semantics.** No rate, coverage, packaging policy, schedule,
 *   order, payment or settlement is written.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
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
  quoteReasonFa,
  resolvePickupToUtc,
  scheduleSummaryFa,
  validateQuoteForm,
  type ShippingOperationalQuoteFormValues,
} from "./shipping-operational-quote-model.ts";
import {
  buildScopeWrite,
  EMPTY_DESTINATION_SELECTION,
  scopeSummary,
  validateDestinationSelection,
  type DestinationSelection,
} from "./shipping-rates-coverage-model.ts";
import { ShippingDestinationPicker } from "./shipping-destination-picker.tsx";
import { loadOneLevelBinding } from "./one-level-binding-client.ts";
import { isRetryableBindingFailure } from "./shipping-one-level-rates-model.ts";
import {
  bindingContextFa,
  bindingUnavailableFa,
  createBindingGenerationGuard,
  ONE_LEVEL_QUOTE_AMOUNT_BOUNDARY_FA,
  ONE_LEVEL_QUOTE_BOUNDARY_FA,
  ONE_LEVEL_QUOTE_CAPACITY_FA,
  ONE_LEVEL_QUOTE_INPUT_BOUNDARY_FA,
  ONE_LEVEL_QUOTE_READ_ONLY_FA,
  oneLevelQuotePayload,
  operationStillTargetsMethod,
  quoteMatchesRequestedIdentity,
  resolveQuoteState,
  type OneLevelBindingState,
} from "./shipping-one-level-quote-model.ts";

const TIME_ZONE_SUGGESTIONS = ["Asia/Tehran", "UTC"];

type Props = {
  /** کد روش یک‌سطحی انتخاب‌شده در زبانهٔ «سرویس‌ها». تهی یعنی هیچ روشی انتخاب نشده است. */
  methodCode: string;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
};

export function ShippingOneLevelQuoteTab({ methodCode, onNotice }: Props) {
  const [bindingLoading, setBindingLoading] = useState(false);
  const [binding, setBinding] = useState<OneLevelBindingState | null>(null);
  const [bindingAttempt, setBindingAttempt] = useState(0);
  const [busy, setBusy] = useState(false);
  const [form, setForm] = useState<ShippingOperationalQuoteFormValues>(emptyQuoteForm());
  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [issues, setIssues] = useState<string[]>([]);
  const [result, setResult] = useState<ShippingOperationalQuoteResult | null>(null);
  const [failure, setFailure] = useState<string | null>(null);

  const guard = useRef(createBindingGenerationGuard());
  const resolvedBinding = binding?.state === "resolved" ? binding.binding : null;

  const emit = useCallback(
    (next: { tone: "success" | "error"; text: string }) => onNotice(next),
    [onNotice],
  );

  useEffect(() => {
    // بازنشانی کامل نتیجه و ورودی‌ها هنگام تغییر روش: نتیجهٔ روش قبلی هرگز زیر روش جدید نمی‌ماند.
    setForm(emptyQuoteForm());
    setSelection(EMPTY_DESTINATION_SELECTION);
    setIssues([]);
    setResult(null);
    setFailure(null);
    setBinding(null);

    if (!methodCode) {
      setBindingLoading(false);
      return;
    }

    const isCurrent = guard.current.begin();
    setBindingLoading(true);
    void (async () => {
      // تنها مسیر مجاز خواندن پیوند؛ تفکیک شکست در همان کلاینت مشترک انجام می‌شود.
      const next = await loadOneLevelBinding(methodCode);
      if (!isCurrent()) return;
      setBinding(next);
      setBindingLoading(false);
    })();
  }, [methodCode, bindingAttempt]);

  const state = resolveQuoteState({
    hasMethod: methodCode.length > 0,
    bindingLoading,
    bindingResolved: resolvedBinding != null,
    bindingKnown: binding != null,
  });

  const weightPreview = useMemo(() => {
    const parsed = parseOptionalWeightGrams(form.weightKilograms);
    if (parsed.value == null) return "";
    return `${parsed.value.toLocaleString("fa-IR")} گرم`;
  }, [form.weightKilograms]);

  const pickupPreview = useMemo(() => {
    const resolved = resolvePickupToUtc(form.pickupLocal, form.timeZoneId);
    if (resolved.kind === "resolved") return `${resolved.utcIso} (${resolved.offsetLabel} → UTC)`;
    if (resolved.kind === "empty") return NOT_EVALUATED_FA;
    return resolved.messageFa;
  }, [form.pickupLocal, form.timeZoneId]);

  async function submit() {
    if (!resolvedBinding) return;
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
    const target = resolvedBinding;
    const startedFor = methodCode;
    const isCurrent = guard.current.begin();
    const response = await evaluateAdminShippingOperationalQuote(
      target.serviceCode,
      oneLevelQuotePayload(form, buildScopeWrite(selection), target),
    );
    setBusy(false);
    // روش عوض شده است: پاسخ به روش جدید نسبت داده نمی‌شود و اعلان به هویت اصلی می‌ماند.
    if (!operationStillTargetsMethod(startedFor, methodCode)) return;
    if (response.state !== "ok" || !response.data) {
      setFailure(
        response.state === "denied"
          ? "دسترسی لازم برای ارزیابی عملیاتی را ندارید (۴۰۱/۴۰۳)."
          : mapQuoteErrorFa(response.message, "ارزیابی عملیاتی ناموفق بود."),
      );
      emit({ tone: "error", text: "ارزیابی هزینه برای این روش ناموفق بود." });
      return;
    }
    // پاسخ ناهم‌خوان با هویت درخواستی هرگز نمایش داده نمی‌شود.
    if (!quoteMatchesRequestedIdentity(response.data, target)) {
      setFailure("پاسخ سرور به سرویس/گزینهٔ همین روش تعلق ندارد؛ برای احتیاط هیچ عددی نمایش داده نمی‌شود.");
      return;
    }
    setResult(response.data);
  }

  if (state === "no-method") {
    return (
      <div className="rounded-ds border border-dashed border-border p-6" data-testid="shipping-one-level-quote-no-method">
        <p className="font-medium">هیچ روشی انتخاب نشده است</p>
        <p className="mt-1 text-sm text-muted">
          برای بررسی هزینه، ابتدا در زبانهٔ «سرویس‌ها» یک روش یک‌سطحی را انتخاب کنید.
        </p>
      </div>
    );
  }

  if (state === "binding-loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-quote-binding-loading">
        در حال خواندن پیوند سرویس/گزینهٔ این روش…
      </p>
    );
  }

  if (state === "binding-unavailable") {
    return (
      <div className="rounded-ds border border-border p-6" data-testid="shipping-one-level-quote-binding-unavailable">
        <p className="font-medium">بررسی هزینه برای این روش در دسترس نیست</p>
        <p className="mt-1 text-sm text-muted">{bindingUnavailableFa(binding, methodCode)}</p>
        <p className="mt-2 text-xs text-muted">
          نگاشت کد روش به سرویس/گزینه فقط سمت سرور انجام می‌شود؛ این صفحه هیچ حدسی نمی‌زند و هیچ عددی نمی‌سازد.
        </p>
        {isRetryableBindingFailure(binding) ? (
          <button
            type="button"
            className="mt-3 rounded-ds border border-border px-3 py-1 text-xs"
            data-testid="shipping-one-level-quote-binding-retry"
            onClick={() => setBindingAttempt((value) => value + 1)}
          >
            تلاش دوباره
          </button>
        ) : null}
      </div>
    );
  }

  if (!resolvedBinding) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-quote-unavailable">
        پیوند این روش حل نشد؛ ارزیابی انجام نمی‌شود.
      </p>
    );
  }

  return (
    <div className="space-y-4" data-testid="shipping-one-level-quote-tab">
      <div className="rounded-ds border border-border p-3">
        <p className="text-sm font-medium" data-testid="shipping-one-level-quote-context">
          {bindingContextFa(resolvedBinding)}
        </p>
        <p className="mt-1 text-xs text-muted">
          هر روش یک ردیف مستقل است. ارزیابی روی همان روش انتخاب‌شده انجام می‌شود و هیچ سطح دوم پیکربندی نمی‌سازد.
        </p>
      </div>

      <p className="rounded-ds bg-secondary p-3 text-[11px] font-semibold text-secondary-foreground" data-testid="shipping-one-level-quote-read-only">
        {ONE_LEVEL_QUOTE_READ_ONLY_FA}
      </p>
      <p className="rounded-ds bg-secondary p-3 text-[11px] text-secondary-foreground" data-testid="shipping-one-level-quote-boundary">
        {ONE_LEVEL_QUOTE_BOUNDARY_FA}
      </p>
      <p className="rounded-ds bg-secondary p-3 text-[11px] text-secondary-foreground" data-testid="shipping-one-level-quote-promise-note">
        {PICKUP_WINDOW_IS_NOT_PROMISE_FA}
      </p>

      <section className="space-y-3 rounded-ds border border-border p-3">
        <h3 className="text-sm font-bold">ورودی‌های تشخیصی ارزیابی</h3>
        <p className="text-[11px] text-muted" data-testid="shipping-one-level-quote-input-boundary">
          {ONE_LEVEL_QUOTE_INPUT_BOUNDARY_FA}
        </p>

        <ShippingDestinationPicker selection={selection} onChange={setSelection} disabled={busy} />

        <div className="grid gap-2 sm:grid-cols-3">
          <label className="text-[11px] text-muted">
            ارز
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              value={form.currencyCode}
              onChange={(e) => { setForm({ ...form, currencyCode: e.target.value }); setIssues([]); }}
              disabled={busy}
              data-testid="shipping-one-level-quote-currency"
            />
          </label>

          <label className="text-[11px] text-muted">
            جمع سبد (بدون هزینهٔ ارسال)
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              inputMode="decimal"
              placeholder="500000"
              value={form.cartSubtotal}
              onChange={(e) => { setForm({ ...form, cartSubtotal: e.target.value }); setIssues([]); }}
              disabled={busy}
              data-testid="shipping-one-level-quote-subtotal"
            />
          </label>

          <label className="text-[11px] text-muted">
            وزن تشخیصی مرسوله (کیلوگرم)
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              inputMode="decimal"
              placeholder="1.999"
              value={form.weightKilograms}
              onChange={(e) => { setForm({ ...form, weightKilograms: e.target.value }); setIssues([]); }}
              disabled={busy}
              data-testid="shipping-one-level-quote-weight"
            />
            <span className="mt-1 block text-[10px] text-muted" data-testid="shipping-one-level-quote-weight-preview">
              {weightPreview ? `معادل دقیق: ${weightPreview} (بدون گرد کردن)` : "خالی = وزن اعلام نشده"}
            </span>
          </label>

          <label className="text-[11px] text-muted">
            تعداد اقلام تشخیصی
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              inputMode="numeric"
              placeholder="2"
              value={form.itemCount}
              onChange={(e) => { setForm({ ...form, itemCount: e.target.value }); setIssues([]); }}
              disabled={busy}
              data-testid="shipping-one-level-quote-item-count"
            />
            <span className="mt-1 block text-[10px] text-muted">تعداد اقلام مرسوله، نه خطوط سبد</span>
          </label>

          <label className="text-[11px] text-muted">
            لحظهٔ تحویل به حامل (ساعت محلی)
            <input
              type="datetime-local"
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              value={form.pickupLocal}
              onChange={(e) => { setForm({ ...form, pickupLocal: e.target.value }); setIssues([]); }}
              disabled={busy}
              data-testid="shipping-one-level-quote-pickup-local"
            />
            <span className="mt-1 block text-[10px] text-muted">
              این ورودی «ساعت محلی» در منطقهٔ زمانی زیر است، نه ساعت مرورگر.
            </span>
          </label>

          <label className="text-[11px] text-muted">
            منطقهٔ زمانی (IANA)
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              list="shipping-one-level-quote-timezones"
              value={form.timeZoneId}
              onChange={(e) => { setForm({ ...form, timeZoneId: e.target.value }); setIssues([]); }}
              disabled={busy}
              data-testid="shipping-one-level-quote-timezone"
            />
            <datalist id="shipping-one-level-quote-timezones">
              {TIME_ZONE_SUGGESTIONS.map((zone) => <option key={zone} value={zone} />)}
            </datalist>
            <span className="mt-1 block text-[10px] text-muted" data-testid="shipping-one-level-quote-pickup-preview">
              {pickupPreview}
            </span>
          </label>
        </div>

        {issues.length > 0 && (
          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-quote-issues">
            {issues.map((issue) => <li key={issue}>{issue}</li>)}
          </ul>
        )}

        <button
          type="button"
          className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
          disabled={busy}
          onClick={() => void submit()}
          data-testid="shipping-one-level-quote-submit"
        >
          {busy ? "در حال ارزیابی…" : "بررسی هزینه"}
        </button>
      </section>

      {failure && (
        <p className="rounded-ds border border-red-300 p-3 text-[11px] font-semibold text-red-600" data-testid="shipping-one-level-quote-failure">
          {failure}
        </p>
      )}

      {result && (
        <section className="space-y-3 rounded-ds border border-border p-3" data-testid="shipping-one-level-quote-result">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h3 className="text-sm font-bold">نتیجهٔ ارزیابی تشخیصی</h3>
            <span
              className={isQuotedResult(result) ? "text-[11px] font-bold text-emerald-700" : "text-[11px] font-bold text-amber-700"}
              data-testid="shipping-one-level-quote-outcome"
            >
              {outcomeLabelFa(result.outcome)} — {quoteReasonFa(result.reasonCode)}
            </span>
          </div>

          {!currencyMatchesRequest(form.currencyCode, result) && (
            <p className="text-[11px] font-semibold text-red-600" data-testid="shipping-one-level-quote-currency-mismatch">
              ارز پاسخ ({result.currencyCode}) با ارز درخواست ({normalizeCurrencyCode(form.currencyCode)}) یکسان نیست؛
              مبالغ با ارز پاسخ نمایش داده می‌شوند و هیچ تبدیل خودکاری انجام نمی‌شود.
            </p>
          )}

          <dl className="grid gap-1 text-[11px] sm:grid-cols-2">
            <div className="flex justify-between gap-2">
              <dt>کرایهٔ حامل</dt>
              <dd data-testid="shipping-one-level-quote-freight">{formatAmountFa(result.freightAmount, result.currencyCode)}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>هزینهٔ بسته‌بندی</dt>
              <dd data-testid="shipping-one-level-quote-packaging">{formatAmountFa(result.packagingAmount, result.currencyCode)}</dd>
            </div>
            <div className="flex justify-between gap-2 font-bold">
              <dt>جمع هزینهٔ خدمت</dt>
              <dd data-testid="shipping-one-level-quote-total">{formatAmountFa(result.totalServiceExpense, result.currencyCode)}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>قابل پرداخت هنگام تسویه (تشخیصی)</dt>
              <dd data-testid="shipping-one-level-quote-checkout">{formatAmountFa(result.amountPayableAtCheckout, result.currencyCode)}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>وصول هنگام تحویل (تشخیصی)</dt>
              <dd data-testid="shipping-one-level-quote-cod">{formatAmountFa(result.amountCollectOnDelivery, result.currencyCode)}</dd>
            </div>
            <div className="flex justify-between gap-2">
              <dt>طرف پرداخت کرایه</dt>
              <dd data-testid="shipping-one-level-quote-payer">{payerModeLabelFa(result.freightPayerMode)}</dd>
            </div>
          </dl>

          <p className="text-[11px] text-muted" data-testid="shipping-one-level-quote-amount-boundary">
            {ONE_LEVEL_QUOTE_AMOUNT_BOUNDARY_FA}
          </p>

          <div className="space-y-1 text-[11px] text-muted">
            <p data-testid="shipping-one-level-quote-payer-explanation">
              {result.freightPayerMode === "CollectOnDelivery" ? COLLECT_ON_DELIVERY_EXPLANATION_FA : PREPAID_EXPLANATION_FA}
            </p>
            <p>{PACKAGING_ALWAYS_CHECKOUT_FA}</p>
            <p className="font-semibold" data-testid="shipping-one-level-quote-payer-boundary">
              {PAYER_SPLIT_IS_QUOTE_FACT_FA}
            </p>
          </div>

          <div className="space-y-1 rounded-ds bg-secondary p-2 text-[11px] text-secondary-foreground">
            <p data-testid="shipping-one-level-quote-schedule">
              وضعیت بازهٔ حامل: {scheduleSummaryFa(result.schedule)}
            </p>
            <p data-testid="shipping-one-level-quote-schedule-reason">
              کد دلیل بازه: {quoteReasonFa(result.schedule?.reasonCode)}
            </p>
            <p data-testid="shipping-one-level-quote-availability">
              {result.schedule ? availabilityLabelFa(result.schedule.availability) : NOT_DECLARED_FA}
            </p>
            <p className="font-semibold" data-testid="shipping-one-level-quote-capacity">
              {ONE_LEVEL_QUOTE_CAPACITY_FA} — {CAPACITY_CONFIG_ONLY_QUOTE_FA}
            </p>
            {result.schedule?.availability === "NotEvaluated" && (
              <p className="font-semibold" data-testid="shipping-one-level-quote-not-evaluated">
                {NOT_EVALUATED_FA} — در این حالت «قابل رزرو» ادعا نمی‌شود.
              </p>
            )}
          </div>

          <div className="space-y-1 text-[11px]">
            <p data-testid="shipping-one-level-quote-condition">پلهٔ اندازهٔ منطبق: {conditionMatchFa(result.matchedCondition)}</p>
            <p data-testid="shipping-one-level-quote-eligible">
              واجد شرایط عملیاتی: {isQuotedResult(result) && (result.schedule?.isConstraintSatisfied ?? false) ? "بله" : "خیر"}
            </p>
            <p data-testid="shipping-one-level-quote-freight-reason">
              کد دلیل کرایه: {result.carrierFreight ? quoteReasonFa(result.carrierFreight.reasonCode) : NOT_DECLARED_FA}
            </p>
            <p data-testid="shipping-one-level-quote-packaging-reason">
              کد دلیل بسته‌بندی: {result.packaging ? quoteReasonFa(result.packaging.reasonCode) : NOT_DECLARED_FA}
            </p>
            <p data-testid="shipping-one-level-quote-scope-echo">
              دامنهٔ ارزیابی‌شده: {scopeSummary({ level: "", ...buildScopeWrite(selection) })}
            </p>
          </div>

          <p className="text-[10px] text-muted" data-testid="shipping-one-level-quote-diagnostic-note">
            {QUOTE_IS_DIAGNOSTIC_FA}
          </p>
        </section>
      )}

      <p className="text-[10px] text-muted">{DEFAULT_QUOTE_CURRENCY} · {NOT_DECLARED_FA}</p>
    </div>
  );
}
