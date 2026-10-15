"use client";

/**
 * TB-SHIPPING-FULL-OPERATIONS-077 — «هزینه بسته‌بندی و نحوه پرداخت کرایه»: the per-service packaging
 * fee and freight payer-mode editor of one Shipping service, reachable from the existing
 * `/admin/shipping-services` service row next to the rate/coverage and schedule controls.
 *
 * It is one focused dialog, not a new route and not a combined form with rates or schedules. Every
 * control binds the real 075 authorized admin routes; nothing is fabricated, patched in memory or
 * persisted locally, and each write is followed by a real re-read.
 *
 * Money here is a commercial fact of the quotation, never a transaction: this screen neither moves
 * money nor wires collect-on-delivery settlement into Checkout or Payment.
 */
import { useCallback, useEffect, useState } from "react";
import { Dialog } from "../../../design-system";
import {
  createAdminShippingPackagingPolicy,
  loadAdminShippingPackagingPolicies,
  setAdminShippingPackagingPolicyActive,
  updateAdminShippingPackagingPolicy,
  type ShippingPackagingPolicyAdminItem,
  type ShippingPackagingPoliciesView,
} from "./shipping-packaging-api.ts";
import {
  FREE_SHIPPING_DOES_NOT_WAIVE_PACKAGING_FA,
  NO_PACKAGING_POLICY_FA,
  PACKAGING_SCOPE_NOTE_FA,
  PAYER_MODE_IS_QUOTE_FACT_FA,
  SERVICE_LEVEL_PACKAGING_FA,
  SERVICE_LEVEL_PACKAGING_VALUE,
  emptyPackagingForm,
  formatPackagingFee,
  groupPackagingByOption,
  mapPackagingErrorFa,
  packagingActiveLabelFa,
  packagingFormFromPolicy,
  packagingFormToPayload,
  packagingOptionKey,
  packagingOptionLabelFa,
  packagingStateFa,
  payerModeLabelFa,
  validatePackagingForm,
  validatePackagingOption,
  type ShippingPackagingFormValues,
} from "./shipping-packaging-model.ts";
import {
  buildScopeWrite,
  EMPTY_DESTINATION_SELECTION,
  scopeSummary,
  validateDestinationSelection,
  type DestinationSelection,
  type ShippingServiceOptionChoice,
} from "./shipping-rates-coverage-model.ts";
import { ShippingDestinationPicker } from "./shipping-destination-picker.tsx";
import { loadShippingMethodTree } from "../shipping-services-api.ts";

type Props = {
  open: boolean;
  serviceCode: string;
  serviceName: string;
  onClose: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
};

const PAYER_MODES = [
  { value: "Prepaid" as const, labelFa: "پیش‌کرایه" },
  { value: "CollectOnDelivery" as const, labelFa: "پس‌کرایه" },
];

