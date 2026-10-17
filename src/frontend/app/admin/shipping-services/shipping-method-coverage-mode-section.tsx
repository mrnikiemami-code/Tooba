"use client";

/**
 * TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — «محدودیت جغرافیایی این روش» inside the existing
 * method-specific «تعرفه و پوشش» tab.
 *
 * Rules this component is built to honor:
 *
 * - **The declaration is read, never inferred.** The mode comes from the Shipping-owned
 *   `methodCoverages` rows of the selected method only (exact service + option). No row means the
 *   documented Unrestricted default; an unreadable row makes the whole declaration untrusted and
 *   blocks writing instead of widening a real restriction.
 * - **Writes are the existing owner seams.** The mode goes through
 *   `PUT …/method-coverage` and an allowed destination through the option-scoped
 *   `POST …/coverage-grants`, both with the option of the server-resolved binding. A provider-level
 *   grant is never used as an approximation, and no rate, availability or slot is created here.
 * - **No optimistic success.** After the write, the caller re-reads the server; the success text is
 *   built from the persisted response, and a partial failure (declaration saved, grant refused) is
 *   reported as such.
 * - **Read-only and in-flight states cannot write.** The only real authorization check remains the
 *   server's 401/403, which is surfaced as a denied state.
 */

import { useEffect, useState } from "react";
import {
  createAdminShippingCoverageGrant,
  setAdminShippingMethodCoverage,
  type ShippingCoverageMode,
  type ShippingRatesAndCoverageView,
} from "./shipping-rates-coverage-api.ts";
import {
  buildScopeWrite,
  EMPTY_DESTINATION_SELECTION,
  validateDestinationSelection,
  type DestinationSelection,
} from "./shipping-rates-coverage-model.ts";
import { ShippingDestinationPicker } from "./shipping-destination-picker.tsx";
import {
  COVERAGE_GRANTS_KEPT_FA,
  COVERAGE_MODE_BOUNDARY_FA,
  coverageModeFa,
  coverageModeWritable,
  resolveCoverageMode,
  RESTRICTED_EMPTY_NOTE_FA,
  restrictedNeedsDestination,
  scopeRatesToBinding,
  type OneLevelMethodBinding,
} from "./shipping-one-level-rates-model.ts";

type Props = {
  /** پیوند authoritative همین روش؛ هویت گزینه فقط از سرور می‌آید. */
  binding: OneLevelMethodBinding;
  /** نمای نرخ/پوشش همان خدمت؛ `null` یعنی هنوز/هرگز تأیید نشده و نوشتن ممنوع است. */
  view: ShippingRatesAndCoverageView | null;
  /** حالت «مشاهده» دیالوگ جزئیات؛ هیچ نوشتنی مجاز نیست. */
  readOnly: boolean;
  /** عملیات در جریان (مشترک با بقیهٔ زبانه). */
  busy: boolean;
  /** نگهبان نسل انتخاب؛ پاسخ دیررسِ روش قبلی هرگز روی روش تازه نمی‌نشیند. */
  beginOperation: () => () => boolean;
  onBusyChange: (busy: boolean) => void;
  /** بازخوانی سرور پس از نوشتن؛ هیچ وضعیت خوش‌بینانهٔ محلی ساخته نمی‌شود. */
  onSaved: (isCurrent: () => boolean) => Promise<void>;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
};

const UNRESTRICTED_FA = "بدون محدودیت مدیریتی";
const RESTRICTED_FA = "محدود به مناطق منتخب";

