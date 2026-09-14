"use client";

import { listStorefrontPalettes, resolveBrandTokens, resolvePaletteKey } from "../../lib/storefront-appearance/palette-registry.ts";
import { listProductCardSkins, resolveProductCardSkin } from "../../lib/storefront-appearance/product-card-skin.ts";
import { resolveThemeMode } from "../../lib/storefront-appearance/theme-mode.ts";
import { THEME_MODE_OPTIONS, appearanceIsDirty, appearancePreviewStyle } from "./admin-appearance-settings.helpers.ts";
import { AdminProductCardSkinPreview } from "./admin-product-card-skin-preview.tsx";
import type { AppearanceSettingsView } from "./appearance-settings-api.ts";

function rgbCss(rgb: string): string {
  return `rgb(${rgb.replaceAll(" ", ",")})`;
}

export function AdminAppearanceSettingsForm(props: {
  view: AppearanceSettingsView | null;
  draftKey: string;
  draftTheme: string;
  draftSkin: string;
  busy: boolean;
  readOnly: boolean;
  error: string | null;
  onSelect: (key: string) => void;
  onSelectTheme: (mode: string) => void;
  onSelectSkin: (skin: string) => void;
  onSave: () => void;
  onCancel: () => void;
}) {
  const presets = props.view?.presets?.length ? props.view.presets : [...listStorefrontPalettes()];
  const skins = [...listProductCardSkins()];
  const preview = resolveBrandTokens(props.draftKey);
  const savedKey = props.view?.paletteKey ?? "tooba-blue";
  const savedTheme = props.view?.themeMode ?? "LightOnly";
  const savedSkin = props.view?.productCardSkin ?? "classic";
  const draftTheme = resolveThemeMode(props.draftTheme);
  const draftSkin = resolveProductCardSkin(props.draftSkin);
  const dirty = appearanceIsDirty(savedKey, props.draftKey, savedTheme, draftTheme, savedSkin, draftSkin);
  const previewDark = draftTheme === "DarkOnly";
  const unknown = props.view != null && !props.view.paletteKeyWasKnown;

  return (
    <form
      className="space-y-5"
      onSubmit={(event) => {
        event.preventDefault();
        props.onSave();
      }}
      data-testid="admin-settings-appearance-form"
    >
      <div>
        <h2 className="text-sm font-bold text-gray-900">ظاهر فروشگاه</h2>
        <p className="text-sm text-gray-500 leading-7 mt-1">
          پالت برند، حالت تم و پوستهٔ کارت کالا را انتخاب کنید. رنگ‌های وضعیت (موفقیت، هشدار، خطر) به پالت وابسته نیستند.
        </p>
      </div>
      {unknown ? (
        <p className="text-xs text-amber-800 bg-amber-50 border border-amber-100 rounded-xl px-3 py-2" data-testid="admin-settings-appearance-unknown">
          پالت ذخیره‌شده دیگر در فهرست نیست. پیش‌نمایش روی آبی توبا است تا پالت معتبری ذخیره شود.
        </p>
      ) : null}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3" data-testid="admin-settings-appearance-presets">
        {presets.map((preset) => {
          const selected = resolvePaletteKey(props.draftKey) === preset.key;
          return (
            <button
              key={preset.key}
              type="button"
              disabled={props.readOnly || props.busy}
              onClick={() => props.onSelect(preset.key)}
              className={`text-right rounded-xl border p-3 transition-all ${
                selected ? "border-[#2563EB] ring-2 ring-[#2563EB]/20" : "border-gray-200 hover:border-gray-300"
              }`}
              data-testid={`admin-settings-appearance-preset-${preset.key}`}
              data-selected={selected ? "true" : "false"}
            >
              <span className="flex items-center gap-2">
                <span
                  className="h-7 w-7 rounded-full border border-black/10"
                  style={{ backgroundColor: rgbCss(preset.tokens.primaryRgb) }}
                />
                <span className="text-sm font-bold text-gray-900">{preset.nameFa}</span>
              </span>
            </button>
          );
        })}
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3" data-testid="admin-settings-appearance-themes">
        {THEME_MODE_OPTIONS.map((option) => {
          const selected = draftTheme === option.key;
          return (
            <button
              key={option.key}
              type="button"
              disabled={props.readOnly || props.busy}
              onClick={() => props.onSelectTheme(option.key)}
              className={`text-right rounded-xl border p-3 transition-all ${
                selected ? "border-[#2563EB] ring-2 ring-[#2563EB]/20" : "border-gray-200 hover:border-gray-300"
              }`}
              data-testid={`admin-settings-appearance-theme-${option.key}`}
              data-selected={selected ? "true" : "false"}
            >
              <span className="block text-sm font-bold text-gray-900">{option.title}</span>
              <span className="block text-xs text-gray-500 mt-2 leading-6">{option.body}</span>
            </button>
          );
        })}
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3" data-testid="admin-settings-appearance-skins">
        {skins.map((skin) => {
          const selected = draftSkin === skin.key;
          return (
            <button
              key={skin.key}
              type="button"
              disabled={props.readOnly || props.busy}
              onClick={() => props.onSelectSkin(skin.key)}
              className={`text-right rounded-xl border p-3 transition-all ${
                selected ? "border-[#2563EB] ring-2 ring-[#2563EB]/20" : "border-gray-200 hover:border-gray-300"
              }`}
              data-testid={`admin-settings-appearance-skin-${skin.key}`}
              data-selected={selected ? "true" : "false"}
            >
              <span className="block text-sm font-bold text-gray-900">{skin.nameFa}</span>
              <span className="block text-xs text-gray-500 mt-1 leading-6">{skin.descriptionFa}</span>
              <span className="block mt-3" data-preview-skin-chrome={skin.key}>
                <AdminProductCardSkinPreview skin={skin.key} compact />
              </span>
            </button>
          );
        })}
      </div>
      <div
        className={`rounded-xl border border-gray-200 p-4 space-y-3 ${previewDark ? "dark bg-background text-foreground" : ""}`}
        style={appearancePreviewStyle(props.draftKey)}
        data-testid="admin-settings-appearance-preview"
        data-preview-theme={draftTheme}
        data-preview-skin={draftSkin}
      >
        <p className="text-xs text-gray-500">پیش‌نمایش ذخیره‌نشده</p>
        <button type="button" className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-bold">
          دکمه اصلی
        </button>
        <a className="block text-sm text-primary underline" href="#preview">پیوند تأکیدی</a>
        <AdminProductCardSkinPreview skin={draftSkin} testId="admin-settings-appearance-card-preview" />
        <p className="text-xs text-gray-500">متن کم‌رنگ</p>
        <div className="flex gap-2 text-[11px]">
          <span className="px-2 py-1 rounded bg-success/15 text-success">موفقیت</span>
          <span className="px-2 py-1 rounded bg-warning/15 text-warning">هشدار</span>
          <span className="px-2 py-1 rounded bg-danger/15 text-danger">خطر</span>
        </div>
        <span className="hidden" data-preview-primary={preview.primaryRgb} />
      </div>
      {props.error ? (
        <p className="text-sm text-red-700" data-testid="admin-settings-appearance-error">{props.error}</p>
      ) : null}
      <div className="flex gap-2">
        <button
          type="submit"
          disabled={props.busy || props.readOnly || !dirty}
          className="flex-1 py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-70"
          data-testid="admin-settings-save-appearance"
        >
          {props.busy ? "در حال ذخیره…" : "ذخیره ظاهر"}
        </button>
        <button
          type="button"
          disabled={props.busy || !dirty}
          onClick={props.onCancel}
          className="px-4 py-2.5 border border-gray-200 rounded-xl text-sm"
          data-testid="admin-settings-cancel-appearance"
        >
          انصراف
        </button>
      </div>
    </form>
  );
}
