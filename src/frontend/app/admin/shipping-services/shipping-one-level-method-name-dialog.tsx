"use client";

/**
 * دیالوگ ویرایش «نام نمایشی» یک روش یک‌سطحی در یک زبان.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-094
 *
 * قواعد سخت این کامپوننت:
 * - دامنه فقط «نام» است: کد روش در URL و به‌صورت فقط-خواندنی نمایش داده می‌شود و هرگز تغییر نمی‌کند.
 * - هیچ سطح دوم (provider → option) و هیچ گروه‌بندی ارائه‌دهنده‌ای وجود ندارد.
 * - فعال/غیرفعال بودن روش نه نمایش داده می‌شود و نه نوشته می‌شود.
 * - «ذخیره» فقط پس از پاسخ تأییدشدهٔ سرور موفق اعلام می‌شود؛ هیچ موفقیت جعلی ساخته نمی‌شود.
 * - نتیجهٔ ذخیرهٔ یک هدف هرگز روی هدف دیگر (روش/زبان عوض‌شده) نمی‌نشیند.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button, Dialog, Field, Input, Select } from "../../../design-system";
import { saveOneLevelMethodName } from "./shipping-one-level-method-name-api.ts";
import {
  createMethodNameGenerationGuard,
  initialMethodName,
  methodNameTargetKey,
  oneLevelMethodNameIssueFa,
  resolveEffectiveLanguageId,
  validateOneLevelMethodName,
} from "./shipping-one-level-method-name-model.ts";
import { sanitizeLanguageId, sanitizeMethodCode, type OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

/** یک گزینهٔ زبان برای انتخاب؛ شناسهٔ واقعی رجیستری زبان است. */
export type OneLevelLanguageOption = {
  languageId: string;
  label: string;
};

type Props = {
  open: boolean;
  /** روش انتخاب‌شده؛ هویت آن تغییرناپذیر است. */
  methodCode: string;
  /** ردیف واقعی همان روش از فهرست سرور، برای پیش‌پر کردن نام فعلی. */
  method: OneLevelMethodRow | undefined;
  /** زبان‌های قابل انتخاب؛ اولین زبان پیش‌فرض فرم است. */
  languages: readonly OneLevelLanguageOption[];
  /** زبان فعال صفحه که به‌عنوان پیش‌فرض انتخاب می‌شود. */
  defaultLanguageId: string;
  onClose: () => void;
  /** بازخورد موفق/خطا/رد مجوز به صفحهٔ والد. */
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  /** پس از ذخیرهٔ موفق: والد باید منبع حقیقت را دوباره بخواند/به‌روز کند. */
  onSaved: (saved: { methodCode: string; languageId: string; name: string }) => void;
};

