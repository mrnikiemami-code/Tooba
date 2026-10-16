"use client";

/**
 * ویرایش چندزبانهٔ نام یک روش ارسال — فشرده، RTL و مبتنی بر زبان‌های واقعی سرور.
 *
 * TB-SHIPPING-ADMIN-MULTILINGUAL-UX-148
 *
 * قواعد سخت:
 *
 * - فهرست زبان‌ها فقط از رجیستری authoritative سرور می‌آید (`loadAdminLanguages` از همان درز مشترک
 *   `features/admin-languages`). هیچ `fa`/`en`/`ru` و هیچ ترجمهٔ ساختگی در این فایل وجود ندارد.
 * - فقط نام **همین روش در همان زبان** نوشته می‌شود؛ کد روش تغییر نمی‌کند و هیچ سیاست دیگری نوشته
 *   نمی‌شود. نبود زبان/پیوند ⇒ هیچ نوشتنی رخ نمی‌دهد.
 * - پیش‌نویس هر زبان جدا نگه داشته می‌شود، پس جابه‌جایی زبان پیش از ذخیره هیچ متنی را از دست نمی‌دهد
 *   و زبان‌های دارای پیش‌نویس ذخیره‌نشده صریح اعلام می‌شوند.
 * - موفقیت فقط پس از پاسخ تأییدشدهٔ سرور برای همان روش و همان زبان اعلام می‌شود؛ پاسخ متعلق به هدف
 *   دیگر روی هدف فعلی نمی‌نشیند.
 */

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button, Field, Input, Select } from "../../../design-system";
import { saveOneLevelMethodName } from "./shipping-one-level-method-name-api.ts";
import {
  dirtyMethodLanguages,
  methodLanguageDefinitions,
  methodNameDraftFor,
  methodNameDraftIsDirty,
  methodNameDrafts,
  methodTranslationStates,
  serverMethodNameFor,
  setMethodNameDraft,
  type MethodLanguageSource,
} from "./shipping-method-language-model.ts";
import {
  ONE_LEVEL_METHOD_NAME_MAX_LENGTH,
  createMethodNameGenerationGuard,
  oneLevelMethodNameIssueFa,
  methodNameTargetKey,
  resolveEffectiveLanguageId,
  validateOneLevelMethodName,
} from "./shipping-one-level-method-name-model.ts";
import { sanitizeLanguageId } from "./shipping-one-level-tabs-model.ts";

export type MethodLanguagesState = "idle" | "loading" | "ok" | "denied" | "error";