export function ShippingMethodCoverageModeSection({
  binding,
  view,
  readOnly,
  busy,
  beginOperation,
  onBusyChange,
  onSaved,
  onNotice,
}: Props) {
  const modeState = resolveCoverageMode(view, binding);
  const ownActiveGrantCount = scopeRatesToBinding(view, binding).grants.filter((grant) => grant.isActive).length;

  // هدف فرم فقط از انتخاب ادمین می‌آید؛ `null` یعنی «از سرور مشتق کن»، نه یک پیش‌فرض محلی.
  const [target, setTarget] = useState<ShippingCoverageMode | null>(null);
  const [selection, setSelection] = useState<DestinationSelection>(EMPTY_DESTINATION_SELECTION);
  const [issues, setIssues] = useState<string[]>([]);

  useEffect(() => {
    setTarget(null);
    setSelection(EMPTY_DESTINATION_SELECTION);
    setIssues([]);
  }, [binding.methodCode, binding.optionCode]);

  const targetMode: ShippingCoverageMode = target
    ?? (modeState.state === "declared" ? modeState.mode : "Unrestricted");
  const canWrite = coverageModeWritable({ readOnly, busy, modeState });

  async function save(): Promise<void> {
    if (!canWrite) {
      onNotice({ tone: "error", text: readOnly ? "این روش در حالت مشاهده است؛ تغییر ذخیره نمی‌شود." : coverageModeFa(modeState) });
      return;
    }
    const hasDestination = selection.countryId.length > 0;
    if (restrictedNeedsDestination({ targetMode, activeOwnGrantCount: ownActiveGrantCount, hasDestination })) {
      setIssues([`برای «${RESTRICTED_FA}» حداقل یک مقصد مجاز انتخاب کنید؛ در غیر این صورت این روش هیچ مقصدی نخواهد داشت.`]);
      return;
    }
    const scopeProblems = hasDestination ? validateDestinationSelection(selection) : [];
    if (scopeProblems.length > 0) {
      setIssues(scopeProblems);
      return;
    }

    setIssues([]);
    const isCurrent = beginOperation();
    onBusyChange(true);

    // ۱) اعلام حالت از درز مالک Shipping؛ پاسخ، ردیف ذخیره‌شده است نه پژواک خوش‌بینانه.
    const declared = await setAdminShippingMethodCoverage(binding.serviceCode, {
      optionCode: binding.optionCode,
      mode: targetMode,
    });
    if (!isCurrent()) return;
    if (declared.state !== "ok") {
      onBusyChange(false);
      onNotice({ tone: "error", text: writeErrorFa(declared.message, "ذخیرهٔ محدودیت جغرافیایی ناموفق بود.") });
      return;
    }

    // ۲) مقصد انتخاب‌شده به‌عنوان مجوز همین روش ثبت می‌شود (هویت گزینه‌دار، نه سطح سرویس).
    let grantFailure: string | null = null;
    if (hasDestination) {
      const grant = await createAdminShippingCoverageGrant(binding.serviceCode, {
        optionCode: binding.optionCode,
        scope: buildScopeWrite(selection),
      });
      if (!isCurrent()) return;
      if (grant.state !== "ok") {
        grantFailure = writeErrorFa(grant.message, "ثبت مقصد مجاز ناموفق بود.");
      }
    }

    onBusyChange(false);
    setTarget(null);
    setSelection(EMPTY_DESTINATION_SELECTION);
    const persistedFa = declared.data!.mode === "Restricted" ? RESTRICTED_FA : UNRESTRICTED_FA;
    onNotice(
      grantFailure
        ? { tone: "error", text: `حالت «${persistedFa}» ذخیره شد؛ اما ${grantFailure}` }
        : {
          tone: "success",
          text: declared.data!.mode === "Restricted"
            ? "محدودیت جغرافیایی این روش روی «محدود به مناطق منتخب» ذخیره شد."
            : "محدودیت جغرافیایی این روش روی «بدون محدودیت مدیریتی» ذخیره شد؛ مجوزهای قبلی حذف نشدند.",
        },
    );
    // هیچ وضعیت محلی خوش‌بینانه‌ای ساخته نمی‌شود: همان حقیقت سرور دوباره خوانده می‌شود.
    await onSaved(isCurrent);
  }

  return (
    <section className="space-y-2 rounded-ds border border-border p-3" data-testid="shipping-one-level-coverage-mode">
      <h3 className="text-sm font-bold">محدودیت جغرافیایی این روش</h3>
      <p
        className="text-xs"
        data-testid="shipping-one-level-coverage-mode-state"
        data-mode={modeState.state === "declared" ? modeState.mode : modeState.state}
      >
        {coverageModeFa(modeState)}
      </p>
      <p className="text-[11px] text-muted" data-testid="shipping-one-level-coverage-mode-boundary">
        {COVERAGE_MODE_BOUNDARY_FA}
      </p>
      {modeState.state === "declared" && modeState.mode === "Restricted" && ownActiveGrantCount === 0 && (
        <p className="text-[11px] font-bold text-amber-800" data-testid="shipping-one-level-coverage-restricted-empty">
          {RESTRICTED_EMPTY_NOTE_FA}
        </p>
      )}
      <p className="text-[11px] text-muted" data-testid="shipping-one-level-coverage-mode-audit">
        {COVERAGE_GRANTS_KEPT_FA}
      </p>

      <div className="flex flex-wrap gap-4 text-xs" role="radiogroup" aria-label="حالت محدودیت جغرافیایی">
        <label className="inline-flex items-center gap-2">
          <input
            type="radio"
            name={`shipping-one-level-coverage-mode-${binding.methodCode}`}
            value="Unrestricted"
            checked={targetMode === "Unrestricted"}
            disabled={!canWrite}
            onChange={() => { setTarget("Unrestricted"); setIssues([]); }}
            data-testid="shipping-one-level-coverage-mode-unrestricted"
          />
          {UNRESTRICTED_FA}
        </label>
        <label className="inline-flex items-center gap-2">
          <input
            type="radio"
            name={`shipping-one-level-coverage-mode-${binding.methodCode}`}
            value="Restricted"
            checked={targetMode === "Restricted"}
            disabled={!canWrite}
            onChange={() => { setTarget("Restricted"); setIssues([]); }}
            data-testid="shipping-one-level-coverage-mode-restricted"
          />
          {RESTRICTED_FA}
        </label>
      </div>

      {targetMode === "Restricted" && (
        <div className="space-y-2" data-testid="shipping-one-level-coverage-mode-destination">
          <p className="text-[11px] text-muted">
            مقصد مجاز را از فهرست مرجع Geography انتخاب کنید؛ فقط مقصدهای همین روش مجاز می‌شوند و
            مجوز روش‌های دیگر به این روش به ارث نمی‌رسد.
          </p>
          <ShippingDestinationPicker
            selection={selection}
            onChange={(next) => { setSelection(next); setIssues([]); }}
            disabled={!canWrite}
          />
        </div>
      )}

      {issues.length > 0 && (
        <ul className="list-inside list-disc text-[11px] text-red-600" data-testid="shipping-one-level-coverage-mode-issues">
          {issues.map((issue) => <li key={issue}>{issue}</li>)}
        </ul>
      )}

      <button
        type="button"
        className="rounded-ds bg-primary px-3 py-1 text-xs font-bold text-primary-foreground disabled:opacity-50"
        disabled={!canWrite}
        onClick={() => void save()}
        data-testid="shipping-one-level-coverage-mode-save"
      >
        ذخیرهٔ محدودیت جغرافیایی
      </button>
      {readOnly && (
        <p className="text-[11px] text-muted" data-testid="shipping-one-level-coverage-mode-read-only">
          این روش در حالت مشاهده باز شده است؛ هیچ تغییری ذخیره نمی‌شود.
        </p>
      )}
    </section>
  );
}

/** خطاهای پایدار همین نوشتن‌ها؛ متن ناشناخته هرگز به «موفق» تبدیل نمی‌شود. */
function writeErrorFa(code: string | undefined, fallback: string): string {
  if (!code) return fallback;
  if (code === "admin.authorization.denied") return "دسترسی به ویرایش محدودیت این روش ندارید.";
  if (code === "host-unreachable") return "ارتباط با سرور برقرار نشد؛ هیچ تغییری ذخیره نشد.";
  if (code === "shipping.service_scope.mismatch") return "این ردیف به خدمت دیگری تعلق دارد.";
  if (code === "shipping.coverage_grant.scope_duplicate") return "این مقصد از قبل برای همین روش مجاز شده است.";
  if (code === "shipping.coverage.scope.invalid") return "دامنهٔ مقصد انتخاب‌شده معتبر نیست؛ آن را از فهرست مرجع دوباره انتخاب کنید.";
  if (code === "shipping.service_code.required") return "کد خدمت این روش برای ذخیره معتبر نیست.";
  if (code?.endsWith(".invalid_response")) return "پاسخ سرور برای این نوشتن معتبر نبود؛ برای احتیاط موفق در نظر گرفته نمی‌شود.";
  return fallback;
}