export function ShippingOneLevelMethodNameDialog({
  open,
  methodCode,
  method,
  languages,
  defaultLanguageId,
  onClose,
  onNotice,
  onSaved,
}: Props) {
  const code = sanitizeMethodCode(methodCode);
  const languageOptions = useMemo(
    () => languages.filter((row) => sanitizeLanguageId(row.languageId)),
    [languages],
  );
  const fallbackLanguage = useMemo(
    () => resolveEffectiveLanguageId(languageOptions, defaultLanguageId, ""),
    [defaultLanguageId, languageOptions],
  );

  const [languageId, setLanguageId] = useState(fallbackLanguage);
  const [name, setName] = useState("");
  const [issue, setIssue] = useState<"required" | "too_long" | null>(null);
  const [saving, setSaving] = useState(false);
  const guard = useRef(createMethodNameGenerationGuard());

  /**
   * فهرست زبان‌ها نامتقارن بارگذاری می‌شود: دیالوگ ممکن است پیش از رسیدن زبان‌ها mount شود.
   * اگر زبان انتخاب‌شده در فهرست فعلی نباشد، به زبان پیش‌فرض مؤثر می‌رسیم تا فرم هرگز روی زبان
   * نامعتبر/خالی گیر نکند و «ذخیره» بی‌دلیل غیرفعال نماند. هیچ زبان جعلی ساخته نمی‌شود.
   */
  const effectiveLanguageId = useMemo(
    () => resolveEffectiveLanguageId(languageOptions, defaultLanguageId, languageId),
    [defaultLanguageId, languageId, languageOptions],
  );

  useEffect(() => {
    if (effectiveLanguageId !== languageId) setLanguageId(effectiveLanguageId);
  }, [effectiveLanguageId, languageId]);

  // هدف فعلی (روش + زبان). هر تغییر هدف، نسل را جلو می‌برد و فرم را از دادهٔ واقعی پیش‌پر می‌کند.
  const targetKey = methodNameTargetKey(code, effectiveLanguageId);
  const lastTarget = useRef("");

  useEffect(() => {
    if (!open) return;
    if (lastTarget.current === targetKey) return;
    lastTarget.current = targetKey;
    // تغییر هدف ⇒ هیچ نتیجهٔ در پرواز اجازهٔ نشستن روی هدف تازه را ندارد.
    guard.current.begin();
    setName(initialMethodName(method, effectiveLanguageId));
    setIssue(null);
    setSaving(false);
  }, [effectiveLanguageId, method, open, targetKey]);

  // بستن دیالوگ، هدف بعدی را تازه می‌کند تا باز شدن دوباره از دادهٔ واقعی پیش‌پر شود.
  useEffect(() => {
    if (!open) lastTarget.current = "";
  }, [open]);

  const submit = useCallback(async () => {
    const validation = validateOneLevelMethodName(name);
    setIssue(validation);
    if (validation !== null) return;

    const requestedKey = targetKey;
    const requestedCode = code;
    const requestedLanguage = effectiveLanguageId;
    const requestedName = name.trim();
    const stillCurrent = guard.current.begin();

    setSaving(true);
    const result = await saveOneLevelMethodName(requestedCode, requestedLanguage, requestedName);
    if (!stillCurrent()) return;
    setSaving(false);

    if (result.state === "denied") {
      onNotice({ tone: "error", text: "دسترسی به ویرایش نام روش ارسال مجاز نیست." });
      return;
    }
    if (result.state !== "ok" || !result.data) {
      onNotice({ tone: "error", text: "ذخیرهٔ نام روش ارسال ناموفق بود." });
      return;
    }
    // سرور ممکن است کد/زبان را canonical برگرداند؛ هدف فعلی باید دقیقاً همان هدف درخواست باشد.
    if (methodNameTargetKey(result.data.methodCode, result.data.languageId) !== requestedKey) return;

    onSaved({
      methodCode: sanitizeMethodCode(result.data.methodCode),
      languageId: sanitizeLanguageId(result.data.languageId),
      name: result.data.name,
    });
    onNotice({ tone: "success", text: `نام روش «${result.data.name}» ذخیره شد.` });
    onClose();
  }, [code, effectiveLanguageId, name, onClose, onNotice, onSaved, targetKey]);

  const issueText = oneLevelMethodNameIssueFa(issue);

  return (
    <Dialog open={open} onClose={onClose} title="ویرایش نام روش ارسال" showCloseButton={false}>
      <div className="space-y-4" dir="rtl" data-testid="admin-shipping-one-level-name-dialog">
        <p className="text-sm text-muted">
          کد روش (تغییرناپذیر): <span dir="ltr" className="font-mono">{code}</span>
        </p>

        <Field id="one-level-method-name-language" label="زبان">
          <Select
            data-testid="admin-shipping-one-level-name-language"
            value={effectiveLanguageId}
            disabled={saving}
            onChange={(event) => setLanguageId(sanitizeLanguageId(event.target.value))}
          >
            {languageOptions.map((row) => (
              <option key={row.languageId} value={row.languageId}>
                {row.label}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          id="one-level-method-name"
          label="نام نمایشی روش"
          hint="این نام مستقل از کد روش است و در فهرست روش‌ها نمایش داده می‌شود."
        >
          <Input
            data-testid="admin-shipping-one-level-name-input"
            value={name}
            disabled={saving}
            invalid={issue !== null}
            onChange={(event) => {
              setName(event.target.value);
              setIssue(null);
            }}
          />
        </Field>

        {issueText ? (
          <p className="text-sm text-danger" role="alert" data-testid="admin-shipping-one-level-name-issue">
            {issueText}
          </p>
        ) : null}

        <div className="flex justify-end gap-2">
          <Button type="button" tone="secondary" onClick={onClose} disabled={saving}>
            انصراف
          </Button>
          <Button
            type="button"
            data-testid="admin-shipping-one-level-name-save"
            disabled={saving || !effectiveLanguageId || !code}
            onClick={() => void submit()}
          >
            {saving ? "در حال ذخیره…" : "ذخیره"}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
