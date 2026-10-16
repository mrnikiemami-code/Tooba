"use client";

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-086 — tab 2 «تعرفه و پوشش» of the one-level shipping shell.
 *
 * Scope rules this component is built to honor:
 *
 * - **One selected one-level method is the whole context.** The tab never renders a second-level
 *   option picker and never renders an editable parent-service grouping. The only thing shown about
 *   the method is its own code plus the server-resolved legacy binding, which is required to call the
 *   existing Shipping-owned rate/coverage APIs.
 * - **The legacy binding comes only from the server.** `method_code` is never split in the browser
 *   into `serviceCode`/`optionCode`; the resolver endpoint is the single authority. When the resolver
 *   cannot produce a binding (gate off, catalog unmigrated, unknown method, bad response) the tab
 *   refuses to act instead of guessing.
 * - **The selection is immutable while an operation is pending.** Switching the selected method
 *   bumps a generation counter, so an in-flight read/write of the previous method can never land on
 *   the new one.
 * - **Only the rates and grants of the resolved binding are shown.** Rates are matched on
 *   `(serviceCode, optionCode)` exactly; grants are matched on `serviceCode`.
 * - The tab does not delete or replace the legacy dialogs. Those remain the OFF-gate route.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  createAdminShippingCoverageGrant,
  createAdminShippingRate,
  loadAdminShippingRates,
  setAdminShippingCoverageGrantActive,
  setAdminShippingRateActive,
  updateAdminShippingRate,
  type ShippingRatesAndCoverageView,
  type ShippingRatePolicyAdminItem,
} from "./shipping-rates-coverage-api.ts";
import {
  buildScopeWrite,
  coverageStateFa,
  EMPTY_DESTINATION_SELECTION,
  formatCharge,
  formatFreeAboveThreshold,
  rateFormFromPolicy,
  rateFormToPayload,
  scopeSummary,
  summarizeRatesCoverage,
  validateDestinationSelection,
  validateShippingRateForm,
  type DestinationSelection,
  type ShippingRateFormValues,
} from "./shipping-rates-coverage-model.ts";
import {
  conditionFormFromPolicy,
  conditionFormToWrite,
  conditionSummaryFa,
  describeConditionFa,
  EMPTY_CONDITION_FORM,
  isConditioned,
  tierErrorFa,
  validateConditionForm,
  type ShippingRateConditionForm,
} from "./shipping-rate-tier-model.ts";
import { ShippingDestinationPicker } from "./shipping-destination-picker.tsx";
import { ConditionEditor } from "./shipping-rate-condition-editor.tsx";
import { adminRequestHeaders } from "../../../lib/admin/admin-result.ts";
import {
  bindingContextFa,
  bindingUnavailableFa,
  createBindingGenerationGuard,
  mapHostBinding,
  oneLevelBindingPath,
  resolveRatesState,
  scopeRatesToBinding,
  viewMatchesBindingService,
  type OneLevelBindingState,
} from "./shipping-one-level-rates-model.ts";

type Props = {
  /** کد روش یک‌سطحی انتخاب‌شده در زبانهٔ «سرویس‌ها». تهی یعنی هیچ روشی انتخاب نشده است. */
  methodCode: string;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /**
   * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146: پیوند حل‌شده‌ای که از **درخت واقعی سرور**
   * (`/v1/admin/shipping-methods`) می‌آید؛ یعنی `serviceCode` و `optionCode` فیلدهای جدا و
   * authoritative هستند و هیچ تقسیم متنیِ کد روش در مرورگر رخ نمی‌دهد. وقتی این مقدار داده شود،
   * زبانه همان پیوند را مصرف می‌کند و resolver غایب را صدا نمی‌زند، پس نبود ۰۸۶ مانع کارکرد نیست.
   */
  bindingOverride?: { serviceCode: string; optionCode: string | null; isActive: boolean } | null;
};