export function ShippingPackagingDialog({ open, serviceCode, serviceName, onClose, onNotice }: Props) {
  const [state, setState] = useState<"loading" | "ok" | "denied" | "error">("loading");
  const [view, setView] = useState<ShippingPackagingPoliciesView | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [options, setOptions] = useState<ShippingServiceOptionChoice[]>([]);
  const [busy, setBusy] = useState(false);

  // فرم ایجاد؛ دامنهٔ مقصد فقط از Geography و گزینه فقط از درخت واقعی خدمت.
  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [createOptionCode, setCreateOptionCode] = useState<string>(SERVICE_LEVEL_PACKAGING_VALUE);
  const [createForm, setCreateForm] = useState<ShippingPackagingFormValues>(emptyPackagingForm());
  const [createIssues, setCreateIssues] = useState<string[]>([]);

  // فرم ویرایش؛ جدا از فرم ایجاد تا ویرایش یک ردیف هرگز مقدار ردیف دیگری را بازتاب ندهد.
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<ShippingPackagingFormValues>(emptyPackagingForm());
  const [editIssues, setEditIssues] = useState<string[]>([]);

  const refresh = useCallback(async () => {
    if (!serviceCode) return;
    setState("loading");
    const result = await loadAdminShippingPackagingPolicies(serviceCode);
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
      setEditIssues([]);
      setSelection(EMPTY_DESTINATION_SELECTION);
      setCreateOptionCode(SERVICE_LEVEL_PACKAGING_VALUE);
      setCreateForm(emptyPackagingForm());
      setCreateIssues([]);
      void refresh();
      void loadOptions();
    }
  }, [open, refresh, loadOptions]);

  async function submitCreate() {
    const scopeProblems = validateDestinationSelection(selection);
    const optionProblems = validatePackagingOption(
      createOptionCode === SERVICE_LEVEL_PACKAGING_VALUE ? null : createOptionCode,
      options,
    );
    const formProblems = validatePackagingForm(createForm).map((issue) => issue.messageFa);
    if (scopeProblems.length > 0 || optionProblems.length > 0 || formProblems.length > 0) {
      setCreateIssues([...scopeProblems, ...optionProblems, ...formProblems]);
      return;
    }

    setCreateIssues([]);
    setBusy(true);
    const result = await createAdminShippingPackagingPolicy(serviceCode, {
      scope: buildScopeWrite(selection),
      optionCode: createOptionCode === SERVICE_LEVEL_PACKAGING_VALUE ? null : createOptionCode,
      ...packagingFormToPayload(createForm),
    });
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: mapPackagingErrorFa(result.message, "ایجاد سیاست بسته‌بندی ناموفق بود.") });
      return;
    }
    onNotice({
      tone: "success",
      text: `سیاست بسته‌بندی «${packagingOptionLabelFa(result.data!.optionCode, options)} · ${scopeSummary(result.data!)} · ${formatPackagingFee(result.data!.feeAmount, result.data!.currencyCode)} · ${payerModeLabelFa(result.data!.freightPayerMode)}» ایجاد شد.`,
    });
    setCreateForm(emptyPackagingForm(createForm.currencyCode));
    await refresh();
  }

  function startEdit(policy: ShippingPackagingPolicyAdminItem) {
    setEditingId(policy.packagingPolicyId);
    setEditForm(packagingFormFromPolicy(policy));
    setEditIssues([]);
  }

  async function submitEdit(policy: ShippingPackagingPolicyAdminItem) {
    const problems = validatePackagingForm(editForm).map((issue) => issue.messageFa);
    if (problems.length > 0) {
      setEditIssues(problems);
      return;
    }

    setEditIssues([]);
    setBusy(true);
    const result = await updateAdminShippingPackagingPolicy(serviceCode, policy.packagingPolicyId, {
      // دامنه و هویت گزینه از ردیف ذخیره‌شده بازگردانده می‌شوند؛ ویرایش مبلغ هرگز سیاست را
      // بی‌صدا به گزینه یا دامنهٔ دیگری منتقل نمی‌کند.
      scope: {
        countryId: policy.countryId,
        administrativeAreaId: policy.administrativeAreaId,
        cityId: policy.cityId,
        serviceAreaId: policy.serviceAreaId,
      },
      optionCode: policy.optionCode,
      ...packagingFormToPayload(editForm),
    });
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: mapPackagingErrorFa(result.message, "به‌روزرسانی سیاست بسته‌بندی ناموفق بود.") });
      return;
    }
    setEditingId(null);
    setEditIssues([]);
    onNotice({
      tone: "success",
      text: `سیاست بسته‌بندی «${scopeSummary(result.data!)} · ${formatPackagingFee(result.data!.feeAmount, result.data!.currencyCode)} · ${payerModeLabelFa(result.data!.freightPayerMode)}» ذخیره شد.`,
    });
    await refresh();
  }

  async function toggle(policy: ShippingPackagingPolicyAdminItem) {
    setBusy(true);
    const result = await setAdminShippingPackagingPolicyActive(
      serviceCode,
      policy.packagingPolicyId,
      !policy.isActive,
    );
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: mapPackagingErrorFa(result.message, "تغییر وضعیت سیاست بسته‌بندی ناموفق بود.") });
      await refresh();
      return;
    }
    onNotice({
      tone: "success",
      text: `سیاست بسته‌بندی «${scopeSummary(result.data!)}» ${packagingActiveLabelFa(result.data!.isActive)} شد.`,
    });
    await refresh();
  }

  const policies = view?.packagingPolicies ?? [];
  const optionIssues = validatePackagingOption(
    createOptionCode === SERVICE_LEVEL_PACKAGING_VALUE ? null : createOptionCode,
    options,
  );

  return (
    <Dialog open={open} onClose={onClose} title={`هزینه بسته‌بندی و نحوه پرداخت کرایه — ${serviceName}`} size="xl">
      <div className="max-h-[78vh] space-y-4 overflow-y-auto" data-testid="shipping-packaging-dialog">
        <p className="text-xs text-gray-600">
          کد خدمت: <span dir="ltr" className="font-mono">{serviceCode}</span>
        </p>

        <p className="rounded-lg bg-gray-50 p-3 text-[11px] text-gray-600" data-testid="shipping-packaging-scope-note">
          {PACKAGING_SCOPE_NOTE_FA}
        </p>

        <p className="rounded-lg bg-amber-50 p-3 text-[11px] font-semibold text-amber-900" data-testid="shipping-packaging-payer-note">
          {PAYER_MODE_IS_QUOTE_FACT_FA}
        </p>

        <p className="rounded-lg bg-gray-50 p-3 text-[11px] text-gray-600" data-testid="shipping-packaging-free-shipping-note">
          {FREE_SHIPPING_DOES_NOT_WAIVE_PACKAGING_FA}
        </p>

        {state === "denied" && (
          <p className="text-sm text-red-600" data-testid="shipping-packaging-denied">
            دسترسی به هزینهٔ بسته‌بندی ندارید؛ این بخش به مجوز مشاهده و مدیریت Fulfillment نیاز دارد.
          </p>
        )}
        {state === "error" && (
          <p className="text-sm text-red-600" data-testid="shipping-packaging-error">
            خطا در خواندن سیاست‌های بسته‌بندی: {mapPackagingErrorFa(message, "دوباره تلاش کنید.")}
          </p>
        )}

        {state === "ok" && (
          <>
            <section className="space-y-2 rounded-lg border border-gray-200 p-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-sm font-bold text-gray-800">سیاست‌های بسته‌بندی ثبت‌شده</h3>
                <span className="text-[11px] text-gray-600" data-testid="shipping-packaging-summary">
                  {packagingStateFa(policies)}
                </span>
              </div>
              {policies.length === 0 ? (
                <p className="text-xs text-gray-500" data-testid="shipping-packaging-empty">
                  {NO_PACKAGING_POLICY_FA}
                </p>
              ) : (
                groupPackagingByOption(policies, options).map((group) => (
                  <div key={packagingOptionKey(group.optionCode)} className="space-y-2">
                    {options.length > 0 && (
                      <p
                        className="text-[11px] font-bold text-gray-700"
                        data-testid={`shipping-packaging-option-group-${packagingOptionKey(group.optionCode) || "service"}`}
                      >
                        گزینه: {packagingOptionLabelFa(group.optionCode, options)}
                      </p>
                    )}
                    {group.policies.map((policy) => (
                      <div
                        key={policy.packagingPolicyId}
                        className="space-y-2 rounded-lg border border-gray-200 p-3"
                        data-testid={`shipping-packaging-row-${policy.packagingPolicyId}`}
                      >
                        <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-gray-700">
                          <span>{scopeSummary(policy)}</span>
                          <span className="font-bold">
                            {formatPackagingFee(policy.feeAmount, policy.currencyCode)}
                          </span>
                        </div>
                        <div className="flex flex-wrap gap-3 text-[11px] text-gray-600">
                          <span data-testid={`shipping-packaging-option-${policy.packagingPolicyId}`}>
                            گزینه: {packagingOptionLabelFa(policy.optionCode, options)}
                          </span>
                          <span data-testid={`shipping-packaging-payer-${policy.packagingPolicyId}`}>
                            کرایه: {payerModeLabelFa(policy.freightPayerMode)}
                          </span>
                          <span
                            className={policy.isActive ? "font-bold text-emerald-700" : "font-bold text-gray-500"}
                            data-testid={`shipping-packaging-active-${policy.packagingPolicyId}`}
                          >
                            {packagingActiveLabelFa(policy.isActive)}
                          </span>
                        </div>

                        {editingId === policy.packagingPolicyId ? (
                          <div className="grid gap-2 sm:grid-cols-3">
                            <label className="text-[11px] text-gray-600">
                              هزینهٔ بسته‌بندی
                              <input
                                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                                dir="ltr"
                                inputMode="decimal"
                                value={editForm.feeAmount}
                                onChange={(e) => { setEditForm({ ...editForm, feeAmount: e.target.value }); setEditIssues([]); }}
                                data-testid={`shipping-packaging-fee-${policy.packagingPolicyId}`}
                              />
                            </label>
                            <label className="text-[11px] text-gray-600">
                              ارز
                              <input
                                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                                dir="ltr"
                                value={editForm.currencyCode}
                                onChange={(e) => { setEditForm({ ...editForm, currencyCode: e.target.value }); setEditIssues([]); }}
                                data-testid={`shipping-packaging-currency-${policy.packagingPolicyId}`}
                              />
                            </label>
                            <label className="text-[11px] text-gray-600">
                              نحوهٔ پرداخت کرایه
                              <select
                                className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                                value={editForm.payerMode}
                                onChange={(e) => { setEditForm({ ...editForm, payerMode: e.target.value as ShippingPackagingFormValues["payerMode"] }); setEditIssues([]); }}
                                data-testid={`shipping-packaging-payer-select-${policy.packagingPolicyId}`}
                              >
                                {PAYER_MODES.map((mode) => (
                                  <option key={mode.value} value={mode.value}>{mode.labelFa}</option>
                                ))}
                              </select>
                            </label>
                          </div>
                        ) : null}

                        {editIssues.length > 0 && editingId === policy.packagingPolicyId && (
                          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid={`shipping-packaging-edit-issues-${policy.packagingPolicyId}`}>
                            {editIssues.map((issue) => <li key={issue}>{issue}</li>)}
                          </ul>
                        )}

                        <div className="flex flex-wrap gap-2">
                          {editingId === policy.packagingPolicyId ? (
                            <>
                              <button
                                type="button"
                                className="rounded bg-blue-600 px-3 py-1 text-xs font-bold text-white disabled:opacity-50"
                                disabled={busy}
                                onClick={() => void submitEdit(policy)}
                                data-testid={`shipping-packaging-save-${policy.packagingPolicyId}`}
                              >
                                {busy ? "در حال ذخیره…" : "ذخیره"}
                              </button>
                              <button
                                type="button"
                                className="rounded border border-gray-300 px-3 py-1 text-xs"
                                onClick={() => { setEditingId(null); setEditIssues([]); }}
                              >
                                انصراف
                              </button>
                            </>
                          ) : (
                            <>
                              <button
                                type="button"
                                className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                                disabled={busy}
                                onClick={() => startEdit(policy)}
                                data-testid={`shipping-packaging-edit-${policy.packagingPolicyId}`}
                              >
                                ویرایش
                              </button>
                              <button
                                type="button"
                                className="rounded border border-gray-300 px-3 py-1 text-xs disabled:opacity-50"
                                disabled={busy}
                                onClick={() => void toggle(policy)}
                                data-testid={`shipping-packaging-toggle-${policy.packagingPolicyId}`}
                              >
                                {policy.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                              </button>
                            </>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                ))
              )}
            </section>

            <section className="space-y-3 rounded-lg border border-dashed border-gray-300 p-3">
              <h3 className="text-sm font-bold text-gray-800">افزودن سیاست بسته‌بندی</h3>
              <ShippingDestinationPicker selection={selection} onChange={setSelection} disabled={busy} />

              <label className="block text-[11px] text-gray-600">
                گزینهٔ خدمت
                <select
                  className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                  value={createOptionCode}
                  onChange={(e) => { setCreateOptionCode(e.target.value); setCreateIssues([]); }}
                  disabled={busy}
                  data-testid="shipping-packaging-create-option"
                >
                  <option value={SERVICE_LEVEL_PACKAGING_VALUE}>سطح خدمت (پیش‌فرض خدمت)</option>
                  {options.map((option) => (
                    <option key={option.code} value={option.code}>{option.labelFa} ({option.code})</option>
                  ))}
                </select>
                <span className="mt-1 block text-[10px] text-gray-500">{SERVICE_LEVEL_PACKAGING_FA}</span>
              </label>

              {optionIssues.length > 0 && (
                <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-packaging-create-option-issues">
                  {optionIssues.map((issue) => <li key={issue}>{issue}</li>)}
                </ul>
              )}

              <div className="grid gap-2 sm:grid-cols-3">
                <label className="text-[11px] text-gray-600">
                  هزینهٔ بسته‌بندی
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    dir="ltr"
                    inputMode="decimal"
                    placeholder="12000"
                    value={createForm.feeAmount}
                    onChange={(e) => { setCreateForm({ ...createForm, feeAmount: e.target.value }); setCreateIssues([]); }}
                    data-testid="shipping-packaging-create-fee"
                  />
                </label>
                <label className="text-[11px] text-gray-600">
                  ارز
                  <input
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    dir="ltr"
                    value={createForm.currencyCode}
                    onChange={(e) => { setCreateForm({ ...createForm, currencyCode: e.target.value }); setCreateIssues([]); }}
                    data-testid="shipping-packaging-create-currency"
                  />
                </label>
                <label className="text-[11px] text-gray-600">
                  نحوهٔ پرداخت کرایه
                  <select
                    className="mt-1 w-full rounded border border-gray-300 px-2 py-1 text-xs"
                    value={createForm.payerMode}
                    onChange={(e) => setCreateForm({ ...createForm, payerMode: e.target.value as ShippingPackagingFormValues["payerMode"] })}
                    disabled={busy}
                    data-testid="shipping-packaging-create-payer"
                  >
                    {PAYER_MODES.map((mode) => (
                      <option key={mode.value} value={mode.value}>{mode.labelFa}</option>
                    ))}
                  </select>
                </label>
              </div>

              <p className="text-[10px] text-gray-500" data-testid="shipping-packaging-create-hint">
                هزینهٔ صفر یک سیاست واقعی است؛ خالی‌بودن فهرست یعنی «هزینهٔ بسته‌بندی صفر» و نه هزینهٔ پنهان.
              </p>

              {createIssues.length > 0 && (
                <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-packaging-create-issues">
                  {createIssues.map((issue) => <li key={issue}>{issue}</li>)}
                </ul>
              )}

              <button
                type="button"
                className="rounded bg-blue-600 px-3 py-1 text-xs font-bold text-white disabled:opacity-50"
                disabled={busy}
                onClick={() => void submitCreate()}
                data-testid="shipping-packaging-create-submit"
              >
                {busy ? "در حال ذخیره…" : "افزودن سیاست بسته‌بندی"}
              </button>
            </section>
          </>
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
