"use client";

/**
 * زبانهٔ «عمومی» یک روش یک‌سطحی — هویت، نام نمایشی و وضعیت فعال‌بودن همان روش.
 *
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
 *
 * قواعد سخت:
 * - تنها دو نوشتن مجاز این زبانه همان دو نوشتن مجاز قفل‌شدهٔ کاتالوگ یک‌سطحی است:
 *   ۱) نام نمایشی روش در زبان فعال (TB-094)؛ ۲) فعال/غیرفعال‌سازی روش گزینه‌دار (TB-096).
 *   هیچ نوشتن دیگری روی سرویس/گزینه/نرخ/پوشش/برنامه/بسته‌بندی از این زبانه انجام نمی‌شود.
 * - هویت ارائه‌دهنده **فرادادهٔ مشترک** است: صریحاً برچسب می‌خورد و هیچ سیاست مشترکی به‌عنوان
 *   سیاست اختصاصی این روش نمایش داده یا نوشته نمی‌شود.
 * - فعال‌سازی فقط وقتی نشان داده می‌شود که پیوند سرور `optionCode` غیرتهی داشته باشد؛ روش بدون
 *   گزینه و پیوند حل‌نشده هیچ سوئیچ ساختگی دریافت نمی‌کنند.
 * - هیچ موفقیت جعلی ساخته نمی‌شود: پس از هر نوشتن، منبع حقیقت از سرور دوباره خوانده می‌شود.
 */

import { useEffect, useState } from "react";
import { Button, Field, Input } from "../../../design-system";
import { saveOneLevelMethodName } from "./shipping-one-level-method-name-api.ts";
import {
  ONE_LEVEL_METHOD_NAME_MAX_LENGTH,
  oneLevelMethodNameIssueFa,
  validateOneLevelMethodName,
} from "./shipping-one-level-method-name-model.ts";
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
import type { FlatMethodRow } from "./flat-method-rows.ts";

