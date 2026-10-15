"use client";

/**
 * TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056 — per-service rates & coverage section, reachable from the
 * existing /admin/shipping-services rows. Reads the real Shipping backend view and can now CREATE,
 * update and toggle rates and coverage grants end to end.
 *
 * Destination scope is chosen exclusively from the authorized Geography admin read (stable GUID ids
 * with display labels), so no hand-typed or invented id can be submitted; the backend re-validates the
 * same ids through the Geography Contracts lookup before writing.
 */
import { useCallback, useEffect, useState } from "react";
import { Dialog } from "../../../design-system";
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
  coverageStateFa,
  formatCharge,
  formatFreeAboveThreshold,
  groupRatesByOption,
  rateFormFromPolicy,
  rateFormToPayload,
  rateOptionKey,
  rateOptionLabelFa,
  scopeSummary,
  SERVICE_LEVEL_RATE_FA,
  SERVICE_OPTION_UNKNOWN_CODE,
  summarizeRatesCoverage,
  validateRateOption,
  validateShippingRateForm,
  buildScopeWrite,
  validateDestinationSelection,
  EMPTY_DESTINATION_SELECTION,
  type DestinationSelection,
  type ShippingRateFormValues,
  type ShippingServiceOptionChoice,
} from "./shipping-rates-coverage-model.ts";
import { loadShippingMethodTree } from "../shipping-services-api.ts";
import { ShippingDestinationPicker } from "./shipping-destination-picker.tsx";

type Props = {
  open: boolean;
  serviceCode: string;
  serviceName: string;
  onClose: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
};

