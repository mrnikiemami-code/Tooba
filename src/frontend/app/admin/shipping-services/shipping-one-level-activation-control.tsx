"use client";

/**
 * کنترل فعال/غیرفعال‌سازی یک روش یک‌سطحی — فقط برای روش‌های گزینه‌دار.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-097 · Parent: TB-SHIPPING-ONE-LEVEL-TABS-096
 *
 * قواعد سخت این کامپوننت:
 *
 * - تنها عملیات، همان درز مجاز ۰۹۶ است و فقط برای روشی که سرور آن را گزینه‌دار حل کرده است.
 *   روش بدون گزینه (`snapp_courier`, `store_courier`, `in_person`) هیچ سوئیچ کاربردی ندارد و فقط
 *   پیام صریح «فعال‌سازی این نوع سرویس هنوز پشتیبانی نمی‌شود» نشان می‌دهد.
 * - هیچ فعال‌سازی خودکار والد و هیچ نوشتن سطح سرویس وجود ندارد.
 * - تأیید صریح پیش از هر عمل؛ اثر عملیات بر واجد شرایط بودن فروشگاه روشن گفته می‌شود.
 * - حالت مشغول، ارسال تکراری را می‌بندد و تا تأیید سرور چیزی «موفق» اعلام نمی‌شود.
 * - هیچ وضعیت خوش‌بینانهٔ محلی وجود ندارد: وضعیت نمایش‌داده‌شده فقط از حقیقت سرور می‌آید و پس از
 *   پاسخ تأییدشده، والد منبع حقیقت را دوباره می‌خواند.
 * - نتیجهٔ در پرواز هرگز روی روش دیگری نمی‌نشیند (نگهبان نسل + کلید هدف).
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button, Dialog } from "../../../design-system";
import {
  ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA,
  activationActionFa,
  activationBindingContextFa,
  activationConfirmFa,
  activationErrorFa,
  activationImpactFa,
  activationStatusFa,
  activationTargetKey,
  activationUnavailableFa,
  createActivationGenerationGuard,
  resolveActivationEligibility,
  setOneLevelMethodActivation,
} from "./shipping-one-level-activation-model.ts";
import type { OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
import { sanitizeMethodCode } from "./shipping-one-level-tabs-model.ts";

export interface ShippingOneLevelActivationControlProps {
  /** روش انتخاب‌شده؛ هویت آن تغییرناپذیر است. */
  methodCode: string;
  /** پیوند حل‌شدهٔ سرور برای همان روش؛ تنها منبع مجاز «گزینه‌دار بودن». */
  bindingState: OneLevelBindingState | null;
  /** آیا پیوند در حال خواندن است؛ تا رسیدن آن هیچ عملیاتی مجاز نیست. */
  bindingLoading: boolean;
  /** بازخورد موفق/خطا/رد مجوز به صفحهٔ والد. */
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /** پس از تأیید سرور: والد باید منبع حقیقت (کاتالوگ و پیوند) را دوباره بخواند. */
  onCompleted: (saved: { methodCode: string; isActive: boolean }) => void;
}