export function FlatMethodLanguageEditor({
  methodCode,
  translations,
  languages,
  languagesState,
  defaultLanguageId,
  readOnly,
  onRetryLanguages,
  onNotice,
  onSaved,
}: {
  methodCode: string;
  translations: readonly { languageId: string; name: string }[];
  languages: readonly MethodLanguageSource[];
  languagesState: MethodLanguagesState;
  defaultLanguageId: string;
  readOnly: boolean;
  onRetryLanguages: () => void;
  onNotice: (notice: { tone: "success" | "error"; text: string }) => void;
  onSaved: (text: string) => Promise<void>;
}) {
  const definitions = useMemo(() => methodLanguageDefinitions(languages), [languages]);
  const states = useMemo(
    () => methodTranslationStates(translations, definitions, defaultLanguageId),
    [defaultLanguageId, definitions, translations],
  );
  const translatedCount = states.filter((row) => row.translated).length;

  const [selectedLanguageId, setSelectedLanguageId] = useState("");
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [issue, setIssue] = useState<"required" | "too_long" | null>(null);
  const [saving, setSaving] = useState(false);
  const guard = useRef(createMethodNameGenerationGuard());
  // پیش‌نویس‌ها فقط با تغییر روش بازنشانی می‌شوند تا ویرایش ذخیره‌نشدهٔ زبان‌های دیگر از دست نرود.
  const draftsOwner = useRef<string>("");

  useEffect(() => {
    if (definitions.length === 0) return;
    if (draftsOwner.current === methodCode) return;
    draftsOwner.current = methodCode;
    setDrafts(methodNameDrafts(translations, definitions));
  }, [definitions, methodCode, translations]);

  const effectiveLanguageId = useMemo(
    () => resolveEffectiveLanguageId(definitions, defaultLanguageId, selectedLanguageId),
    [defaultLanguageId, definitions, selectedLanguageId],
  );
  useEffect(() => {
    if (effectiveLanguageId !== selectedLanguageId) setSelectedLanguageId(effectiveLanguageId);
  }, [effectiveLanguageId, selectedLanguageId]);

  const serverName = serverMethodNameFor(translations, effectiveLanguageId);
  const draft = methodNameDraftFor(drafts, effectiveLanguageId);
  const dirty = methodNameDraftIsDirty(draft, serverName);
  const unsaved = useMemo(
    () => dirtyMethodLanguages(drafts, translations, definitions),
    [definitions, drafts, translations],
  );

  const changeDraft = useCallback((value: string) => {
    setDrafts((current) => setMethodNameDraft(current, effectiveLanguageId, value));
    setIssue(null);
  }, [effectiveLanguageId]);

  const save = useCallback(async () => {
    const validation = validateOneLevelMethodName(draft);
    setIssue(validation);
    if (validation !== null || !effectiveLanguageId) return;

    const requestedCode = methodCode;
    const requestedLanguage = effectiveLanguageId;
    const requestedName = draft.trim();
    const requestedKey = methodNameTargetKey(requestedCode, requestedLanguage);
    // نسل تازه: پاسخ در پرواز اجازهٔ نشستن روی هدف دیگری را ندارد.
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
    // سرور ممکن است کد/زبان را canonical برگرداند؛ نتیجه فقط برای همان هدف درخواست معتبر است.
    const saved = result.data;
    if (methodNameTargetKey(saved.methodCode, saved.languageId) !== requestedKey) return;

    setDrafts((current) => setMethodNameDraft(current, requestedLanguage, saved.name));
    await onSaved(`نام روش در زبان انتخاب‌شده به «${saved.name}» ذخیره و دوباره خوانده شد.`);
  }, [draft, effectiveLanguageId, methodCode, onNotice, onSaved]);

  const selected = states.find((row) => row.languageId === effectiveLanguageId);
  const canEdit = !readOnly && languagesState === "ok" && states.length > 0;
  const issueText = oneLevelMethodNameIssueFa(issue);

  return (
    <section className="space-y-2 rounded-xl border border-gray-200 p-3" data-testid="admin-shipping-method-languages">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="text-sm font-bold text-gray-900">نام نمایشی روش در زبان‌ها</h2>
        {languagesState === "ok" && states.length > 0 ? (
          <span className="text-[11px] text-gray-500" data-testid="admin-shipping-method-language-summary">
            ترجمه‌شده: {translatedCount} از {states.length}
          </span>
        ) : null}
      </div>

      {languagesState === "loading" || languagesState === "idle" ? (
        <p className="text-xs text-gray-500" data-testid="admin-shipping-method-languages-loading">
          در حال خواندن زبان‌های ثبت‌شدهٔ سرور…
        </p>
      ) : languagesState === "denied" ? (
        <p className="text-xs text-amber-800" role="alert" data-testid="admin-shipping-method-languages-denied">
          دسترسی به فهرست زبان‌ها مجاز نیست؛ ویرایش نام انجام نمی‌شود.
        </p>
      ) : languagesState === "error" ? (
        <div className="flex flex-wrap items-center gap-2">
          <p className="text-xs text-amber-800" role="alert" data-testid="admin-shipping-method-languages-error">
            خواندن فهرست زبان‌ها ناموفق بود؛ ویرایش نام انجام نمی‌شود.
          </p>
          <Button type="button" tone="secondary" onClick={onRetryLanguages}>
            تلاش دوباره
          </Button>
        </div>
      ) : states.length === 0 ? (
        <p className="text-xs text-amber-800" data-testid="admin-shipping-method-languages-empty">
          سرور هیچ زبان دارای شناسهٔ قابل‌ویرایش اعلام نکرده است؛ زبان جعلی ساخته نمی‌شود.
        </p>
      ) : (
        <>
          <ul className="grid gap-1 sm:grid-cols-2" data-testid="admin-shipping-method-language-list">
            {states.map((row) => {
              const rowDirty = unsaved.includes(row.languageId);
              const isCurrent = row.languageId === effectiveLanguageId;
              return (
                <li key={row.languageId}>
                  <button
                    type="button"
                    data-testid={`admin-shipping-method-language-option-${row.languageId}`}
                    aria-pressed={isCurrent}
                    className={
                      isCurrent
                        ? "flex w-full items-center justify-between gap-2 rounded-lg border border-[#2563EB] bg-blue-50 px-2 py-1 text-start text-xs"
                        : "flex w-full items-center justify-between gap-2 rounded-lg border border-gray-200 px-2 py-1 text-start text-xs"
                    }
                    onClick={() => setSelectedLanguageId(row.languageId)}
                  >
                    <span className="truncate font-semibold text-gray-800">
                      {row.label}
                      {row.isListLanguage ? <span className="mr-1 text-[10px] text-gray-500">(زبان فهرست)</span> : null}
                    </span>
                    <span className={row.translated ? "text-emerald-700" : "text-amber-700"}>
                      {rowDirty ? "پیش‌نویس ذخیره‌نشده" : row.translated ? "ثبت‌شده" : "بدون ترجمه"}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>

          <Field
            id="admin-shipping-method-language"
            label="زبان"
            hint="فقط زبان‌های ثبت‌شدهٔ سرور؛ هر زبان مستقل ذخیره می‌شود و زبان‌های دیگر تغییر نمی‌کنند."
          >
            <Select
              data-testid="admin-shipping-method-language-select"
              value={effectiveLanguageId}
              disabled={saving}
              onChange={(event) => setSelectedLanguageId(sanitizeLanguageId(event.target.value))}
            >
              {states.map((row) => (
                <option key={row.languageId} value={row.languageId}>
                  {row.label}
                  {row.translated ? "" : " — بدون ترجمه"}
                </option>
              ))}
            </Select>
          </Field>

          <Field
            id="admin-shipping-method-name"
            label={`نام این روش در «${selected?.label ?? ""}»`}
            hint={`فقط نام همین روش در همین زبان نوشته می‌شود؛ کد روش تغییر نمی‌کند. حداکثر ${ONE_LEVEL_METHOD_NAME_MAX_LENGTH} نویسه.`}
            error={issueText || undefined}
          >
            <Input
              data-testid="admin-shipping-method-name-input"
              dir={selected?.direction === "ltr" ? "ltr" : "rtl"}
              value={draft}
              readOnly={readOnly}
              disabled={saving}
              invalid={issue !== null}
              placeholder={readOnly ? "" : "نام نمایشی این روش در این زبان"}
              onChange={(event) => changeDraft(event.target.value)}
            />
          </Field>

          <p className="text-[11px] text-gray-500" data-testid="admin-shipping-method-name-state">
            {serverName.length > 0
              ? `مقدار ثبت‌شدهٔ سرور: ${serverName}`
              : "در این زبان هیچ نامی ثبت نشده است؛ این «خالی» است و نامی ساخته نمی‌شود."}
          </p>

          {readOnly ? (
            <p className="text-xs text-gray-500" data-testid="admin-shipping-method-name-readonly">
              برای ویرایش، روش را در حالت «ویرایش» باز کنید.
            </p>
          ) : (
            <div className="flex flex-wrap items-center justify-end gap-2">
              {unsaved.length > 0 ? (
                <span className="text-[11px] font-semibold text-amber-700" data-testid="admin-shipping-method-name-unsaved">
                  پیش‌نویس ذخیره‌نشده در {unsaved.length} زبان
                </span>
              ) : null}
              <Button
                type="button"
                tone="secondary"
                disabled={saving || !dirty}
                onClick={() => changeDraft(serverName)}
              >
                بازگردانی
              </Button>
              <Button
                type="button"
                data-testid="admin-shipping-method-name-save"
                disabled={saving || !canEdit || !dirty}
                onClick={() => void save()}
              >
                {saving ? "در حال ذخیره…" : "ذخیرهٔ نام این زبان"}
              </Button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
