"use client";

import { useMemo, useState } from "react";
import { AlertCircle } from "lucide-react";
import {
  adminImplementedVariants,
  adminSelectableSectionTypes,
  defaultConfigForCompositionSection,
  type AdminCompositionSectionChoice,
} from "./admin-composition-catalog.ts";
import {
  LandingSectionForm,
  type SectionWizardStep,
} from "./admin-landing-section-forms.tsx";
import { validateWizardStep, wizardStepsForHost } from "./admin-section-wizard-logic.ts";
import { getSectionType, getVariant } from "../../../lib/storefront-composition/registry.ts";
import { VariantLivePreview } from "../../../lib/storefront-composition/variant-live-preview.tsx";
import { SIZE_PRESETS } from "../../../lib/storefront-composition/types.ts";
import { SIZE_PRESET_CONTRACTS } from "../../../lib/storefront-composition/size-presets.ts";
import { sectionSupportsHeightPreset } from "./admin-composition-catalog.ts";
import {
  bannerSlotCountForVariant,
  productSourceLabelFa,
  summarizeLandingSection,
  type LandingSectionType,
} from "./landing-section-catalog.ts";
import {
  HERO_HEIGHT_PRESET_LABELS_FA,
  isHeroHeightPreset,
  readHeroSliderFields,
  validateHeroSliderDetailed,
  type HeroSlideFieldKey,
} from "../../../lib/storefront-composition/hero-slider-config.ts";

const STEP_LABELS: Record<SectionWizardStep, string> = {
  type: "نوع بخش",
  variant: "ظاهر",
  source: "منبع محتوا",
  settings: "تنظیمات",
  preview: "بازبینی",
};

export type SectionWizardSavePayload = {
  hostType: LandingSectionType;
  config: Record<string, unknown>;
  sectionTypeKey: string;
  variantKey: string;
};

type Props = {
  open: boolean;
  mode: "create" | "edit";
  initialHostType?: string;
  initialConfig?: Record<string, unknown>;
  busy?: boolean;
  onClose: () => void;
  onSave: (payload: SectionWizardSavePayload) => void | Promise<void>;
};

/**
 * One coherent multi-step wizard for section create/edit.
 * Replaces fragmented modal → drawer workflow.
 */
