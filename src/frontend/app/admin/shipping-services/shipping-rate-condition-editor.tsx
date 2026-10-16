"use client";

/**
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146 — ویرایشگر فشردهٔ «شرایط مرسوله».
 *
 * این بخش از `shipping-one-level-rates-tab.tsx` جدا شد تا آن فایل زیر سقف اندازهٔ معماری بماند و هیچ
 * رفتاری تغییر نکند: همان معناشناسی نیم‌باز `[min, max)`، همان کران‌های بک‌اند، همان برچسب‌ها، همان
 * اعتبارسنجی و همان `data-testid`ها که تست‌های موجود انتظار دارند.
 */

import {
  conditionFormToWrite,
  describeConditionFa,
  type ShippingRateConditionForm,
} from "./shipping-rate-tier-model.ts";

type ConditionEditorProps = {
  form: ShippingRateConditionForm;
  onChange: (next: ShippingRateConditionForm) => void;
  disabled: boolean;
  testIdPrefix: string;
};

export function ConditionEditor({ form, onChange, disabled, testIdPrefix }: ConditionEditorProps) {
  const preview = conditionFormToWrite(form);
  return (
    <fieldset className="space-y-2 rounded-ds border border-border p-2" data-testid={`${testIdPrefix}-condition`}>
      <legend className="px-1 text-[11px] font-bold">شرایط مرسوله (اختیاری)</legend>
      <p className="text-[10px] text-muted">
        بازه‌ها نیم‌بازند: «از» شامل و «تا» غیرشامل است. خالی‌بودن «تا» یعنی بدون کران بالا.
      </p>
      <ConditionAxis
        titleFa="وزن مرسوله"
        unitFa="کیلوگرم"
        hintFa="وزن را با حداکثر سه رقم اعشار وارد کنید؛ مثال 1.999 دقیقاً ۱۹۹۹ گرم است."
        axis={form.weight}
        onChange={(axis) => onChange({ ...form, weight: axis })}
        disabled={disabled}
        testId={`${testIdPrefix}-condition-weight`}
      />
      <ConditionAxis
        titleFa="تعداد اقلام مرسوله"
        unitFa="قلم"
        hintFa="تعداد اقلام فیزیکی مرسوله است، نه تعداد خطوط سبد."
        axis={form.count}
        onChange={(axis) => onChange({ ...form, count: axis })}
        disabled={disabled}
        testId={`${testIdPrefix}-condition-count`}
      />
      <p className="text-[10px] text-muted" data-testid={`${testIdPrefix}-condition-preview`}>
        نتیجه: {describeConditionFa(preview)}
      </p>
    </fieldset>
  );
}

type ConditionAxisProps = {
  titleFa: string;
  unitFa: string;
  hintFa: string;
  axis: ShippingRateConditionForm["weight"];
  onChange: (next: ShippingRateConditionForm["weight"]) => void;
  disabled: boolean;
  testId: string;
};

function ConditionAxis({ titleFa, unitFa, hintFa, axis, onChange, disabled, testId }: ConditionAxisProps) {
  return (
    <div className="space-y-1" data-testid={testId}>
      <label className="flex items-center gap-2 text-[11px] font-bold">
        <input
          type="checkbox"
          checked={axis.enabled}
          onChange={(e) => onChange({ ...axis, enabled: e.target.checked })}
          disabled={disabled}
          data-testid={`${testId}-enabled`}
        />
        {titleFa}
      </label>
      {axis.enabled && (
        <>
          <div className="grid grid-cols-2 gap-2">
            <label className="text-[10px] text-muted">
              از (شامل) — {unitFa}
              <input
                className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                value={axis.min}
                onChange={(e) => onChange({ ...axis, min: e.target.value })}
                disabled={disabled}
                data-testid={`${testId}-min`}
              />
            </label>
            <label className="text-[10px] text-muted">
              تا (غیرشامل) — {unitFa}؛ خالی = بدون کران بالا
              <input
                className="mt-1 w-full rounded-ds border border-border px-2 py-1 text-xs"
                value={axis.max}
                onChange={(e) => onChange({ ...axis, max: e.target.value })}
                disabled={disabled}
                data-testid={`${testId}-max`}
              />
            </label>
          </div>
          <p className="text-[10px] text-muted">{hintFa}</p>
        </>
      )}
    </div>
  );
}
