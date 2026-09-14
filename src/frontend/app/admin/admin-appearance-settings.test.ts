import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { appearanceIsDirty, appearancePreviewStyle } from "./admin-appearance-settings.helpers.ts";
import { listStorefrontPalettes, resolveBrandTokens } from "../../lib/storefront-appearance/palette-registry.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const page = readFileSync(join(dir, "settings/page.tsx"), "utf8");
const form = readFileSync(join(dir, "admin-appearance-settings.tsx"), "utf8");
const helpers = readFileSync(join(dir, "admin-appearance-settings.helpers.ts"), "utf8");
const api = readFileSync(join(dir, "appearance-settings-api.ts"), "utf8");

test("settings page has appearance tab on existing shell", () => {
  assert.match(page, /admin-settings-tab-\$\{tab\.id\}/);
  assert.match(page, /id: "appearance"/);
  assert.match(page, /AdminAppearanceSettingsForm/);
  assert.match(page, /loadAppearanceSettings/);
  assert.match(page, /appearanceSkinDraft/);
  assert.match(page, /appearanceBackgroundDraft/);
  assert.match(page, /onSelectSkin/);
  assert.match(page, /onSelectBackground/);
  assert.doesNotMatch(page, /type=\"color\"/);
});

test("current palette dirty cancel save states are wired", () => {
  assert.match(form, /admin-settings-appearance-form/);
  assert.match(form, /admin-settings-save-appearance/);
  assert.match(form, /admin-settings-cancel-appearance/);
  assert.match(form, /admin-settings-appearance-preview/);
  assert.match(form, /admin-settings-appearance-error/);
  assert.match(form, /admin-settings-appearance-unknown/);
  assert.match(form, /admin-settings-appearance-themes/);
  assert.match(form, /admin-settings-appearance-skins/);
  assert.match(form, /admin-settings-appearance-backgrounds/);
  assert.match(form, /admin-settings-appearance-background-\$\{option\.key\}/);
  assert.match(form, /admin-settings-appearance-skin-\$\{skin\.key\}/);
  assert.match(form, /AdminProductCardSkinPreview/);
  assert.match(form, /descriptionFa/);
  assert.match(form, /admin-settings-appearance-card-preview/);
  assert.match(form, /پس‌زمینه فروشگاه/);
  assert.match(form, /خنثی/);
  assert.match(helpers, /رنگی ملایم/);
  assert.match(helpers, /title: "خنثی"/);
  assert.doesNotMatch(form, /dir=\"ltr\">\{skin\.key\}/);
  assert.doesNotMatch(form, /dir=\"ltr\">\{option\.key\}/);
  assert.doesNotMatch(form, /dir=\"ltr\">\{preset\.key\}/);
  assert.equal(appearanceIsDirty("tooba-blue", "forest-green"), true);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue"), false);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue", "LightOnly", "DarkOnly"), true);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue", "Light", "LightOnly"), false);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue", "LightOnly", "LightOnly", "classic", "clean"), true);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue", "LightOnly", "LightOnly", "classic", "classic"), false);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue", "LightOnly", "LightOnly", "classic", "classic", "Neutral", "PaletteTint"), true);
  assert.equal(appearanceIsDirty("tooba-blue", "tooba-blue", "LightOnly", "LightOnly", "classic", "classic", "Neutral", "Neutral"), false);
});

test("preview uses canonical registry tokens and leaves status colors", () => {
  const style = appearancePreviewStyle("violet-royal");
  assert.equal(style["--color-primary"], resolveBrandTokens("violet-royal").primaryRgb);
  assert.equal(style["--color-primary-on-dark"], resolveBrandTokens("violet-royal").primaryOnDarkRgb);
  assert.equal(Object.hasOwn(style, "--color-danger"), false);
  assert.match(form, /bg-success|text-success/);
  assert.match(form, /bg-danger|text-danger/);
  assert.doesNotMatch(form, /type=\"color\"/);
  assert.doesNotMatch(api, /localStorage/);
  assert.doesNotMatch(form, /useEffect/);
});

test("save posts paletteKey only once per helper", () => {
  assert.match(api, /method: \"PUT\"/);
  assert.match(api, /JSON\.stringify\(\{ paletteKey, themeMode, productCardSkin, backgroundStyle \}\)/);
  assert.doesNotMatch(api, /JSON\.stringify\(\{[^}]*primaryRgb/);
  assert.doesNotMatch(api, /localStorage/);
  assert.equal(listStorefrontPalettes().length >= 6, true);
  assert.equal(listStorefrontPalettes().length <= 8, true);
});

test("save failure leaves dirty draft and surfaces error", () => {
  assert.match(page, /ذخیرهٔ ظاهر فروشگاه انجام نشد/);
  assert.match(page, /error=\{error\}/);
  assert.match(page, /if \(!result.ok\) \{\s*setError/);
  assert.doesNotMatch(page, /setAppearanceDraft\(\"tooba-blue\"\)/);
  assert.match(form, /admin-settings-appearance-error/);
});
