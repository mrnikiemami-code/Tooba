import { FORBIDDEN_SETTING_KEYS, type ControlledSettingKey, type ControlledSettingsSchema } from "./types.ts";
import { isSizePreset } from "./size-presets.ts";
import { DATA_SOURCE_KINDS } from "./types.ts";
import { SURFACE_ROLES } from "./types.ts";

export function assertNoForbiddenSettings(settings: Record<string, unknown>): void {
  for (const key of Object.keys(settings)) {
    if ((FORBIDDEN_SETTING_KEYS as readonly string[]).includes(key)) {
      throw new Error(`Forbidden setting key: ${key}`);
    }
    if (/css|className|breakpoint|px$/i.test(key) && !(["ctaHref", "itemCount"] as string[]).includes(key)) {
      if ((FORBIDDEN_SETTING_KEYS as readonly string[]).includes(key) || /css|className|breakpoint/i.test(key)) {
        throw new Error(`Forbidden setting key: ${key}`);
      }
    }
  }
}

export function normalizeControlledSettings(
  schema: ControlledSettingsSchema,
  raw: Record<string, unknown>,
): Record<string, unknown> {
  assertNoForbiddenSettings(raw);
  const out: Record<string, unknown> = { ...schema.defaults };
  for (const key of schema.allowedKeys) {
    if (raw[key] === undefined) continue;
    out[key] = coerceSetting(key, raw[key]);
  }
  for (const key of Object.keys(raw)) {
    if (!(schema.allowedKeys as readonly string[]).includes(key)) {
      throw new Error(`Unknown or arbitrary setting rejected: ${key}`);
    }
  }
  return out;
}

function coerceSetting(key: ControlledSettingKey, value: unknown): string | number | boolean {
  switch (key) {
    case "title":
    case "subtitle":
    case "ctaLabel":
    case "ctaHref":
    case "layoutPreset":
    case "dataSource":
      if (typeof value !== "string") throw new Error(`${key} must be string`);
      if (key === "dataSource" && !(DATA_SOURCE_KINDS as readonly string[]).includes(value)) {
        throw new Error(`Unknown dataSource: ${value}`);
      }
      return value;
    case "heightPreset":
      if (!isSizePreset(value)) throw new Error("heightPreset must be a size preset");
      return value;
    case "itemCount":
      if (typeof value !== "number" || !Number.isFinite(value) || value < 1 || value > 48) {
        throw new Error("itemCount must be 1..48");
      }
      return Math.floor(value);
    case "autoplay":
    case "enabled":
      if (typeof value !== "boolean") throw new Error(`${key} must be boolean`);
      return value;
    case "surfaceRole":
      if (typeof value !== "string" || !(SURFACE_ROLES as readonly string[]).includes(value)) {
        throw new Error("surfaceRole must be a global surface role");
      }
      return value;
    default:
      throw new Error(`Unsupported setting: ${key}`);
  }
}

export const BASE_SECTION_SETTINGS: ControlledSettingsSchema = {
  allowedKeys: ["title", "subtitle", "ctaLabel", "ctaHref", "itemCount", "dataSource", "layoutPreset", "heightPreset", "autoplay", "enabled", "surfaceRole"],
  defaults: {
    enabled: true,
    itemCount: 8,
    heightPreset: "Medium",
    surfaceRole: "section",
    autoplay: false,
  },
};