export function ShippingOneLevelActivationControl({
  methodCode,
  bindingState,
  bindingLoading,
  onNotice,
  onCompleted,
}: ShippingOneLevelActivationControlProps) {
  const code = sanitizeMethodCode(methodCode);
  const eligibility = useMemo(
    () => resolveActivationEligibility(bindingState, code),
    [bindingState, code],
  );

  const [confirmOpen, setConfirmOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const guard = useRef(createActivationGenerationGuard());
  const targetKey = activationTargetKey(code);

  // تغییر هدف ⇒ هیچ پاسخ در پروازی اجازهٔ نشستن روی هدف تازه را ندارد و دیالوگ بسته می‌شود.
  const lastTarget = useRef("");
  useEffect(() => {
    if (lastTarget.current === targetKey) return;
    lastTarget.current = targetKey;
    guard.current.begin();
    setConfirmOpen(false);
    setBusy(false);
  }, [targetKey]);

  const currentIsActive = eligibility.state === "supported" ? eligibility.currentIsActive : false;

  const submit = useCallback(async () => {
    if (eligibility.state !== "supported") return;
    const requestedCode = eligibility.methodCode;
    const requestedState = !eligibility.currentIsActive;
    const stillCurrent = guard.current.begin();

    setBusy(true);
    const result = await setOneLevelMethodActivation(requestedCode, requestedState);
    if (!stillCurrent()) return;
    setBusy(false);
    setConfirmOpen(false);

    if (result.state === "denied") {
      onNotice({ tone: "error", text: "دسترسی به تغییر وضعیت فعال‌بودن روش ارسال مجاز نیست." });
      return;
    }
    if (result.state !== "ok" || !result.data) {
      onNotice({ tone: "error", text: activationErrorFa(result.message ?? "") });
      return;
    }
    // سرور ممکن است کد روش را canonical برگرداند؛ هدف فعلی باید دقیقاً همان هدف درخواست باشد.
    if (activationTargetKey(result.data.methodCode) !== activationTargetKey(requestedCode)) return;

    onCompleted({ methodCode: result.data.methodCode, isActive: result.data.isActive });
    onNotice({
      tone: "success",
      text: `وضعیت «${result.data.methodCode}» به ${activationStatusFa(result.data.isActive)} تغییر کرد.`,
    });
  }, [eligibility, onCompleted, onNotice]);

  if (eligibility.state === "unavailable" && eligibility.reason === "no-method") {
    return null;
  }

  return (
    <section
      className="rounded-ds border border-border p-4"
      dir="rtl"
      data-testid="admin-shipping-one-level-activation"
    >
      <h3 className="text-sm font-semibold">فعال‌بودن روش</h3>

      {bindingLoading ? (
        <p className="mt-2 text-sm text-muted" data-testid="admin-shipping-one-level-activation-loading">
          در حال به‌روزرسانی وضعیت از سرور…
        </p>
      ) : null}

      {!bindingLoading && eligibility.state === "unavailable" ? (
        <p
          className="mt-2 text-sm text-muted"
          data-testid="admin-shipping-one-level-activation-unavailable"
        >
          {activationUnavailableFa(eligibility.reason)}
        </p>
      ) : null}

      {!bindingLoading && eligibility.state === "optionless" ? (
        // روش بدون گزینه: هیچ سوئیچی وجود ندارد؛ هرگز فعال‌سازی سطح سرویس جا زده نمی‌شود.
        <p
          className="mt-2 text-sm text-muted"
          data-testid="admin-shipping-one-level-activation-optionless"
        >
          <span dir="ltr" className="font-mono">{eligibility.methodCode}</span>
          {" — "}
          {ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA}
        </p>
      ) : null}

      {!bindingLoading && eligibility.state === "supported"
        ? (
          <div className="mt-2 space-y-2">
            {bindingState?.state === "resolved" ? (
              <p className="text-sm text-muted" data-testid="admin-shipping-one-level-activation-context">
                {activationBindingContextFa(bindingState.binding)}
              </p>
            ) : null}
            <p className="text-sm">
              وضعیت فعلی (از سرور):{" "}
              <span
                data-testid="admin-shipping-one-level-activation-state"
                className={currentIsActive ? "text-emerald-700" : "text-muted"}
              >
                {activationStatusFa(currentIsActive)}
              </span>
            </p>
            <p className="text-xs text-muted">{activationImpactFa(currentIsActive)}</p>
            <Button
              type="button"
              tone="secondary"
              data-testid="admin-shipping-one-level-activation-toggle"
              aria-busy={busy}
              disabled={busy}
              onClick={() => setConfirmOpen(true)}
            >
              {busy ? "در حال اعمال…" : activationActionFa(currentIsActive)}
            </Button>
          </div>
        )
        : null}

      <Dialog
        open={confirmOpen && eligibility.state === "supported"}
        onClose={() => {
          if (busy) return;
          setConfirmOpen(false);
        }}
        title={`${activationActionFa(currentIsActive)} روش ارسال`}
        showCloseButton={false}
      >
        <div className="space-y-4" dir="rtl" data-testid="admin-shipping-one-level-activation-confirm">
          <p className="text-sm text-muted">
            کد روش (تغییرناپذیر): <span dir="ltr" className="font-mono">{code}</span>
          </p>
          <p className="text-sm" data-testid="admin-shipping-one-level-activation-confirm-impact">
            {activationConfirmFa(currentIsActive)}
          </p>
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              tone="secondary"
              disabled={busy}
              onClick={() => setConfirmOpen(false)}
            >
              انصراف
            </Button>
            <Button
              type="button"
              data-testid="admin-shipping-one-level-activation-confirm-submit"
              aria-busy={busy}
              disabled={busy || eligibility.state !== "supported"}
              onClick={() => void submit()}
            >
              {busy ? "در حال اعمال…" : `تأیید ${activationActionFa(currentIsActive)}`}
            </Button>
          </div>
        </div>
      </Dialog>
    </section>
  );
}

