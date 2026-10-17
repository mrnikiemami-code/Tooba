"use client";

/**
 * TB-SHIPPING-ONE-LEVEL-TABS-087 — tab 3 «بسته‌بندی و کرایه» of the one-level shipping shell.
 *
 * Scope rules honored here:
 *
 * - **One selected one-level method is the whole context.** The tab never renders an option
 *   dropdown and never renders an editable parent-service grouping. The only method facts shown are
 *   its own code plus the server-resolved legacy binding.
 * - **The binding comes only from the server** (086 resolver). `method_code` is never split in the
 *   browser; a raw client-provided `serviceCode`/`optionCode` can never substitute for it.
 * - **Money and payer-mode semantics are reused, not reimplemented.** Fee parsing, the `decimal(18,2)`
 *   round trip, currency normalization, the Prepaid/CollectOnDelivery contract and every Persian
 *   refusal message come from `shipping-packaging-model.ts` unchanged.
 * - **Payer mode is a quotation fact.** Nothing here creates a Payment, collects money, settles a
 *   collect-on-delivery amount, or changes Checkout pricing.
 * - **The selection is immutable while an operation is pending.** A generation counter is bumped on
 *   every method change so an in-flight read of the previous method can never land on the new one,
 *   and a completion notice stays tied to the identity that started the operation.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  createAdminShippingPackagingPolicy,
  loadAdminShippingPackagingPolicies,
  setAdminShippingPackagingPolicyActive,
  updateAdminShippingPackagingPolicy,
  type ShippingFreightPayerMode,
  type ShippingPackagingPoliciesView,
  type ShippingPackagingPolicyAdminItem,
} from "./shipping-packaging-api.ts";
import {
  emptyPackagingForm,
  formatPackagingFee,
  mapPackagingErrorFa,
  NO_PACKAGING_POLICY_FA,
  packagingFormFromPolicy,
  packagingFormToPayload,
  packagingStateFa,
  PAYER_MODE_IS_QUOTE_FACT_FA,
  payerModeLabelFa,
  summarizePackagingPolicies,
  validatePackagingForm,
  type ShippingPackagingFormValues,
} from "./shipping-packaging-model.ts";
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
  ONE_LEVEL_NO_PACKAGING_FA,
  ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA,
  ONE_LEVEL_PACKAGING_IDENTITY_ISOLATION_FA,
  ONE_LEVEL_PACKAGING_LEGACY_ROW_FA,
  ONE_LEVEL_PACKAGING_READ_ONLY_FA,
  operationStillTargetsMethod,
  packagingViewMatchesBindingService,
  packagingWritesAllowed,
  packagingWriteTarget,
  resolvePackagingState,
  scopePackagingToBinding,
  type OneLevelBindingState,
} from "./shipping-one-level-packaging-model.ts";

const PAYER_MODES: { value: ShippingFreightPayerMode; labelFa: string }[] = [
  { value: "Prepaid", labelFa: "پیش‌کرایه" },
  { value: "CollectOnDelivery", labelFa: "پس‌کرایه" },
];

type Props = {
  /** کد روش یک‌سطحی انتخاب‌شده در زبانهٔ «سرویس‌ها». تهی یعنی هیچ روشی انتخاب نشده است. */
  methodCode: string;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /**
   * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146: پیوند حل‌شده از درخت واقعی سرور
   * (`/v1/admin/shipping-methods`)؛ `serviceCode` و `optionCode` فیلدهای جدا و authoritative هستند و
   * هیچ تقسیم متنیِ کد روش در مرورگر انجام نمی‌شود. در حضور آن، resolver غایب صدا زده نمی‌شود.
   */
  bindingOverride?: { serviceCode: string; optionCode: string | null; isActive: boolean } | null;
  /**
   * TB-SHIPPING-ADMIN-METHOD-PACKAGING-176: حالت مشاهده هیچ نوشتنی ندارد. پیش از این، دیالوگ
   * «مشاهده» این پرچم را به این زبانه نمی‌داد و سیاست بسته‌بندی از مسیر فقط-خواندنی قابل ویرایش بود.
   */
  readOnly?: boolean;
};