export function ShippingOneLevelRatesTab({ methodCode, onNotice, bindingOverride = null }: Props) {
  const [bindingLoading, setBindingLoading] = useState(false);
  const [binding, setBinding] = useState<OneLevelBindingState | null>(null);
  const [ratesState, setRatesState] = useState<"ok" | "denied" | "error" | null>(null);
  const [view, setView] = useState<ShippingRatesAndCoverageView | null>(null);
  const [busy, setBusy] = useState(false);
  // بازخورد ذخیره/خطا داخل خود زبانه نمایش داده می‌شود تا صفحه نیازی به بالابرندهٔ وضعیت نداشته باشد.
  const [notice, setNotice] = useState<{ tone: "success" | "error"; text: string } | null>(null);
  const emit = useCallback(
    (next: { tone: "success" | "error"; text: string }) => {
      setNotice(next);
      onNotice(next);
    },
    [onNotice],
  );

  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ShippingRateFormValues>(rateFormFromPolicy(null));
  const [issues, setIssues] = useState<string[]>([]);
  const [editConditionForm, setEditConditionForm] = useState<ShippingRateConditionForm>(EMPTY_CONDITION_FORM);
  const [editConditionIssues, setEditConditionIssues] = useState<string[]>([]);

  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [scopeIssues, setScopeIssues] = useState<string[]>([]);
  const [createRateForm, setCreateRateForm] = useState<ShippingRateFormValues>(rateFormFromPolicy(null));
  const [createFormIssues, setCreateFormIssues] = useState<string[]>([]);
  const [createConditionForm, setCreateConditionForm] = useState<ShippingRateConditionForm>(EMPTY_CONDITION_FORM);
  const [createConditionIssues, setCreateConditionIssues] = useState<string[]>([]);

  // نسل انتخاب: هر تغییر روش، پاسخ‌های در راهِ روش قبلی را باطل می‌کند.
  const guard = useRef(createBindingGenerationGuard());
  const resolvedBinding = binding?.state === "resolved" ? binding.binding : null;

  const refresh = useCallback(async (serviceCode: string, isCurrent: () => boolean) => {
    setRatesState(null);
    const result = await loadAdminShippingRates(serviceCode);
    if (!isCurrent()) return;
    if (result.state !== "ok" || !result.data) {
      setRatesState(result.state === "denied" ? "denied" : "error");
      setView(null);
      return;
    }
    setView(result.data);
    setRatesState("ok");
  }, []);

  useEffect(() => {
    // بازنشانی کامل وضعیت ویرایش هنگام تغییر روش انتخاب‌شده.
    setEditingId(null);
    setIssues([]);
    setForm(rateFormFromPolicy(null));
    setEditConditionForm(EMPTY_CONDITION_FORM);
    setEditConditionIssues([]);
    setSelection(EMPTY_DESTINATION_SELECTION);
    setScopeIssues([]);
    setCreateRateForm(rateFormFromPolicy(null));
    setCreateFormIssues([]);
    setCreateConditionForm(EMPTY_CONDITION_FORM);
    setCreateConditionIssues([]);
    setView(null);
    setRatesState(null);
    setBinding(null);

    if (!methodCode) {
      setBindingLoading(false);
      return;
    }

    // پیوند authoritative از درخت واقعی سرور (بدون تقسیم متنی کد روش در مرورگر).
    if (bindingOverride) {
      const isCurrent = guard.current.begin();
      setBinding({
        state: "resolved",
        binding: {
          methodCode,
          serviceCode: bindingOverride.serviceCode,
          optionCode: bindingOverride.optionCode,
          isActive: bindingOverride.isActive,
        },
      });
      setBindingLoading(false);
      void refresh(bindingOverride.serviceCode, isCurrent);
      return;
    }

    const isCurrent = guard.current.begin();
    setBindingLoading(true);
    void (async () => {
      let next: OneLevelBindingState;
      try {
        const response = await fetch(oneLevelBindingPath(methodCode), {
          headers: adminRequestHeaders(),
          cache: "no-store",
        });
        const payload = await response.json().catch(() => null);
        next = response.ok ? mapHostBinding(payload) : { state: "untrusted" };
      } catch {
        next = { state: "untrusted" };
      }
      if (!isCurrent()) return;
      setBinding(next);
      setBindingLoading(false);
      if (next.state === "resolved") {
        void refresh(next.binding.serviceCode, isCurrent);
      }
    })();
  }, [methodCode, refresh, bindingOverride?.serviceCode, bindingOverride?.optionCode, bindingOverride?.isActive]);

  const state = resolveRatesState({
    hasMethod: methodCode.length > 0,
    bindingLoading,
    bindingState: binding,
    ratesLoading: false,
    ratesState,
  });

  const scoped = useMemo(
    () => (resolvedBinding ? scopeRatesToBinding(view, resolvedBinding) : { rates: [], grants: [] }),
    [resolvedBinding, view],
  );
  const summary = useMemo(() => summarizeRatesCoverage(view), [view]);

  async function submitCreateRate() {
    if (!resolvedBinding) return;
    const scopeProblems = validateDestinationSelection(selection);
    const formProblems = validateShippingRateForm(createRateForm).map((issue) => issue.messageFa);
    const conditionProblems = validateConditionForm(createConditionForm);
    if (scopeProblems.length > 0 || formProblems.length > 0 || conditionProblems.length > 0) {
      setScopeIssues(scopeProblems);
      setCreateFormIssues(formProblems);
      setCreateConditionIssues(conditionProblems);
      return;
    }
    setScopeIssues([]);
    setCreateFormIssues([]);
    setCreateConditionIssues([]);
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await createAdminShippingRate(resolvedBinding.serviceCode, {
      scope: buildScopeWrite(selection),
      // پیوند گزینه فقط از پاسخ سرور می‌آید؛ هرگز از کد روش ساخته نمی‌شود.
      optionCode: resolvedBinding.optionCode,
      ...rateFormToPayload(createRateForm),
      condition: conditionFormToWrite(createConditionForm),
    });
    if (!isCurrent()) return;
    setBusy(false);
    if (result.state !== "ok") {
      emit({ tone: "error", text: errorFa(result.message, "ایجاد نرخ ناموفق بود.") });
      return;
    }
    emit({ tone: "success", text: `نرخ «${scopeSummary(result.data!)} · ${conditionSummaryFa(result.data!.condition)}» ایجاد شد.` });
    setCreateRateForm(rateFormFromPolicy(null));
    setCreateConditionForm(EMPTY_CONDITION_FORM);
    await refresh(resolvedBinding.serviceCode, isCurrent);
  }

  async function submitCreateGrant() {
    if (!resolvedBinding) return;
    const scopeProblems = validateDestinationSelection(selection);
    if (scopeProblems.length > 0) {
      setScopeIssues(scopeProblems);
      setCreateFormIssues([]);
      return;
    }
    setScopeIssues([]);
    setCreateFormIssues([]);
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await createAdminShippingCoverageGrant(resolvedBinding.serviceCode, {
      scope: buildScopeWrite(selection),
    });
    if (!isCurrent()) return;
    setBusy(false);
    if (result.state !== "ok") {
      emit({ tone: "error", text: errorFa(result.message, "ایجاد مجوز پوشش ناموفق بود.") });
      return;
    }
    emit({ tone: "success", text: `مجوز پوشش «${scopeSummary(result.data!)}» ایجاد شد.` });
    await refresh(resolvedBinding.serviceCode, isCurrent);
  }

  async function submitEdit(policy: ShippingRatePolicyAdminItem) {
    if (!resolvedBinding) return;
    const found = validateShippingRateForm(form);
    const conditionProblems = validateConditionForm(editConditionForm);
    if (found.length > 0 || conditionProblems.length > 0) {
      setIssues(found.map((issue) => issue.messageFa));
      setEditConditionIssues(conditionProblems);
      return;
    }
    setIssues([]);
    setEditConditionIssues([]);
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await updateAdminShippingRate(resolvedBinding.serviceCode, policy.ratePolicyId, {
      // دامنه و هویت گزینه تغییرناپذیرند: مقدار ذخیره‌شده بازگردانده می‌شود.
      scope: {
        countryId: policy.countryId,
        administrativeAreaId: policy.administrativeAreaId,
        cityId: policy.cityId,
        serviceAreaId: policy.serviceAreaId,
      },
      optionCode: policy.optionCode,
      ...rateFormToPayload(form),
      condition: conditionFormToWrite(editConditionForm),
    });
    if (!isCurrent()) return;
    setBusy(false);
    if (result.state !== "ok") {
      emit({ tone: "error", text: errorFa(result.message, "به‌روزرسانی نرخ ناموفق بود.") });
      return;
    }
    setEditingId(null);
    setEditConditionIssues([]);
    emit({ tone: "success", text: `نرخ «${scopeSummary(result.data!)}» ذخیره شد.` });
    await refresh(resolvedBinding.serviceCode, isCurrent);
  }

  async function toggleRate(policy: ShippingRatePolicyAdminItem) {
    if (!resolvedBinding) return;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await setAdminShippingRateActive(resolvedBinding.serviceCode, policy.ratePolicyId, !policy.isActive);
    if (!isCurrent()) return;
    setBusy(false);
    if (result.state !== "ok") {
      emit({ tone: "error", text: errorFa(result.message, "تغییر وضعیت نرخ ناموفق بود.") });
      return;
    }
    emit({ tone: "success", text: `نرخ «${scopeSummary(result.data!)}» ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.` });
    await refresh(resolvedBinding.serviceCode, isCurrent);
  }

  async function toggleGrant(grantId: string, isActive: boolean) {
    if (!resolvedBinding) return;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await setAdminShippingCoverageGrantActive(resolvedBinding.serviceCode, grantId, !isActive);
    if (!isCurrent()) return;
    setBusy(false);
    if (result.state !== "ok") {
      emit({ tone: "error", text: errorFa(result.message, "تغییر وضعیت پوشش ناموفق بود.") });
      return;
    }
    emit({ tone: "success", text: `مجوز پوشش ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.` });
    await refresh(resolvedBinding.serviceCode, isCurrent);
  }

  if (state === "no-method") {
    return (
      <div className="rounded-ds border border-dashed border-border p-6" data-testid="shipping-one-level-rates-no-method">
        <p className="font-medium">هیچ روشی انتخاب نشده است</p>
        <p className="mt-1 text-sm text-muted">
          برای دیدن تعرفه و پوشش، ابتدا در زبانهٔ «سرویس‌ها» یک روش یک‌سطحی را انتخاب کنید.
        </p>
      </div>
    );
  }

  if (state === "binding-loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-rates-binding-loading">
        در حال خواندن پیوند سرویس/گزینهٔ این روش…
      </p>
    );
  }

  if (state === "binding-unavailable") {
    return (
      <div className="rounded-ds border border-border p-6" data-testid="shipping-one-level-rates-binding-unavailable">
        <p className="font-medium">تعرفه و پوشش این روش در دسترس نیست</p>
        <p className="mt-1 text-sm text-muted">{bindingUnavailableFa(binding, methodCode)}</p>
        <p className="mt-2 text-xs text-muted">
          نگاشت کد روش به سرویس/گزینه فقط سمت سرور انجام می‌شود؛ این صفحه هیچ حدسی نمی‌زند.
        </p>
      </div>
    );
  }

  if (state === "loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-rates-loading">
        در حال بارگذاری نرخ‌ها و پوشش…
      </p>
    );
  }

  if (state === "denied") {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-rates-denied">
        دسترسی به تعرفه و پوشش این روش ندارید.
      </p>
    );
  }

  if (state === "error" || !resolvedBinding) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-rates-error">
        خطا در خواندن نرخ‌ها و پوشش این روش؛ دوباره تلاش کنید.
      </p>
    );
  }

  // پاسخِ خدمتِ دیگر هرگز زیر نام این روش رندر نمی‌شود.
  if (!viewMatchesBindingService(view, resolvedBinding)) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-rates-service-mismatch">
        پاسخ سرور به خدمت این روش تعلق ندارد؛ برای احتیاط هیچ نرخی نمایش داده نمی‌شود.
      </p>
    );
  }

  return (
    <div className="space-y-4" data-testid="shipping-one-level-rates-tab">
      <div className="rounded-ds border border-border p-3">
        <p className="text-sm font-medium" data-testid="shipping-one-level-rates-context">
          {bindingContextFa(resolvedBinding)}
        </p>
        <p className="mt-1 text-xs text-muted">
          هر روش یک ردیف مستقل است. این بخش روی همان روش انتخاب‌شده عمل می‌کند و هیچ سطح دوم پیکربندی نمی‌سازد.
        </p>
      </div>

      {notice && (
        <p
          className={notice.tone === "success" ? "text-sm text-emerald-700" : "text-sm text-red-600"}
          data-testid="shipping-one-level-rates-notice"
        >
          {notice.text}
        </p>
      )}

      <div className="rounded-ds bg-secondary p-3 text-xs text-secondary-foreground">
        {coverageStateFa(view)}
        <span className="mr-2 text-muted">
          (نرخ‌های فعال: {summary.activeRateCount.toLocaleString("fa-IR")} از {summary.rateCount.toLocaleString("fa-IR")})
        </span>
      </div>

      <section className="space-y-2">
        <h3 className="text-sm font-bold">سیاست‌های نرخ این روش</h3>
        {scoped.rates.length === 0 && (
          <p className="text-xs text-muted" data-testid="shipping-one-level-rates-empty">
            برای این روش نرخی ثبت نشده است. تا زمانی که نرخ ثبت نشود، این روش قیمت عملیاتی ندارد.
          </p>
        )}
        {scoped.rates.map((policy) => (
          <div
            key={policy.ratePolicyId}
            className="space-y-2 rounded-ds border border-border p-3"
            data-testid={`shipping-one-level-rate-row-${policy.ratePolicyId}`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
              <span>{scopeSummary(policy)}</span>
              <span className="font-bold">{formatCharge(policy.chargeAmount, policy.currencyCode)}</span>
            </div>
            <div className="flex flex-wrap gap-3 text-[11px] text-muted">
              <span>آماده‌سازی: {policy.preparationDays.toLocaleString("fa-IR")} روز</span>
              <span>حمل: {policy.leadDays.toLocaleString("fa-IR")} روز</span>
              <span>ارسال رایگان از: {formatFreeAboveThreshold(policy.freeAboveThreshold, policy.currencyCode)}</span>
              <span className={policy.isActive ? "font-bold text-emerald-700" : "font-bold text-muted"}>
                {policy.isActive ? "فعال" : "غیرفعال"}
              </span>
            </div>
            <p className="text-[11px] text-muted" data-testid={`shipping-one-level-rate-condition-${policy.ratePolicyId}`}>
              شرایط مرسوله: {describeConditionFa(policy.condition)}
              {isConditioned(policy.condition) && (
                <span className="mr-2 rounded bg-amber-100 px-1 text-[10px] font-bold text-amber-800">پله‌ای</span>
              )}
            </p>

            {editingId === policy.ratePolicyId && (
              <div className="grid grid-cols-2 gap-2">
                <label className="text-[11px] text-muted">
                  مبلغ هزینه
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    value={form.chargeAmount}
                    onChange={(e) => setForm({ ...form, chargeAmount: e.target.value })}
                    data-testid="shipping-one-level-rate-charge-input"
                  />
                </label>
                <label className="text-[11px] text-muted">
                  ارز
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    value={form.currencyCode}
                    onChange={(e) => setForm({ ...form, currencyCode: e.target.value })}
                    data-testid="shipping-one-level-rate-currency-input"
                  />
                </label>
                <label className="text-[11px] text-muted">
                  روزهای آماده‌سازی
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    value={form.preparationDays}
                    onChange={(e) => setForm({ ...form, preparationDays: e.target.value })}
                    data-testid="shipping-one-level-rate-preparation-input"
                  />
                </label>
                <label className="text-[11px] text-muted">
                  روزهای حمل
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    value={form.leadDays}
                    onChange={(e) => setForm({ ...form, leadDays: e.target.value })}
                    data-testid="shipping-one-level-rate-lead-input"
                  />
                </label>
                <label className="col-span-2 text-[11px] text-muted">
                  آستانهٔ ارسال رایگان (خالی = بدون آستانه)
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    value={form.freeAboveThreshold}
                    onChange={(e) => setForm({ ...form, freeAboveThreshold: e.target.value })}
                    data-testid="shipping-one-level-rate-threshold-input"
                  />
                </label>
                <div className="col-span-2">
                  <ConditionEditor
                    form={editConditionForm}
                    onChange={(next) => { setEditConditionForm(next); setEditConditionIssues([]); }}
                    disabled={busy}
                    testIdPrefix="shipping-one-level-rate-edit"
                  />
                </div>
              </div>
            )}

            {issues.length > 0 && editingId === policy.ratePolicyId && (
              <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-rate-issues">
                {issues.map((issue) => <li key={issue}>{issue}</li>)}
              </ul>
            )}
            {editConditionIssues.length > 0 && editingId === policy.ratePolicyId && (
              <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-rate-edit-condition-issues">
                {editConditionIssues.map((issue) => <li key={issue}>{issue}</li>)}
              </ul>
            )}

            <div className="flex gap-2">
              {editingId === policy.ratePolicyId ? (
                <>
                  <button
                    type="button"
                    className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
                    disabled={busy}
                    onClick={() => void submitEdit(policy)}
                    data-testid={`shipping-one-level-rate-save-${policy.ratePolicyId}`}
                  >
                    ذخیره
                  </button>
                  <button
                    type="button"
                    className="rounded-ds border border-border px-3 py-1 text-xs"
                    onClick={() => { setEditingId(null); setIssues([]); setEditConditionIssues([]); }}
                  >
                    انصراف
                  </button>
                </>
              ) : (
                <>
                  <button
                    type="button"
                    className="rounded-ds border border-border px-3 py-1 text-xs"
                    onClick={() => {
                      setEditingId(policy.ratePolicyId);
                      setForm(rateFormFromPolicy(policy));
                      setIssues([]);
                      setEditConditionForm(conditionFormFromPolicy(policy.condition));
                      setEditConditionIssues([]);
                    }}
                    data-testid={`shipping-one-level-rate-edit-${policy.ratePolicyId}`}
                  >
                    ویرایش
                  </button>
                  <button
                    type="button"
                    className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
                    disabled={busy}
                    onClick={() => void toggleRate(policy)}
                    data-testid={`shipping-one-level-rate-toggle-${policy.ratePolicyId}`}
                  >
                    {policy.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                  </button>
                </>
              )}
            </div>
          </div>
        ))}
      </section>

      <section className="space-y-2">
        <h3 className="text-sm font-bold">پوشش جغرافیایی این روش</h3>
        {scoped.grants.length === 0 && (
          <p className="text-xs text-muted" data-testid="shipping-one-level-coverage-empty">
            بدون پوشش: هیچ مجوز پوششی برای این روش ثبت نشده است (این وضعیت به‌معنای پوشش نامحدود نیست).
          </p>
        )}
        {scoped.grants.map((grant) => (
          <div
            key={grant.coverageGrantId}
            className="flex flex-wrap items-center justify-between gap-2 rounded-ds border border-border p-3 text-xs"
            data-testid={`shipping-one-level-coverage-row-${grant.coverageGrantId}`}
          >
            <span>{scopeSummary(grant)}</span>
            <span className={grant.isActive ? "font-bold text-emerald-700" : "font-bold text-muted"}>
              {grant.isActive ? "فعال" : "غیرفعال"}
            </span>
            <button
              type="button"
              className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
              disabled={busy}
              onClick={() => void toggleGrant(grant.coverageGrantId, grant.isActive)}
              data-testid={`shipping-one-level-coverage-toggle-${grant.coverageGrantId}`}
            >
              {grant.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
            </button>
          </div>
        ))}
      </section>

      <section className="space-y-3 rounded-ds border border-dashed border-border p-3">
        <h3 className="text-sm font-bold">افزودن نرخ / پوشش برای این روش</h3>
        <p className="text-[11px] text-muted">
          نرخ و پوشش روی همان خدمتی ثبت می‌شود که این روش به آن پیوند دارد؛ پیوند از سرور می‌آید و قابل‌تغییر نیست.
        </p>
        <ShippingDestinationPicker selection={selection} onChange={setSelection} disabled={busy} />

        {scopeIssues.length > 0 && (
          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-destination-issues">
            {scopeIssues.map((issue) => <li key={issue}>{issue}</li>)}
          </ul>
        )}

        <div className="grid grid-cols-2 gap-2">
          <label className="text-[11px] text-muted">
            مبلغ هزینه (نرخ جدید)
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              value={createRateForm.chargeAmount}
              onChange={(e) => setCreateRateForm({ ...createRateForm, chargeAmount: e.target.value })}
              data-testid="shipping-one-level-create-rate-charge"
            />
          </label>
          <label className="text-[11px] text-muted">
            ارز
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              value={createRateForm.currencyCode}
              onChange={(e) => setCreateRateForm({ ...createRateForm, currencyCode: e.target.value })}
              data-testid="shipping-one-level-create-rate-currency"
            />
          </label>
          <label className="text-[11px] text-muted">
            روزهای آماده‌سازی
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              value={createRateForm.preparationDays}
              onChange={(e) => setCreateRateForm({ ...createRateForm, preparationDays: e.target.value })}
              data-testid="shipping-one-level-create-rate-preparation"
            />
          </label>
          <label className="text-[11px] text-muted">
            روزهای حمل
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              value={createRateForm.leadDays}
              onChange={(e) => setCreateRateForm({ ...createRateForm, leadDays: e.target.value })}
              data-testid="shipping-one-level-create-rate-lead"
            />
          </label>
          <label className="col-span-2 text-[11px] text-muted">
            آستانهٔ ارسال رایگان (خالی = بدون آستانه)
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              value={createRateForm.freeAboveThreshold}
              onChange={(e) => setCreateRateForm({ ...createRateForm, freeAboveThreshold: e.target.value })}
              data-testid="shipping-one-level-create-rate-threshold"
            />
          </label>
        </div>

        <ConditionEditor
          form={createConditionForm}
          onChange={(next) => { setCreateConditionForm(next); setCreateConditionIssues([]); }}
          disabled={busy}
          testIdPrefix="shipping-one-level-create-rate"
        />

        {createConditionIssues.length > 0 && (
          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-create-rate-condition-issues">
            {createConditionIssues.map((issue) => <li key={issue}>{issue}</li>)}
          </ul>
        )}
        {createFormIssues.length > 0 && (
          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-create-rate-issues">
            {createFormIssues.map((issue) => <li key={issue}>{issue}</li>)}
          </ul>
        )}

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
            disabled={busy}
            onClick={() => void submitCreateRate()}
            data-testid="shipping-one-level-create-rate-submit"
          >
            افزودن نرخ
          </button>
          <button
            type="button"
            className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
            disabled={busy}
            onClick={() => void submitCreateGrant()}
            data-testid="shipping-one-level-create-grant-submit"
          >
            افزودن مجوز پوشش
          </button>
        </div>
      </section>
    </div>
  );
}

function errorFa(code: string | undefined, fallback: string): string {
  if (!code) return fallback;
  if (code === "admin.authorization.denied") return "دسترسی ندارید.";
  if (code === "shipping.service_scope.mismatch") return "این ردیف به خدمت دیگری تعلق دارد.";
  if (code === "shipping.rate_policy.not_found") return "سیاست نرخ پیدا نشد.";
  if (code === "shipping.coverage_grant.not_found") return "مجوز پوشش پیدا نشد.";
  if (code === "shipping.rate_policy.scope_duplicate") return "برای این دامنه از قبل نرخی ثبت شده است.";
  if (code === "shipping.coverage_grant.scope_duplicate") return "برای این دامنه از قبل مجوز پوشش ثبت شده است.";
  return tierErrorFa(code) ?? fallback;
}