export function FlatGeneralTab({
  row,
  readOnly,
  activeLanguageId,
  binding,
  onNotice,
  onSaved,
}: {
  row: FlatMethodRow;
  readOnly: boolean;
  activeLanguageId: string;
  binding: OneLevelBindingState;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  onSaved: (text: string) => Promise<void>;
}) {
  const [name, setName] = useState(row.label);
  const [issue, setIssue] = useState<"required" | "too_long" | null>(null);
  const [saving, setSaving] = useState(false);
  const [activationBusy, setActivationBusy] = useState(false);

  useEffect(() => {
    setName(row.label);
    setIssue(null);
  }, [row.label, row.methodCode]);

  const eligibility = resolveActivationEligibility(binding, row.methodCode);
  const resolved = binding.state === "resolved" ? binding.binding : null;
  const dirty = name.trim() !== row.label.trim();

  async function submitName() {
    const validation = validateOneLevelMethodName(name);
    setIssue(validation);
    if (validation !== null || !activeLanguageId) return;
    setSaving(true);
    const result = await saveOneLevelMethodName(row.methodCode, activeLanguageId, name.trim());
    setSaving(false);
    if (result.state === "denied") {
      onNotice({ tone: "error", text: "دسترسی به ویرایش نام روش ارسال مجاز نیست." });
      return;
    }
    if (result.state !== "ok" || !result.data) {
      onNotice({ tone: "error", text: "ذخیرهٔ نام روش ارسال ناموفق بود." });
      return;
    }
    await onSaved(`نام روش «${result.data.name}» ذخیره شد.`);
  }

  async function applyActivation() {
    if (eligibility.state !== "supported") return;
    const next = !eligibility.currentIsActive;
    setActivationBusy(true);
    const result = await setOneLevelMethodActivation(row.methodCode, next);
    setActivationBusy(false);
    if (result.state !== "ok" || !result.data) {
      onNotice({ tone: "error", text: activationErrorFa(result.message ?? "") });
      return;
    }
    await onSaved(
      `روش «${row.label || row.methodCode}» ${activationStatusFa(result.data.isActive)} شد.`,
    );
  }

  const issueText = oneLevelMethodNameIssueFa(issue);

  return (
    <div className="space-y-4" data-testid="admin-shipping-method-general">
      <section className="space-y-2 rounded-xl border border-gray-200 p-3">
        <h2 className="text-sm font-bold text-gray-900">هویت روش</h2>
        <p className="text-xs text-gray-600">
          کد روش (تغییرناپذیر): <span dir="ltr" className="font-mono">{row.methodCode}</span>
        </p>
        <p className="text-xs text-gray-600">
          وضعیت فعلی: <span className="font-bold">{activationStatusFa(row.isActive)}</span>
        </p>
      </section>

      <section className="space-y-2 rounded-xl border border-amber-200 bg-amber-50 p-3" data-testid="admin-shipping-method-shared-warning">
        <h2 className="text-sm font-bold text-amber-900">هویت ارائه‌دهنده — فرادادهٔ مشترک</h2>
        <p className="text-xs text-amber-900">
          این بخش مشترک بین همهٔ روش‌های ارائه‌دهندهٔ یکسان است و سیاست اختصاصی این روش نیست؛
          تنظیمات تعرفه، پوشش، برنامه و بسته‌بندی هر روش در زبانهٔ خودش و مستقل نگهداری می‌شود.
        </p>
        <p className="text-xs text-amber-900">
          سرویس authoritative: <span dir="ltr" className="font-mono">{resolved ? resolved.serviceCode : "—"}</span>
          <span className="mx-2">·</span>
          گزینه authoritative:{" "}
          <span dir="ltr" className="font-mono">
            {resolved ? (resolved.optionCode ?? "سطح سرویس (بدون گزینه)") : "—"}
          </span>
          {binding.state === "untrusted" ? <span className="mr-2">(پیوند از سرور تأیید نشد)</span> : null}
        </p>
        <p className="text-[11px] text-amber-800">
          در این صفحه هویت ارائه‌دهنده/گزینه ویرایش نمی‌شود تا هیچ سیاست مشترکی به‌عنوان سیاست
          اختصاصی این روش نوشته نشود.
        </p>
      </section>

      <section className="space-y-3 rounded-xl border border-gray-200 p-3" data-testid="admin-shipping-method-name-editor">
        <h2 className="text-sm font-bold text-gray-900">نام نمایشی روش</h2>
        {readOnly ? (
          <p className="text-sm text-gray-700" data-testid="admin-shipping-method-name-readonly">
            {row.label || "در زبان فعال ثبت نشده است"}
          </p>
        ) : (
          <>
            <Field
              id="admin-shipping-method-name"
              label="نام نمایشی در زبان فعال"
              hint={`فقط نام همین روش در یک زبان نوشته می‌شود؛ کد روش تغییر نمی‌کند. حداکثر ${ONE_LEVEL_METHOD_NAME_MAX_LENGTH} نویسه.`}
            >
              <Input
                data-testid="admin-shipping-method-name-input"
                dir="auto"
                value={name}
                disabled={saving || !activeLanguageId}
                invalid={issue !== null}
                onChange={(event) => {
                  setName(event.target.value);
                  setIssue(null);
                }}
              />
            </Field>
            {activeLanguageId ? null : (
              <p className="text-xs text-amber-700" data-testid="admin-shipping-method-name-language-missing">
                زبان فعال از سرور اعلام نشده است؛ تا آن زمان ویرایش نام انجام نمی‌شود.
              </p>
            )}
            {issueText ? (
              <p className="text-sm text-danger" role="alert" data-testid="admin-shipping-method-name-issue">
                {issueText}
              </p>
            ) : null}
            <div className="flex justify-end">
              <Button
                type="button"
                data-testid="admin-shipping-method-name-save"
                disabled={saving || !activeLanguageId || !dirty}
                onClick={() => void submitName()}
              >
                {saving ? "در حال ذخیره…" : "ذخیرهٔ نام"}
              </Button>
            </div>
          </>
        )}
      </section>

      <section className="space-y-2 rounded-xl border border-gray-200 p-3" data-testid="admin-shipping-method-activation">
        <h2 className="text-sm font-bold text-gray-900">فعال‌بودن روش</h2>
        {eligibility.state === "supported" ? (
          <>
            <p className="text-xs text-gray-600">{activationImpactFa(eligibility.currentIsActive)}</p>
            <p className="text-xs text-gray-600">
              وضعیت تأییدشدهٔ سرور: <span className="font-bold">{activationStatusFa(eligibility.currentIsActive)}</span>
            </p>
            {readOnly ? (
              <p className="text-xs text-gray-500">
                برای تغییر وضعیت، روش را در حالت «ویرایش» باز کنید.
              </p>
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
    </div>
  );
}
