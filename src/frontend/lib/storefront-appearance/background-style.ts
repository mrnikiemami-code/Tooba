export const BACKGROUND_STYLES = ["Neutral", "PaletteTint"] as const;
export type StorefrontBackgroundStyle = (typeof BACKGROUND_STYLES)[number];

export const DEFAULT_BACKGROUND_STYLE: StorefrontBackgroundStyle = "Neutral";

export function resolveBackgroundStyle(raw: string | null | undefined): StorefrontBackgroundStyle {
  return raw?.trim() === "PaletteTint" ? "PaletteTint" : "Neutral";
}

export function isKnownBackgroundStyle(raw: string | null | undefined): boolean {
  const value = raw?.trim();
  return value === "Neutral" || value === "PaletteTint";
}