export function ShippingRatesCoverageDialog({ open, serviceCode, serviceName, onClose, onNotice }: Props) {
  const [state, setState] = useState<"loading" | "ok" | "denied" | "error">("loading");
  const [view, setView] = useState<ShippingRatesAndCoverageView | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ShippingRateFormValues>(rateFormFromPolicy(null));
  const [issues, setIssues] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  // انتخاب دامنهٔ مقصد برای ایجاد نرخ/پوشش جدید؛ فقط شناسه‌های برگشتی Geography.
  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [scopeIssues, setScopeIssues] = useState<string[]>([]);
  // ایرادهای شکلِ نرخِ در حال ایجاد، جدا از ایرادهای دامنه گزارش می‌شوند تا کاربر بفهمد کدام بخش
  // مشکل دارد (پیش از این هر دو زیر فهرست دامنه نمایش داده می‌شد).
  const [createFormIssues, setCreateFormIssues] = useState<string[]>([]);
  const [createRateForm, setCreateRateForm] = useState<ShippingRateFormValues>(rateFormFromPolicy(null));
  // گزینه‌های واقعی همین خدمت از درخت سرویس Fulfillment؛ هیچ گزینه‌ای اختراع نمی‌شود.
  const [options, setOptions] = useState<ShippingServiceOptionChoice[]>([]);
  // گزینهٔ نرخ در حال ایجاد: تهی یعنی سیاست سطح خدمت (رفتار فعلی تک‌گزینه‌ای‌ها دست‌نخورده می‌ماند).
  const [createOptionCode, setCreateOptionCode] = useState<string>("");
  const [createOptionIssues, setCreateOptionIssues] = useState<string[]>([]);

  const refresh = useCallback(async () => {
    if (!serviceCode) return;
    setState("loading");
    const result = await loadAdminShippingRates(serviceCode);
    if (result.state !== "ok" || !result.data) {
      setState(result.state === "denied" ? "denied" : "error");
      setMessage(result.message);
      setView(null);
      return;
    }
    setView(result.data);
    setState("ok");
    setMessage(undefined);
  }, [serviceCode]);

  // فهرست گزینه‌ها از همان درخت سرویس مجاز admin خوانده می‌شود؛ نبود نتیجه یعنی خدمت بدون گزینه.
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
      setEditingId(null);
      setIssues([]);
      setForm(rateFormFromPolicy(null));
      setSelection(EMPTY_DESTINATION_SELECTION);
      setScopeIssues([]);
      setCreateRateForm(rateFormFromPolicy(null));
      setCreateOptionCode("");
      setCreateOptionIssues([]);
      void refresh();
      void loadOptions();
    }
  }, [open, refresh, loadOptions]);

  /** ایجاد نرخ جدید روی مسیر واقعی؛ دامنه با شناسه‌های Geography و پس از اعتبارسنجی محلی. */
  async function submitCreateRate() {
    const scopeProblems = validateDestinationSelection(selection);
    const formProblems = validateShippingRateForm(createRateForm).map((issue) => issue.messageFa);
    const optionProblems = validateRateOption(createOptionCode || null, options);
    if (scopeProblems.length > 0 || formProblems.length > 0 || optionProblems.length > 0) {
      setScopeIssues(scopeProblems);
      setCreateFormIssues(formProblems);
      setCreateOptionIssues(optionProblems);
      return;
    }
    setScopeIssues([]);
    setCreateFormIssues([]);
    setCreateOptionIssues([]);
    setBusy(true);
    const result = await createAdminShippingRate(serviceCode, {
      scope: buildScopeWrite(selection),
      optionCode: createOptionCode || null,
      ...rateFormToPayload(createRateForm),
    });
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: errorFa(result.message, "ایجاد نرخ ناموفق بود.") });
      return;
    }
    onNotice({
      tone: "success",
      text: `نرخ «${rateOptionLabelFa(result.data!.optionCode, options)} · ${scopeSummary(result.data!)}» ایجاد شد.`,
    });
    setCreateRateForm(rateFormFromPolicy(null));
    await refresh();
  }

  /** ایجاد مجوز پوشش جدید روی همان دامنهٔ انتخاب‌شده. */
  async function submitCreateGrant() {
    const scopeProblems = validateDestinationSelection(selection);
    if (scopeProblems.length > 0) {
      setScopeIssues(scopeProblems);
      setCreateFormIssues([]);
      return;
    }
    setScopeIssues([]);
    setCreateFormIssues([]);
    setBusy(true);
    const result = await createAdminShippingCoverageGrant(serviceCode, {
      scope: buildScopeWrite(selection),
    });
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: errorFa(result.message, "ایجاد مجوز پوشش ناموفق بود.") });
      return;
    }
    onNotice({ tone: "success", text: `مجوز پوشش «${scopeSummary(result.data!)}» ایجاد شد.` });
    await refresh();
  }

  function startEdit(policy: ShippingRatePolicyAdminItem) {
    setEditingId(policy.ratePolicyId);
    setForm(rateFormFromPolicy(policy));
    setIssues([]);
  }

  async function submitEdit(policy: ShippingRatePolicyAdminItem) {
    const found = validateShippingRateForm(form);
    if (found.length > 0) {
      setIssues(found.map((issue) => issue.messageFa));
      return;
    }
    setIssues([]);
    setBusy(true);
    const payload = rateFormToPayload(form);
    const result = await updateAdminShippingRate(serviceCode, policy.ratePolicyId, {
      // The scope of an existing policy is immutable through this seam: the stored scope is echoed back.
      scope: {
        countryId: policy.countryId,
        administrativeAreaId: policy.administrativeAreaId,
        cityId: policy.cityId,
        serviceAreaId: policy.serviceAreaId,
      },
      // هویت گزینه نیز تغییرناپذیر است: همان مقدار ذخیره‌شده بازگردانده می‌شود تا نرخ هرگز
      // در سکوت به گزینهٔ دیگری منتقل نشود.
      optionCode: policy.optionCode,
      ...payload,
    });
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: errorFa(result.message, "به‌روزرسانی نرخ ناموفق بود.") });
      return;
    }
    setEditingId(null);
    onNotice({
      tone: "success",
      text: `نرخ «${rateOptionLabelFa(result.data!.optionCode, options)} · ${scopeSummary(result.data!)}» ذخیره شد.`,
    });
    await refresh();
  }

  async function toggleRate(policy: ShippingRatePolicyAdminItem) {
    setBusy(true);
    const result = await setAdminShippingRateActive(serviceCode, policy.ratePolicyId, !policy.isActive);
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: errorFa(result.message, "تغییر وضعیت نرخ ناموفق بود.") });
      return;
    }
    onNotice({
      tone: "success",
      text: `نرخ «${scopeSummary(result.data!)}» ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.`,
    });
    await refresh();
  }

  async function toggleGrant(grantId: string, isActive: boolean) {
    setBusy(true);
    const result = await setAdminShippingCoverageGrantActive(serviceCode, grantId, !isActive);
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: errorFa(result.message, "تغییر وضعیت پوشش ناموفق بود.") });
      return;
    }
    onNotice({
      tone: "success",
      text: `مجوز پوشش ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.`,
    });
    await refresh();
  }

  const summary = summarizeRatesCoverage(view);

  return (
    <Dialog open={open} onClose={onClose} title={`نرخ و پوشش — ${serviceName}`}>
      <div className="max-h-[70vh] space-y-4 overflow-y-auto" data-testid="shipping-rates-coverage-dialog">
        <p className="text-xs text-gray-600">
          کد خدمت: <span dir="ltr" className="font-mono">{serviceCode}</span>
        </p>

        <div className="rounded-lg bg-gray-50 p-3 text-xs text-gray-700">
          {coverageStateFa(view)}
          {state === "ok" && (
            <span className="mr-2 text-gray-500">
              (نرخ‌های فعال: {summary.activeRateCount.toLocaleString("fa-IR")} از {summary.rateCount.toLocaleString("fa-IR")})
            </span>
          )}
        </div>

        <p className="rounded-lg bg-gray-50 p-3 text-[11px] text-gray-600" data-testid="shipping-rates-scope-note">
          دامنهٔ مقصد فقط از فهرست مرجع Geography انتخاب می‌شود؛ شناسه‌ها پایدارند و شناسهٔ دستی یا ساختگی پذیرفته نمی‌شود.
        </p>

        {state === "denied" && (
          <p className="text-sm text-red-600" data-testid="shipping-rates-denied">دسترسی به نرخ و پوشش ندارید.</p>
        )}
        {state === "error" && (
          <p className="text-sm text-red-600" data-testid="shipping-rates-error">
            خطا در خواندن نرخ‌ها: {errorFa(message, "دوباره تلاش کنید.")}
          </p>
        )}

        {state === "ok" && (
          <>
            <section className="space-y-2">
              <h3 className="text-sm font-bold text-gray-800">سیاست‌های نرخ</h3>
              {options.length > 0 && (
                <p className="rounded-lg bg-gray-50 p-3 text-[11px] text-gray-600" data-testid="shipping-rate-service-level-note">
                  {SERVICE_LEVEL_RATE_FA}
                </p>
              )}
              {view && view.ratePolicies.length === 0 && (
                <p className="text-xs text-gray-500">هیچ سیاست نرخی ثبت نشده است.</p>
              )}
              {groupRatesByOption(view?.ratePolicies ?? [], options).map((group) => (
                <div key={rateOptionKey(group.optionCode)} className="space-y-2">
                  {options.length > 0 && (
                    <p
                      className="text-[11px] font-bold text-gray-700"
                      data-testid={`shipping-rate-option-group-${rateOptionKey(group.optionCode) || "service"}`}
                    >
                      گزینه: {rateOptionLabelFa(group.optionCode, options)}
                    </p>
                  )}
                  {group.rates.map((policy) => (
                    <div
                      key={policy.ratePolicyId}
                      className="space-y-2 rounded-lg border border-gray-200 p-3"
                      data-testid={`shipping-rate-row-${policy.ratePolicyId}`}
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-gray-700">
                        <span>{scopeSummary(policy)}</span>
                        <span className="font-bold">{formatCharge(policy.chargeAmount, policy.currencyCode)}</span>
                      </div>
                      <div className="flex flex-wrap gap-3 text-[11px] text-gray-600">
                        <span data-testid={`shipping-rate-option-${policy.ratePolicyId}`}>
                          گزینه: {rateOptionLabelFa(policy.optionCode, options)}
                        </span>
                        <span>آماده‌سازی: {policy.preparationDays.toLocaleString("fa-IR")} روز</span>
                        <span>حمل: {policy.leadDays.toLocaleString("fa-IR")} روز</span>
                        <span>ارسال رایگان از: {formatFreeAboveThreshold(policy.freeAboveThreshold, policy.currencyCode)}</span>
                        <span className={policy.isActive ? "font-bold text-emerald-700" : "font-bold text-gray-500"}>
                          {policy.isActive ? "فعال" : "غیرفعال"}
                        </span>
                      </div>

                  {editingId === policy.ratePolicyId ? (
                    <div className="grid grid-cols-2 gap-2">
                      <label className="text-[11px] text-gray-600">
                        مبلغ هزینه
                        <input
                          className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                          value={form.chargeAmount}
                          onChange={(e) => setForm({ ...form, chargeAmount: e.target.value })}
                          data-testid="shipping-rate-charge-input"
                        />
                      </label>
                      <label className="text-[11px] text-gray-600">
                        ارز
                        <input
                          className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                          value={form.currencyCode}
                          onChange={(e) => setForm({ ...form, currencyCode: e.target.value })}
                          data-testid="shipping-rate-currency-input"
                        />
                      </label>
                      <label className="text-[11px] text-gray-600">
                        روزهای آماده‌سازی
                        <input
                          className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                          value={form.preparationDays}
                          onChange={(e) => setForm({ ...form, preparationDays: e.target.value })}
                          data-testid="shipping-rate-preparation-input"
                        />
                      </label>
                      <label className="text-[11px] text-gray-600">
                        روزهای حمل
                        <input
                          className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                          value={form.leadDays}
                          onChange={(e) => setForm({ ...form, leadDays: e.target.value })}
                          data-testid="shipping-rate-lead-input"
                        />
                      </label>
                      <label className="col-span-2 text-[11px] text-gray-600">
                        آستانهٔ ارسال رایگان (خالی = بدون آستانه)
                        <input
                          className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                          value={form.freeAboveThreshold}
                          onChange={(e) => setForm({ ...form, freeAboveThreshold: e.target.value })}
                          data-testid="shipping-rate-threshold-input"
                        />
                      </label>
                    </div>
                  ) : null}

                  {issues.length > 0 && editingId === policy.ratePolicyId && (
                    <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-rate-issues">
                      {issues.map((issue) => <li key={issue}>{issue}</li>)}
                    </ul>
                  )}

                  <div className="flex gap-2">
                    {editingId === policy.ratePolicyId ? (
                      <>
                        <button
                          type="button"
                          className="rounded bg-blue-600 px-3 py-1 text-xs font-bold text-white disabled:opacity-50"
                          disabled={busy}
                          onClick={() => void submitEdit(policy)}
                          data-testid={`shipping-rate-save-${policy.ratePolicyId}`}
                        >
                          ذخیره
                        </button>
                        <button
                          type="button"
                          className="rounded border border-gray-300 px-3 py-1 text-xs"
                          onClick={() => { setEditingId(null); setIssues([]); }}
                        >
                          انصراف
                        </button>
                      </>
                    ) : (
                      <>
                        <button
                          type="button"
                          className="rounded border border-gray-300 px-3 py-1 text-xs"
                          onClick={() => startEdit(policy)}
                          data-testid={`shipping-rate-edit-${policy.ratePolicyId}`}
                        >
                          ویرایش
                        </button>
                        <button
                          type="button"
                          className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                          disabled={busy}
                          onClick={() => void toggleRate(policy)}
                          data-testid={`shipping-rate-toggle-${policy.ratePolicyId}`}
                        >
                          {policy.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                        </button>
                      </>
                    )}
                  </div>
                    </div>
                  ))}
                </div>
              ))}
            </section>

            <section className="space-y-2">
              <h3 className="text-sm font-bold text-gray-800">پوشش جغرافیایی</h3>
              {view && view.coverageGrants.length === 0 && (
                <p className="text-xs text-gray-500" data-testid="shipping-coverage-empty">
                  بدون پوشش: هیچ مجوز پوششی ثبت نشده است (این وضعیت به‌معنای پوشش نامحدود نیست).
                </p>
              )}
              {view?.coverageGrants.map((grant) => (
                <div
                  key={grant.coverageGrantId}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-gray-200 p-3 text-xs text-gray-700"
                  data-testid={`shipping-coverage-row-${grant.coverageGrantId}`}
                >
                  <span>{scopeSummary(grant)}</span>
                  <span className={grant.isActive ? "font-bold text-emerald-700" : "font-bold text-gray-500"}>
                    {grant.isActive ? "فعال" : "غیرفعال"}
                  </span>
                  <button
                    type="button"
                    className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                    disabled={busy}
                    onClick={() => void toggleGrant(grant.coverageGrantId, grant.isActive)}
                    data-testid={`shipping-coverage-toggle-${grant.coverageGrantId}`}
                  >
                    {grant.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                  </button>
                </div>
              ))}
            </section>

            <section className="space-y-3 rounded-lg border border-dashed border-gray-300 p-3">
              <h3 className="text-sm font-bold text-gray-800">افزودن نرخ / پوشش</h3>
              <ShippingDestinationPicker selection={selection} onChange={setSelection} disabled={busy} />

              {scopeIssues.length > 0 && (
                <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-destination-issues">
                  {scopeIssues.map((issue) => <li key={issue}>{issue}</li>)}
                </ul>
              )}

              {options.length > 0 && (
                <label className="block text-[11px] text-gray-600">
                  گزینهٔ خدمت (برای خدمات چندگزینه‌ای)
                  <select
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createOptionCode}
                    onChange={(e) => { setCreateOptionCode(e.target.value); setCreateOptionIssues([]); }}
                    disabled={busy}
                    data-testid="shipping-create-rate-option"
                  >
                    <option value="">سطح خدمت (پیش‌فرض خدمت)</option>
                    {options.map((option) => (
                      <option key={option.code} value={option.code}>{option.labelFa}</option>
                    ))}
                  </select>
                  <span className="mt-1 block text-[10px] text-gray-500">{SERVICE_LEVEL_RATE_FA}</span>
                </label>
              )}

              {createOptionIssues.length > 0 && (
                <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-create-rate-option-issues">
                  {createOptionIssues.map((issue) => <li key={issue}>{issue}</li>)}
                </ul>
              )}

              <div className="grid grid-cols-2 gap-2">
                <label className="text-[11px] text-gray-600">
                  مبلغ هزینه (نرخ جدید)
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createRateForm.chargeAmount}
                    onChange={(e) => setCreateRateForm({ ...createRateForm, chargeAmount: e.target.value })}
                    data-testid="shipping-create-rate-charge"
                  />
                </label>
                <label className="text-[11px] text-gray-600">
                  ارز
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createRateForm.currencyCode}
                    onChange={(e) => setCreateRateForm({ ...createRateForm, currencyCode: e.target.value })}
                    data-testid="shipping-create-rate-currency"
                  />
                </label>
                <label className="text-[11px] text-gray-600">
                  روزهای آماده‌سازی
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createRateForm.preparationDays}
                    onChange={(e) => setCreateRateForm({ ...createRateForm, preparationDays: e.target.value })}
                    data-testid="shipping-create-rate-preparation"
                  />
                </label>
                <label className="text-[11px] text-gray-600">
                  روزهای حمل
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createRateForm.leadDays}
                    onChange={(e) => setCreateRateForm({ ...createRateForm, leadDays: e.target.value })}
                    data-testid="shipping-create-rate-lead"
                  />
                </label>
                <label className="col-span-2 text-[11px] text-gray-600">
                  آستانهٔ ارسال رایگان (خالی = بدون آستانه)
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createRateForm.freeAboveThreshold}
                    onChange={(e) => setCreateRateForm({ ...createRateForm, freeAboveThreshold: e.target.value })}
                    data-testid="shipping-create-rate-threshold"
                  />
                </label>
              </div>

              {createFormIssues.length > 0 && (
                <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-create-rate-issues">
                  {createFormIssues.map((issue) => <li key={issue}>{issue}</li>)}
                </ul>
              )}

              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  className="rounded bg-blue-600 px-3 py-1 text-xs font-bold text-white disabled:opacity-50"
                  disabled={busy}
                  onClick={() => void submitCreateRate()}
                  data-testid="shipping-create-rate-submit"
                >
                  افزودن نرخ
                </button>
                <button
                  type="button"
                  className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                  disabled={busy}
                  onClick={() => void submitCreateGrant()}
                  data-testid="shipping-create-grant-submit"
                >
                  افزودن مجوز پوشش
                </button>
              </div>
            </section>
          </>
        )}
      </div>

      <div className="mt-4 flex justify-end">
        <button type="button" className="rounded border border-gray-300 px-3 py-1 text-xs" onClick={onClose}>
          بستن
        </button>
      </div>
    </Dialog>
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
  if (code === SERVICE_OPTION_UNKNOWN_CODE) return "گزینهٔ انتخابی برای این خدمت شناخته‌شده نیست.";
  return fallback;
}