export function AdminSectionWizard({
  open,
  mode,
  initialHostType,
  initialConfig,
  busy,
  onClose,
  onSave,
}: Props) {
  const compositionSections = useMemo(() => adminSelectableSectionTypes(), []);
  const initialChoice = useMemo(() => {
    if (!initialHostType) return null;
    const variantKey = typeof initialConfig?.variantKey === "string" ? initialConfig.variantKey : undefined;
    if (variantKey) {
      const fromVariant = getVariant(variantKey);
      if (fromVariant) {
        return compositionSections.find((s) => s.sectionTypeKey === fromVariant.sectionTypeKey) ?? null;
      }
    }
    return compositionSections.find((s) => s.hostType === initialHostType) ?? null;
  }, [compositionSections, initialConfig, initialHostType]);

  const [step, setStep] = useState<SectionWizardStep>(mode === "edit" ? "variant" : "type");
  const [choice, setChoice] = useState<AdminCompositionSectionChoice | null>(initialChoice);
  const [variantKey, setVariantKey] = useState<string | null>(
    typeof initialConfig?.variantKey === "string"
      ? initialConfig.variantKey
      : initialChoice?.defaultVariantKey ?? null,
  );
  const [config, setConfig] = useState<Record<string, unknown>>(
    initialConfig
      ?? (initialChoice
        ? defaultConfigForCompositionSection(initialChoice.sectionTypeKey, initialChoice.defaultVariantKey)
        : {}),
  );
  const [error, setError] = useState<string | null>(null);
  const [validationModal, setValidationModal] = useState<string | null>(null);
  const [heroShowErrors, setHeroShowErrors] = useState(false);
  const [heroFocusRequest, setHeroFocusRequest] = useState<{
    slideIndex: number;
    field: HeroSlideFieldKey;
    token: number;
  } | null>(null);
  const [blockedSteps, setBlockedSteps] = useState<Partial<Record<SectionWizardStep, boolean>>>({});

  const steps = useMemo(
    () => wizardStepsForHost(choice?.hostType ?? initialHostType),
    [choice?.hostType, initialHostType],
  );

  const stepIndex = Math.max(0, steps.indexOf(step));
  const heroValidation = useMemo(
    () => (choice?.hostType === "Hero" ? validateHeroSliderDetailed(config) : null),
    [choice?.hostType, config],
  );

  if (!open) return null;

  const applyHeroValidationUx = (summary: string) => {
    setHeroShowErrors(true);
    setValidationModal(summary);
    setError(summary);
    setBlockedSteps((current) => ({ ...current, settings: true, preview: true }));
    const detailed = validateHeroSliderDetailed(config);
    if (detailed.firstInvalidSlideIndex != null && detailed.firstInvalidField) {
      setHeroFocusRequest({
        slideIndex: detailed.firstInvalidSlideIndex,
        field: detailed.firstInvalidField,
        token: Date.now(),
      });
    }
    setStep("settings");
  };

  const clearHeroValidationUxIfResolved = (nextConfig: Record<string, unknown>) => {
    if (!heroShowErrors || choice?.hostType !== "Hero") return;
    const next = validateHeroSliderDetailed(nextConfig);
    if (next.ok) {
      setHeroShowErrors(false);
      setValidationModal(null);
      setError(null);
      setBlockedSteps((current) => ({ ...current, settings: false, preview: false }));
    }
  };

  const goNext = () => {
    const message = validateWizardStep(step, {
      sectionTypeKey: choice?.sectionTypeKey,
      variantKey,
      hostType: choice?.hostType,
      config,
    });
    if (message) {
      if (choice?.hostType === "Hero" && step === "settings") {
        applyHeroValidationUx(message);
        return;
      }
      setError(message);
      setBlockedSteps((current) => ({ ...current, [step]: true }));
      return;
    }
    setError(null);
    setValidationModal(null);
    setBlockedSteps((current) => ({ ...current, [step]: false }));
    const next = steps[stepIndex + 1];
    if (next) setStep(next);
  };

  const goBack = () => {
    setError(null);
    setValidationModal(null);
    const prev = steps[stepIndex - 1];
    if (prev) setStep(prev);
    else onClose();
  };

  const finish = async () => {
    if (!choice || !variantKey) {
      setError("بخش کامل نیست.");
      return;
    }
    for (const s of steps) {
      const message = validateWizardStep(s, {
        sectionTypeKey: choice.sectionTypeKey,
        variantKey,
        hostType: choice.hostType,
        config,
      });
      if (message) {
        if (choice.hostType === "Hero" && (s === "settings" || s === "preview")) {
          applyHeroValidationUx(message);
          return;
        }
        setError(message);
        setBlockedSteps((current) => ({ ...current, [s]: true }));
        setStep(s);
        return;
      }
    }
    setError(null);
    setValidationModal(null);
    const saveConfig: Record<string, unknown> = { ...config, variantKey };
    if (choice.hostType === "Hero") {
      const fields = readHeroSliderFields(saveConfig);
      const firstTitle = fields.slides[0]?.title?.trim() ?? "";
      if (firstTitle && !(typeof saveConfig.title === "string" && saveConfig.title.trim())) {
        saveConfig.title = firstTitle;
      }
      saveConfig.slideCount = fields.slides.length;
      saveConfig.autoplay = true;
    }
    await onSave({
      hostType: choice.hostType,
      sectionTypeKey: choice.sectionTypeKey,
      variantKey,
      config: saveConfig,
    });
  };

  const showHeight = Boolean(
    choice
      && choice.hostType !== "Hero"
      && sectionSupportsHeightPreset(choice.sectionTypeKey, variantKey ?? undefined),
  );

  const heightSummaryFa = (() => {
    if (choice?.hostType === "Hero") {
      const preset = config.heightPreset;
      if (isHeroHeightPreset(preset)) return HERO_HEIGHT_PRESET_LABELS_FA[preset];
      return HERO_HEIGHT_PRESET_LABELS_FA.Medium;
    }
    if (typeof config.heightPreset === "string" && config.heightPreset in SIZE_PRESET_CONTRACTS) {
      return SIZE_PRESET_CONTRACTS[config.heightPreset as keyof typeof SIZE_PRESET_CONTRACTS].nameFa;
    }
    return "پیش‌فرض";
  })();

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4" data-testid="section-wizard">
      <div
        className="flex h-[min(90vh,720px)] w-full max-w-4xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl"
        data-testid="section-wizard-shell"
        data-wizard-shell="fixed"
      >
        <div className="shrink-0 border-b border-border px-5 py-4">
          <div className="flex items-center justify-between gap-3">
            <h3 className="text-lg font-black">{mode === "edit" ? "ویرایش بخش" : "افزودن بخش"}</h3>
            <button type="button" onClick={onClose}>بستن</button>
          </div>
          <ol className="mt-3 flex flex-wrap gap-2" data-testid="section-wizard-steps">
            {steps.map((item, index) => {
              const state = index === stepIndex ? "current" : index < stepIndex ? "completed" : "upcoming";
              const blocked = Boolean(blockedSteps[item]) || (
                heroShowErrors && choice?.hostType === "Hero" && (item === "settings" || item === "preview")
                && Boolean(heroValidation && !heroValidation.ok)
              );
              return (
                <li
                  key={item}
                  className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-xs font-bold ${
                    blocked
                      ? "bg-red-50 text-red-700 ring-1 ring-red-400"
                      : state === "current"
                        ? "bg-[#2563EB] text-white"
                        : state === "completed"
                          ? "bg-emerald-50 text-emerald-700"
                          : "bg-slate-100 text-slate-600"
                  }`}
                  data-wizard-step={item}
                  data-step-state={state}
                  data-step-error={blocked ? "true" : undefined}
                  data-active={state === "current" ? "true" : undefined}
                >
                  {blocked ? <AlertCircle className="h-3.5 w-3.5" aria-hidden /> : null}
                  {(index + 1).toLocaleString("fa-IR")}. {STEP_LABELS[item]}
                </li>
              );
            })}
          </ol>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-4" data-testid="section-wizard-scroll">
          {step === "type" ? (
            <div className="grid gap-3 sm:grid-cols-2" data-testid="composition-section-catalog" role="listbox" aria-label="نوع بخش">
              {compositionSections.map((item) => {
                const selected = choice?.sectionTypeKey === item.sectionTypeKey;
                return (
                  <div
                    key={item.sectionTypeKey}
                    role="option"
                    aria-selected={selected}
                    data-testid={item.testId}
                    data-section-type-key={item.sectionTypeKey}
                    className={`flex h-full flex-col rounded-2xl border p-3 text-start ${
                      selected ? "border-[#2563EB] bg-blue-50 ring-1 ring-[#2563EB]" : "border-border"
                    }`}
                  >
                    <VariantLivePreview variantKey={item.defaultVariantKey} size="card" />
                    <strong className="mt-2 block">{item.nameFa}</strong>
                    <p className="mt-1 text-xs text-muted">{item.descriptionFa}</p>
                    <div className="mt-auto flex w-full justify-end pt-3">
                      <button
                        type="button"
                        data-select-choice="1"
                        aria-pressed={selected}
                        className={`rounded-xl px-3 py-1.5 text-xs font-bold focus-visible:ring-2 focus-visible:ring-[#2563EB] ${
                          selected
                            ? "bg-[#2563EB] text-white"
                            : "border border-border bg-white text-slate-800 hover:border-[#2563EB] hover:text-[#2563EB]"
                        }`}
                        onClick={() => {
                          setChoice(item);
                          setVariantKey(item.defaultVariantKey);
                          setConfig(defaultConfigForCompositionSection(item.sectionTypeKey, item.defaultVariantKey));
                          setError(null);
                          setHeroShowErrors(false);
                          setBlockedSteps({});
                        }}
                      >
                        {selected ? "انتخاب‌شده" : "انتخاب"}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : null}

          {step === "variant" && choice ? (
            <div
              className="grid gap-3 sm:grid-cols-2"
              data-testid="composition-variant-picker"
              data-variant-picker-v2="1"
              role="listbox"
              aria-label="ظاهر بخش"
            >
              {adminImplementedVariants(choice.sectionTypeKey).map((variant) => {
                const selected = variantKey === variant.variantKey;
                return (
                  <div
                    key={variant.variantKey}
                    role="option"
                    aria-selected={selected}
                    data-variant-key={variant.variantKey}
                    data-preview-fingerprint={variant.variantKey}
                    data-testid={`pick-variant-${variant.variantKey.replace(/\./g, "-")}`}
                    className={`flex h-full flex-col rounded-2xl border p-3 text-start ${
                      selected ? "border-[#2563EB] bg-blue-50 ring-1 ring-[#2563EB]" : "border-border"
                    }`}
                  >
                    <VariantLivePreview
                      variantKey={variant.variantKey}
                      size="card"
                      testId={`variant-live-preview-${variant.variantKey.replace(/\./g, "-")}`}
                    />
                    <strong className="mt-2 block text-sm" data-variant-design-name={variant.variantKey}>
                      {variant.nameFa}
                    </strong>
                    <p className="mt-1 text-xs text-muted">{variant.descriptionFa}</p>
                    {variant.recommendedUseFa ? (
                      <span className="mt-2 inline-flex rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-bold text-emerald-700">
                        {variant.recommendedUseFa}
                      </span>
                    ) : null}
                    <div className="mt-auto flex w-full justify-end pt-3">
                      <button
                        type="button"
                        data-select-choice="1"
                        aria-pressed={selected}
                        title={variant.descriptionFa}
                        className={`rounded-xl px-3 py-1.5 text-xs font-bold focus-visible:ring-2 focus-visible:ring-[#2563EB] ${
                          selected
                            ? "bg-[#2563EB] text-white"
                            : "border border-border bg-white text-slate-800 hover:border-[#2563EB] hover:text-[#2563EB]"
                        }`}
                        onClick={() => {
                          setVariantKey(variant.variantKey);
                          const next = defaultConfigForCompositionSection(choice.sectionTypeKey, variant.variantKey);
                          if (variant.variantKey.startsWith("banner.")) {
                            const slots = bannerSlotCountForVariant(variant.variantKey);
                            next.items = Array.from({ length: slots }, (_, index) => ({
                              imageUrl: "",
                              href: "/offers",
                              title: `بنر ${(index + 1).toLocaleString("fa-IR")}`,
                            }));
                          }
                          if (choice.hostType === "StoryRail") {
                            next.items = [];
                            next.take = typeof config.take === "number" ? config.take : 12;
                          }
                          setConfig({ ...config, ...next, title: typeof config.title === "string" ? config.title : next.title });
                          setError(null);
                        }}
                      >
                        {selected ? "انتخاب‌شده" : "انتخاب"}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : null}

          {step === "source" && choice ? (
            <div data-testid="section-wizard-source">
              <LandingSectionForm type={choice.hostType} value={config} onChange={setConfig} mode="source" />
            </div>
          ) : null}

          {step === "settings" && choice ? (
            <div className="space-y-4" data-testid="section-wizard-settings">
              {mode === "edit" ? (
                <div data-testid="edit-variant-picker" className="rounded-xl border border-border p-3 text-sm text-muted">
                  ظاهر: <strong className="text-foreground">{getVariant(variantKey ?? "")?.nameFa ?? "—"}</strong>
                </div>
              ) : null}
              {showHeight ? (
                <label className="block text-sm">
                  <span className="mb-1 block font-bold">اندازه نمایش</span>
                  <select
                    className="w-full rounded-xl border px-3 py-2"
                    value={typeof config.heightPreset === "string" ? config.heightPreset : "Medium"}
                    onChange={(e) => setConfig({ ...config, heightPreset: e.target.value })}
                    data-testid="height-preset-select"
                  >
                    {SIZE_PRESETS.map((preset) => (
                      <option key={preset} value={preset}>{SIZE_PRESET_CONTRACTS[preset].nameFa}</option>
                    ))}
                  </select>
                </label>
              ) : null}
              <LandingSectionForm
                type={choice.hostType}
                value={config}
                onChange={(next) => {
                  setConfig(next);
                  clearHeroValidationUxIfResolved(next);
                }}
                mode="settings"
                heroShowErrors={heroShowErrors}
                heroFocusRequest={heroFocusRequest}
                heroVariantKey={variantKey}
              />
            </div>
          ) : null}

          {step === "preview" && choice ? (
            <div className="space-y-4" data-testid="section-wizard-preview" data-review-variant-aware="1" data-review-live-preview="1">
              <div className="rounded-2xl border border-border bg-slate-50 p-3">
                <p className="mb-2 text-xs font-bold text-amber-800" data-review-preview-notice="1">
                  پیش‌نمایش با محتوای نمونه (غیرواقعی)
                </p>
                <VariantLivePreview
                  variantKey={variantKey ?? choice.defaultVariantKey}
                  eager
                  size="review"
                  testId={`review-preview-${(variantKey ?? choice.defaultVariantKey).replace(/\./g, "-")}`}
                />
              </div>
              <dl className="grid gap-2 text-sm" data-testid="review-plain-summary">
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">نوع</dt>
                  <dd className="font-bold">{getSectionType(choice.sectionTypeKey)?.nameFa}</dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">ظاهر</dt>
                  <dd className="font-bold" data-review-design-name="1">{getVariant(variantKey ?? "")?.nameFa}</dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">نوع منبع</dt>
                  <dd className="font-bold text-end" data-testid="review-source-label">
                    {choice.hostType === "ProductCollection"
                      ? productSourceLabelFa(config.source)
                      : summarizeLandingSection(choice.hostType, config)}
                  </dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">تعداد</dt>
                  <dd className="font-bold">
                    {typeof config.take === "number"
                      ? config.take.toLocaleString("fa-IR")
                      : typeof config.slideCount === "number"
                        ? config.slideCount.toLocaleString("fa-IR")
                      : Array.isArray(config.items)
                        ? config.items.length.toLocaleString("fa-IR")
                        : Array.isArray(config.productIds)
                          ? config.productIds.length.toLocaleString("fa-IR")
                          : Array.isArray(config.brandIds)
                            ? config.brandIds.length.toLocaleString("fa-IR")
                            : Array.isArray(config.articleIds)
                              ? config.articleIds.length.toLocaleString("fa-IR")
                              : "—"}
                  </dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">اندازه</dt>
                  <dd className="font-bold">{heightSummaryFa}</dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">فعال</dt>
                  <dd className="font-bold">{config.enabled === false ? "خیر" : "بله"}</dd>
                </div>
              </dl>
            </div>
          ) : null}

          {error && !validationModal ? <p className="mt-3 text-sm text-red-600" role="alert">{error}</p> : null}
        </div>

        <div
          className="sticky bottom-0 flex shrink-0 flex-wrap items-center justify-between gap-2 border-t border-border bg-white px-5 py-3"
          data-testid="section-wizard-footer"
        >
          <button type="button" className="rounded-xl border px-4 py-2 text-sm font-bold" onClick={goBack} data-testid="section-wizard-back">
            {stepIndex === 0 ? "انصراف" : "قبلی"}
          </button>
          {step === "preview" ? (
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white disabled:opacity-50"
              disabled={busy}
              onClick={() => void finish()}
              data-testid="section-wizard-save"
            >
              ذخیره بخش
            </button>
          ) : (
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              onClick={goNext}
              data-testid="section-wizard-next"
            >
              بعدی
            </button>
          )}
        </div>
      </div>

      {validationModal ? (
        <div
          className="fixed inset-0 z-[80] flex items-center justify-center bg-black/50 p-4"
          role="alertdialog"
          aria-modal="true"
          aria-labelledby="hero-validation-title"
          data-testid="hero-validation-modal"
        >
          <div className="w-full max-w-md rounded-2xl bg-white p-5 shadow-2xl">
            <h4 id="hero-validation-title" className="text-base font-black text-slate-900">
              تکمیل اطلاعات لازم است
            </h4>
            <p className="mt-3 text-sm leading-7 text-slate-700">{validationModal}</p>
            <div className="mt-5 flex justify-end">
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => setValidationModal(null)}
                data-testid="hero-validation-modal-dismiss"
              >
                متوجه شدم
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