export function ShippingOneLevelPackagingTab({ methodCode, onNotice, bindingOverride = null, readOnly = false }: Props) {
  const [bindingLoading, setBindingLoading] = useState(false);
  const [binding, setBinding] = useState<OneLevelBindingState | null>(null);
  const [bindingAttempt, setBindingAttempt] = useState(0);
  const [policiesState, setPoliciesState] = useState<"ok" | "denied" | "error" | null>(null);
  const [view, setView] = useState<ShippingPackagingPoliciesView | null>(null);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ tone: "success" | "error"; text: string } | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<ShippingPackagingFormValues>(emptyPackagingForm());
  const [editIssues, setEditIssues] = useState<string[]>([]);

  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [createForm, setCreateForm] = useState<ShippingPackagingFormValues>(emptyPackagingForm());
  const [createIssues, setCreateIssues] = useState<string[]>([]);

  const guard = useRef(createBindingGenerationGuard());
  const resolvedBinding = binding?.state === "resolved" ? binding.binding : null;

  const emit = useCallback(
    (next: { tone: "success" | "error"; text: string }) => {
      setNotice(next);
      onNotice(next);
    },
    [onNotice],
  );

  const refresh = useCallback(async (serviceCode: string, isCurrent: () => boolean) => {
    setPoliciesState(null);
    const result = await loadAdminShippingPackagingPolicies(serviceCode);
    if (!isCurrent()) return;
    if (result.state !== "ok" || !result.data) {
      setPoliciesState(result.state === "denied" ? "denied" : "error");
      setView(null);
      return;
    }
    setView(result.data);
    setPoliciesState("ok");
  }, []);

  useEffect(() => {
    // بازنشانی کامل وضعیت ویرایش هنگام تغییر روش انتخاب‌شده.
    setEditingId(null);
    setEditForm(emptyPackagingForm());
    setEditIssues([]);
    setSelection(EMPTY_DESTINATION_SELECTION);
    setCreateForm(emptyPackagingForm());
    setCreateIssues([]);
    setNotice(null);
    setView(null);
    setPoliciesState(null);
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
      // تنها مسیر مجاز خواندن پیوند؛ تفکیک شکست در همان کلاینت مشترک انجام می‌شود.
      const next = await loadOneLevelBinding(methodCode);
      if (!isCurrent()) return;
      setBinding(next);
      setBindingLoading(false);
      if (next.state === "resolved") {
        void refresh(next.binding.serviceCode, isCurrent);
      }
    })();
  }, [methodCode, refresh, bindingOverride?.serviceCode, bindingOverride?.optionCode, bindingOverride?.isActive, bindingAttempt]);

  const state = resolvePackagingState({
    hasMethod: methodCode.length > 0,
    bindingLoading,
    bindingResolved: resolvedBinding != null,
    bindingKnown: binding != null,
    policiesLoading: false,
    policiesState,
  });

  const scoped = useMemo(
    () => (resolvedBinding ? scopePackagingToBinding(view?.packagingPolicies ?? null, resolvedBinding) : []),
    [resolvedBinding, view],
  );
  const summary = useMemo(() => summarizePackagingPolicies(scoped), [scoped]);
  const writesBlocked = !packagingWritesAllowed(readOnly);

  async function submitCreate() {
    if (!resolvedBinding) return;
    if (writesBlocked) {
      emit({ tone: "error", text: ONE_LEVEL_PACKAGING_READ_ONLY_FA });
      return;
    }
    const scopeProblems = validateDestinationSelection(selection);
    const formProblems = validatePackagingForm(createForm).map((issue) => issue.messageFa);
    if (scopeProblems.length > 0 || formProblems.length > 0) {
      setCreateIssues([...scopeProblems, ...formProblems]);
      return;
    }
    setCreateIssues([]);
    const target = packagingWriteTarget(resolvedBinding);
    const startedFor = methodCode;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await createAdminShippingPackagingPolicy(target.serviceCode, {
      scope: buildScopeWrite(selection),
      // هویت گزینه فقط از پیوند سرور می‌آید؛ هیچ‌گاه از کد روش یا ورودی کاربر.
      optionCode: target.optionCode,
      ...packagingFormToPayload(createForm),
    });
    setBusy(false);
    if (!operationStillTargetsMethod(startedFor, methodCode)) {
      // روش عوض شده است؛ نتیجه به روش جدید نسبت داده نمی‌شود و اعلان آن به هویت اصلی می‌ماند.
      return;
    }
    if (result.state !== "ok") {
      emit({ tone: "error", text: mapPackagingErrorFa(result.message, "ایجاد سیاست بسته‌بندی ناموفق بود.") });
      return;
    }
    emit({
      tone: "success",
      text: `سیاست بسته‌بندی «${scopeSummary(result.data!)} · ${formatPackagingFee(result.data!.feeAmount, result.data!.currencyCode)} · ${payerModeLabelFa(result.data!.freightPayerMode)}» برای روش ${startedFor} ایجاد شد.`,
    });
    setCreateForm(emptyPackagingForm(createForm.currencyCode));
    if (isCurrent()) await refresh(target.serviceCode, isCurrent);
  }

  async function submitEdit(policy: ShippingPackagingPolicyAdminItem) {
    if (!resolvedBinding) return;
    if (writesBlocked) {
      emit({ tone: "error", text: ONE_LEVEL_PACKAGING_READ_ONLY_FA });
      return;
    }
    const problems = validatePackagingForm(editForm).map((issue) => issue.messageFa);
    if (problems.length > 0) {
      setEditIssues(problems);
      return;
    }
    setEditIssues([]);
    const target = packagingWriteTarget(resolvedBinding);
    const startedFor = methodCode;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await updateAdminShippingPackagingPolicy(target.serviceCode, policy.packagingPolicyId, {
      // دامنه و هویت گزینه تغییرناپذیرند: مقدار ذخیره‌شده بازگردانده می‌شود تا سیاست
      // در سکوت به گزینه یا دامنهٔ دیگری منتقل نشود.
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
    if (!operationStillTargetsMethod(startedFor, methodCode)) return;
    if (result.state !== "ok") {
      emit({ tone: "error", text: mapPackagingErrorFa(result.message, "به‌روزرسانی سیاست بسته‌بندی ناموفق بود.") });
      return;
    }
    setEditingId(null);
    emit({
      tone: "success",
      text: `سیاست بسته‌بندی «${scopeSummary(result.data!)} · ${formatPackagingFee(result.data!.feeAmount, result.data!.currencyCode)} · ${payerModeLabelFa(result.data!.freightPayerMode)}» برای روش ${startedFor} ذخیره شد.`,
    });
    if (isCurrent()) await refresh(target.serviceCode, isCurrent);
  }

  async function toggle(policy: ShippingPackagingPolicyAdminItem) {
    if (!resolvedBinding) return;
    if (writesBlocked) {
      emit({ tone: "error", text: ONE_LEVEL_PACKAGING_READ_ONLY_FA });
      return;
    }
    const target = packagingWriteTarget(resolvedBinding);
    const startedFor = methodCode;
    const isCurrent = guard.current.begin();
    setBusy(true);
    const result = await setAdminShippingPackagingPolicyActive(target.serviceCode, policy.packagingPolicyId, !policy.isActive);
    setBusy(false);
    if (!operationStillTargetsMethod(startedFor, methodCode)) return;
    if (result.state !== "ok") {
      emit({ tone: "error", text: mapPackagingErrorFa(result.message, "تغییر وضعیت سیاست بسته‌بندی ناموفق بود.") });
      return;
    }
    emit({
      tone: "success",
      text: `سیاست بسته‌بندی «${scopeSummary(result.data!)}» برای روش ${startedFor} ${result.data!.isActive ? "فعال" : "غیرفعال"} شد.`,
    });
    if (isCurrent()) await refresh(target.serviceCode, isCurrent);
  }

  /** ورود به ویرایش یک ردیف؛ در حالت مشاهده حتی باز شدن فرم ویرایش مجاز نیست. */
  function startEdit(policy: ShippingPackagingPolicyAdminItem) {
    if (writesBlocked) {
      emit({ tone: "error", text: ONE_LEVEL_PACKAGING_READ_ONLY_FA });
      return;
    }
    setEditingId(policy.packagingPolicyId);
    setEditForm(packagingFormFromPolicy(policy));
    setEditIssues([]);
  }

  if (state === "no-method") {
    return (
      <div className="rounded-ds border border-dashed border-border p-6" data-testid="shipping-one-level-packaging-no-method">
        <p className="font-medium">هیچ روشی انتخاب نشده است</p>
        <p className="mt-1 text-sm text-muted">
          برای دیدن بسته‌بندی و کرایه، ابتدا در زبانهٔ «سرویس‌ها» یک روش یک‌سطحی را انتخاب کنید.
        </p>
      </div>
    );
  }

  if (state === "binding-loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-packaging-binding-loading">
        در حال خواندن پیوند سرویس/گزینهٔ این روش…
      </p>
    );
  }

  if (state === "binding-unavailable") {
    return (
      <div className="rounded-ds border border-border p-6" data-testid="shipping-one-level-packaging-binding-unavailable">
        <p className="font-medium">بسته‌بندی و کرایهٔ این روش در دسترس نیست</p>
        <p className="mt-1 text-sm text-muted">{bindingUnavailableFa(binding, methodCode)}</p>
        <p className="mt-2 text-xs text-muted">
          نگاشت کد روش به سرویس/گزینه فقط سمت سرور انجام می‌شود؛ این صفحه هیچ حدسی نمی‌زند.
        </p>
        {isRetryableBindingFailure(binding) ? (
          <button
            type="button"
            className="mt-3 rounded-ds border border-border px-3 py-1 text-xs"
            data-testid="shipping-one-level-packaging-binding-retry"
            onClick={() => setBindingAttempt((value) => value + 1)}
          >
            تلاش دوباره
          </button>
        ) : null}
      </div>
    );
  }

  if (state === "loading") {
    return (
      <p className="p-6 text-sm text-muted" data-testid="shipping-one-level-packaging-loading">
        در حال بارگذاری سیاست‌های بسته‌بندی…
      </p>
    );
  }

  if (state === "denied") {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-packaging-denied">
        دسترسی لازم برای مدیریت بسته‌بندی و کرایهٔ این روش را ندارید.
      </p>
    );
  }

  if (state === "error" || !resolvedBinding) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-packaging-error">
        خطا در خواندن سیاست‌های بسته‌بندی این روش؛ دوباره تلاش کنید.
      </p>
    );
  }

  // پاسخِ خدمتِ دیگر هرگز زیر نام این روش رندر یا ویرایش نمی‌شود.
  if (!packagingViewMatchesBindingService(view, resolvedBinding)) {
    return (
      <p className="p-6 text-sm text-red-600" data-testid="shipping-one-level-packaging-service-mismatch">
        پاسخ سرور به خدمت این روش تعلق ندارد؛ برای احتیاط هیچ سیاستی نمایش داده نمی‌شود.
      </p>
    );
  }

  return (
    <div className="space-y-4" data-testid="shipping-one-level-packaging-tab">
      <div className="rounded-ds border border-border p-3">
        <p className="text-sm font-medium" data-testid="shipping-one-level-packaging-context">
          {bindingContextFa(resolvedBinding)}
        </p>
        <p className="mt-1 text-xs text-muted">
          هر روش یک ردیف مستقل است. این بخش روی همان روش انتخاب‌شده عمل می‌کند و هیچ سطح دوم پیکربندی نمی‌سازد.
        </p>
      </div>

      {notice && (
        <p
          className={notice.tone === "success" ? "text-sm text-emerald-700" : "text-sm text-red-600"}
          data-testid="shipping-one-level-packaging-notice"
        >
          {notice.text}
        </p>
      )}

      {readOnly && (
        <p
          className="rounded-ds border border-border bg-secondary p-3 text-[11px] text-secondary-foreground"
          data-testid="shipping-one-level-packaging-read-only"
        >
          {ONE_LEVEL_PACKAGING_READ_ONLY_FA}
        </p>
      )}

      <div className="space-y-1 rounded-ds border border-border p-3" data-testid="shipping-one-level-packaging-scope">
        <p className="text-[11px] text-muted" data-testid="shipping-one-level-packaging-customer-effect">
          {ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA}
        </p>
        <p className="text-[11px] text-muted" data-testid="shipping-one-level-packaging-identity-isolation">
          {ONE_LEVEL_PACKAGING_IDENTITY_ISOLATION_FA}
        </p>
        <p className="text-[11px] text-muted" data-testid="shipping-one-level-packaging-legacy-row">
          {ONE_LEVEL_PACKAGING_LEGACY_ROW_FA}
        </p>
      </div>

      <div className="rounded-ds bg-secondary p-3 text-xs text-secondary-foreground">
        {scoped.length === 0 ? NO_PACKAGING_POLICY_FA : packagingStateFa(scoped)}
        <span className="mr-2 text-muted">
          (پس‌کرایه: {summary.collectOnDeliveryCount.toLocaleString("fa-IR")})
        </span>
      </div>

      <p className="rounded-ds bg-secondary p-3 text-[11px] text-secondary-foreground" data-testid="shipping-one-level-packaging-payer-note">
        {PAYER_MODE_IS_QUOTE_FACT_FA}
      </p>

      <section className="space-y-2">
        <h3 className="text-sm font-bold">سیاست‌های بسته‌بندی این روش</h3>
        {scoped.length === 0 && (
          <p className="text-xs text-muted" data-testid="shipping-one-level-packaging-empty">
            {ONE_LEVEL_NO_PACKAGING_FA}
          </p>
        )}
        {scoped.map((policy) => (
          <div
            key={policy.packagingPolicyId}
            className="space-y-2 rounded-ds border border-border p-3"
            data-testid={`shipping-one-level-packaging-row-${policy.packagingPolicyId}`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
              <span>{scopeSummary(policy)}</span>
              <span className="font-bold">{formatPackagingFee(policy.feeAmount, policy.currencyCode)}</span>
            </div>
            <div className="flex flex-wrap gap-3 text-[11px] text-muted">
              <span data-testid={`shipping-one-level-packaging-payer-${policy.packagingPolicyId}`}>
                کرایه: {payerModeLabelFa(policy.freightPayerMode)}
              </span>
              <span className={policy.isActive ? "font-bold text-emerald-700" : "font-bold text-muted"}>
                {policy.isActive ? "فعال" : "غیرفعال"}
              </span>
            </div>

            {editingId === policy.packagingPolicyId && (
              <div className="grid gap-2 sm:grid-cols-3">
                <label className="text-[11px] text-muted">
                  هزینهٔ بسته‌بندی
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    dir="ltr"
                    inputMode="decimal"
                    placeholder="12000"
                    value={editForm.feeAmount}
                    disabled={busy || writesBlocked}
                    onChange={(e) => { setEditForm({ ...editForm, feeAmount: e.target.value }); setEditIssues([]); }}
                    data-testid="shipping-one-level-packaging-edit-fee"
                  />
                </label>
                <label className="text-[11px] text-muted">
                  ارز
                  <input
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    dir="ltr"
                    value={editForm.currencyCode}
                    disabled={busy || writesBlocked}
                    onChange={(e) => { setEditForm({ ...editForm, currencyCode: e.target.value }); setEditIssues([]); }}
                    data-testid="shipping-one-level-packaging-edit-currency"
                  />
                </label>
                <label className="text-[11px] text-muted">
                  نحوهٔ پرداخت کرایه
                  <select
                    className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                    value={editForm.payerMode}
                    onChange={(e) => setEditForm({ ...editForm, payerMode: e.target.value as ShippingFreightPayerMode })}
                    disabled={busy || writesBlocked}
                    data-testid="shipping-one-level-packaging-edit-payer"
                  >
                    {PAYER_MODES.map((mode) => (
                      <option key={mode.value} value={mode.value}>{mode.labelFa}</option>
                    ))}
                  </select>
                </label>
              </div>
            )}

            {editIssues.length > 0 && editingId === policy.packagingPolicyId && (
              <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-packaging-edit-issues">
                {editIssues.map((issue) => <li key={issue}>{issue}</li>)}
              </ul>
            )}

            <div className="flex gap-2">
              {editingId === policy.packagingPolicyId ? (
                <>
                  <button
                    type="button"
                    className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
                    disabled={busy || writesBlocked}
                    onClick={() => void submitEdit(policy)}
                    data-testid={`shipping-one-level-packaging-save-${policy.packagingPolicyId}`}
                  >
                    {busy ? "در حال ذخیره…" : "ذخیره"}
                  </button>
                  <button
                    type="button"
                    className="rounded-ds border border-border px-3 py-1 text-xs"
                    onClick={() => { setEditingId(null); setEditIssues([]); }}
                  >
                    انصراف
                  </button>
                </>
              ) : (
                <>
                  <button
                    type="button"
                    className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
                    disabled={busy || writesBlocked}
                    onClick={() => startEdit(policy)}
                    data-testid={`shipping-one-level-packaging-edit-${policy.packagingPolicyId}`}
                  >
                    ویرایش
                  </button>
                  <button
                    type="button"
                    className="rounded-ds border border-border px-3 py-1 text-xs disabled:opacity-50"
                    disabled={busy || writesBlocked}
                    onClick={() => void toggle(policy)}
                    data-testid={`shipping-one-level-packaging-toggle-${policy.packagingPolicyId}`}
                  >
                    {policy.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                  </button>
                </>
              )}
            </div>
          </div>
        ))}
      </section>

      <section className="space-y-3 rounded-ds border border-dashed border-border p-3">
        <h3 className="text-sm font-bold">افزودن سیاست بسته‌بندی برای این روش</h3>
        <p className="text-[11px] text-muted">
          سیاست روی همان خدمتی ثبت می‌شود که این روش به آن پیوند دارد؛ پیوند از سرور می‌آید و قابل‌تغییر نیست.
        </p>
        <ShippingDestinationPicker selection={selection} onChange={setSelection} disabled={busy || writesBlocked} />

        <div className="grid gap-2 sm:grid-cols-3">
          <label className="text-[11px] text-muted">
            هزینهٔ بسته‌بندی
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              inputMode="decimal"
              placeholder="12000"
              value={createForm.feeAmount}
              disabled={busy || writesBlocked}
              onChange={(e) => { setCreateForm({ ...createForm, feeAmount: e.target.value }); setCreateIssues([]); }}
              data-testid="shipping-one-level-packaging-create-fee"
            />
          </label>
          <label className="text-[11px] text-muted">
            ارز
            <input
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              dir="ltr"
              value={createForm.currencyCode}
              disabled={busy || writesBlocked}
              onChange={(e) => { setCreateForm({ ...createForm, currencyCode: e.target.value }); setCreateIssues([]); }}
              data-testid="shipping-one-level-packaging-create-currency"
            />
          </label>
          <label className="text-[11px] text-muted">
            نحوهٔ پرداخت کرایه
            <select
              className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
              value={createForm.payerMode}
              onChange={(e) => setCreateForm({ ...createForm, payerMode: e.target.value as ShippingFreightPayerMode })}
              disabled={busy || writesBlocked}
              data-testid="shipping-one-level-packaging-create-payer"
            >
              {PAYER_MODES.map((mode) => (
                <option key={mode.value} value={mode.value}>{mode.labelFa}</option>
              ))}
            </select>
          </label>
        </div>

        <p className="text-[10px] text-muted">
          هزینهٔ صفر یک سیاست واقعی است؛ خالی‌بودن فهرست یعنی «هزینهٔ بسته‌بندی صفر» و نه هزینهٔ پنهان.
        </p>

        {createIssues.length > 0 && (
          <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-packaging-create-issues">
            {createIssues.map((issue) => <li key={issue}>{issue}</li>)}
          </ul>
        )}

        <button
          type="button"
          className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
          disabled={busy || writesBlocked}
          onClick={() => void submitCreate()}
          data-testid="shipping-one-level-packaging-create-submit"
        >
          {busy ? "در حال ذخیره…" : "افزودن سیاست بسته‌بندی"}
        </button>
      </section>
    </div>
  );
}
