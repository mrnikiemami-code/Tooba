"use client";

import { useMemo, useState } from "react";
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
import { VariantPreviewCanvas } from "./layout-aware-previews.tsx";
import { getSectionType, getVariant } from "../../../lib/storefront-composition/registry.ts";
import { SIZE_PRESETS } from "../../../lib/storefront-composition/types.ts";
import { SIZE_PRESET_CONTRACTS } from "../../../lib/storefront-composition/size-presets.ts";
import { sectionSupportsHeightPreset } from "./admin-composition-catalog.ts";
import { summarizeLandingSection, type LandingSectionType } from "./landing-section-catalog.ts";
import { bannerSlotCountForVariant } from "./landing-section-catalog.ts";

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

  const steps = useMemo(
    () => wizardStepsForHost(choice?.hostType ?? initialHostType),
    [choice?.hostType, initialHostType],
  );

  const stepIndex = Math.max(0, steps.indexOf(step));

  if (!open) return null;

  const goNext = () => {
    const message = validateWizardStep(step, {
      sectionTypeKey: choice?.sectionTypeKey,
      variantKey,
      hostType: choice?.hostType,
      config,
    });
    if (message) {
      setError(message);
      return;
    }
    setError(null);
    const next = steps[stepIndex + 1];
    if (next) setStep(next);
  };

  const goBack = () => {
    setError(null);
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
        setError(message);
        setStep(s);
        return;
      }
    }
    setError(null);
    await onSave({
      hostType: choice.hostType,
      sectionTypeKey: choice.sectionTypeKey,
      variantKey,
      config: { ...config, variantKey },
    });
  };

  const showHeight = Boolean(
    choice && sectionSupportsHeightPreset(choice.sectionTypeKey, variantKey ?? undefined),
  );

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4" data-testid="section-wizard">
      <div className="flex max-h-[90vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="border-b border-border px-5 py-4">
          <div className="flex items-center justify-between gap-3">
            <h3 className="text-lg font-black">{mode === "edit" ? "ویرایش بخش" : "افزودن بخش"}</h3>
            <button type="button" onClick={onClose}>بستن</button>
          </div>
          <ol className="mt-3 flex flex-wrap gap-2" data-testid="section-wizard-steps">
            {steps.map((item, index) => (
              <li
                key={item}
                className={`rounded-full px-3 py-1 text-xs font-bold ${
                  index === stepIndex
                    ? "bg-[#2563EB] text-white"
                    : index < stepIndex
                      ? "bg-emerald-50 text-emerald-700"
                      : "bg-slate-100 text-slate-600"
                }`}
                data-wizard-step={item}
                data-active={index === stepIndex ? "true" : undefined}
              >
                {(index + 1).toLocaleString("fa-IR")}. {STEP_LABELS[item]}
              </li>
            ))}
          </ol>
        </div>

        <div className="min-h-0 flex-1 overflow-auto px-5 py-4">
          {step === "type" ? (
            <div className="grid gap-3 sm:grid-cols-2" data-testid="composition-section-catalog">
              {compositionSections.map((item) => (
                <button
                  key={item.sectionTypeKey}
                  type="button"
                  data-testid={item.testId}
                  data-section-type-key={item.sectionTypeKey}
                  className={`rounded-2xl border p-4 text-start hover:border-[#2563EB] ${
                    choice?.sectionTypeKey === item.sectionTypeKey ? "border-[#2563EB] bg-blue-50" : ""
                  }`}
                  onClick={() => {
                    setChoice(item);
                    setVariantKey(item.defaultVariantKey);
                    setConfig(defaultConfigForCompositionSection(item.sectionTypeKey, item.defaultVariantKey));
                    setError(null);
                  }}
                >
                  <VariantPreviewCanvas variantKey={item.defaultVariantKey} />
                  <strong>{item.nameFa}</strong>
                  <p className="mt-1 text-xs text-muted">{item.descriptionFa}</p>
                </button>
              ))}
            </div>
          ) : null}

          {step === "variant" && choice ? (
            <div className="grid gap-3 sm:grid-cols-2" data-testid="composition-variant-picker">
              {adminImplementedVariants(choice.sectionTypeKey).map((variant) => (
                <button
                  key={variant.variantKey}
                  type="button"
                  data-variant-key={variant.variantKey}
                  data-preview-fingerprint={variant.variantKey}
                  data-testid={`pick-variant-${variant.variantKey.replace(/\./g, "-")}`}
                  className={`rounded-2xl border p-4 text-start hover:border-[#2563EB] ${
                    variantKey === variant.variantKey ? "border-[#2563EB] bg-blue-50" : ""
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
                  <VariantPreviewCanvas variantKey={variant.variantKey} />
                  <strong>{variant.nameFa}</strong>
                  <p className="mt-1 text-xs text-muted">{variant.descriptionFa}</p>
                  {variant.recommendedUseFa ? (
                    <span className="mt-2 inline-flex rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-bold text-emerald-700">
                      {variant.recommendedUseFa}
                    </span>
                  ) : null}
                </button>
              ))}
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
              <LandingSectionForm type={choice.hostType} value={config} onChange={setConfig} mode="settings" />
            </div>
          ) : null}

          {step === "preview" && choice ? (
            <div className="space-y-4" data-testid="section-wizard-preview">
              <VariantPreviewCanvas variantKey={variantKey ?? choice.defaultVariantKey} />
              <dl className="grid gap-2 text-sm">
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">نوع</dt>
                  <dd className="font-bold">{getSectionType(choice.sectionTypeKey)?.nameFa}</dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">ظاهر</dt>
                  <dd className="font-bold">{getVariant(variantKey ?? "")?.nameFa}</dd>
                </div>
                <div className="flex justify-between gap-3 border-b border-dashed py-2">
                  <dt className="text-muted">خلاصه</dt>
                  <dd className="font-bold text-end">{summarizeLandingSection(choice.hostType, config)}</dd>
                </div>
              </dl>
            </div>
          ) : null}

          {error ? <p className="mt-3 text-sm text-red-600" role="alert">{error}</p> : null}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border px-5 py-3">
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
    </div>
  );
}
