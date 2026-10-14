"use client";

/**
 * TB-SHIPPING-MVP-ADMIN-LIVE-WIRING-055 — per-service rates & coverage section, reachable from the
 * existing /admin/shipping-services rows. Reads the real Shipping backend view, and lets the operator
 * update money/lead-time of a rate and toggle activation of rates and coverage grants.
 *
 * Creating a new rate/grant needs a destination scope. No authorized API currently returns stable
 * Geography ids, so the create controls stay visibly disabled rather than letting an operator submit
 * an invented id; that blocker is shown inline and reported, never faked.
 */
import { useCallback, useEffect, useState } from "react";
import { Dialog } from "../../../design-system";
import {
  loadAdminShippingRates,
  setAdminShippingCoverageGrantActive,
  setAdminShippingRateActive,
  updateAdminShippingRate,
  type ShippingRatesAndCoverageView,
  type ShippingRatePolicyAdminItem,
} from "./shipping-rates-coverage-api.ts";
import {
  GEOGRAPHY_ID_LOOKUP_BLOCKED_FA,
  coverageStateFa,
  formatCharge,
  formatFreeAboveThreshold,
  rateFormFromPolicy,
  rateFormToPayload,
  scopeSummary,
  summarizeRatesCoverage,
  validateShippingRateForm,
  type ShippingRateFormValues,
} from "./shipping-rates-coverage-model.ts";

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

  useEffect(() => {
    if (open) {
      setEditingId(null);
      setIssues([]);
      setForm(rateFormFromPolicy(null));
      void refresh();
    }
  }, [open, refresh]);

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
      ...payload,
    });
    setBusy(false);
    if (result.state !== "ok") {
      onNotice({ tone: "error", text: errorFa(result.message, "به‌روزرسانی نرخ ناموفق بود.") });
      return;
    }
    setEditingId(null);
    onNotice({ tone: "success", text: `نرخ «${scopeSummary(result.data!)}» ذخیره شد.` });
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

        <p className="rounded-lg bg-amber-50 p-3 text-xs text-amber-800" data-testid="shipping-rates-geography-blocker">
          {GEOGRAPHY_ID_LOOKUP_BLOCKED_FA}
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
              {view && view.ratePolicies.length === 0 && (
                <p className="text-xs text-gray-500">هیچ سیاست نرخی ثبت نشده است.</p>
              )}
              {view?.ratePolicies.map((policy) => (
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

            <section className="space-y-2 rounded-lg border border-dashed border-gray-300 p-3">
              <h3 className="text-sm font-bold text-gray-800">افزودن نرخ / پوشش</h3>
              <button
                type="button"
                disabled
                className="rounded bg-gray-200 px-3 py-1 text-xs font-bold text-gray-500"
                data-testid="shipping-rates-create-disabled"
              >
                افزودن نرخ (غیرفعال تا فراهم‌شدن شناسه‌های Geography)
              </button>
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
  return fallback;
}
