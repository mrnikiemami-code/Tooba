import type { StorefrontBackgroundStyle } from "./background-style.ts";
import type { StorefrontTintTokens } from "./palette-registry.ts";

export const NEUTRAL_CARD_RGB = "255 255 255";
export const NEUTRAL_ELEVATED_RGB = "255 255 255";
export const NEUTRAL_INPUT_RGB = "255 255 255";
export const NEUTRAL_INTERACTIVE_RGB = "244 244 245";
export const NEUTRAL_MEDIA_RGB = "249 250 251";
export const NEUTRAL_OVERLAY_RGB = "255 255 255";
export const NEUTRAL_BORDER_RGB = "228 228 231";

export const NEUTRAL_CARD_DARK_RGB = "28 28 32";
export const NEUTRAL_ELEVATED_DARK_RGB = "44 44 50";
export const NEUTRAL_INPUT_DARK_RGB = "28 28 32";
export const NEUTRAL_INTERACTIVE_DARK_RGB = "39 39 42";
export const NEUTRAL_MEDIA_DARK_RGB = "12 12 14";
export const NEUTRAL_OVERLAY_DARK_RGB = "44 44 50";
export const NEUTRAL_BORDER_DARK_RGB = "72 72 80";

export type DerivedComponentSurface = {
  card: string;
  elevated: string;
  input: string;
  interactive: string;
  media: string;
  overlay: string;
  border: string;
};

function clampChannel(value: number): number {
  return Math.max(0, Math.min(255, Math.round(value)));
}

export function mixRgb(first: string, second: string, amount: number): string {
  const a = first.split(" ").map((part) => Number.parseInt(part, 10));
  const b = second.split(" ").map((part) => Number.parseInt(part, 10));
  return [0, 1, 2]
    .map((index) => clampChannel((a[index] ?? 0) + ((b[index] ?? 0) - (a[index] ?? 0)) * amount))
    .join(" ");
}

/**
 * Card/Elevated/Input/Interactive/Media/Overlay/Border are system-derived.
 * They are not Store Appearance fields and must not gain Admin/DB controls.
 */
export function deriveComponentSurfaces(
  tint: StorefrontTintTokens,
  backgroundStyle: StorefrontBackgroundStyle,
  dark: boolean,
): DerivedComponentSurface {
  if (backgroundStyle === "Neutral") {
    return dark
      ? {
          card: NEUTRAL_CARD_DARK_RGB,
          elevated: NEUTRAL_ELEVATED_DARK_RGB,
          input: NEUTRAL_INPUT_DARK_RGB,
          interactive: NEUTRAL_INTERACTIVE_DARK_RGB,
          media: NEUTRAL_MEDIA_DARK_RGB,
          overlay: NEUTRAL_OVERLAY_DARK_RGB,
          border: NEUTRAL_BORDER_DARK_RGB,
        }
      : {
          card: NEUTRAL_CARD_RGB,
          elevated: NEUTRAL_ELEVATED_RGB,
          input: NEUTRAL_INPUT_RGB,
          interactive: NEUTRAL_INTERACTIVE_RGB,
          media: NEUTRAL_MEDIA_RGB,
          overlay: NEUTRAL_OVERLAY_RGB,
          border: NEUTRAL_BORDER_RGB,
        };
  }

  if (dark) {
    return {
      card: tint.sectionBackgroundDarkRgb,
      elevated: mixRgb(tint.sectionBackgroundDarkRgb, tint.sectionAccentDarkRgb, 0.42),
      input: mixRgb(tint.sectionBackgroundDarkRgb, tint.pageBackgroundDarkRgb, 0.22),
      interactive: tint.sectionAccentDarkRgb,
      media: tint.pageBackgroundDarkRgb,
      overlay: mixRgb(tint.sectionBackgroundDarkRgb, tint.sectionAccentDarkRgb, 0.55),
      border: mixRgb(tint.sectionAlternateDarkRgb, NEUTRAL_BORDER_DARK_RGB, 0.45),
    };
  }

  return {
    card: tint.sectionBackgroundRgb,
    elevated: mixRgb(tint.sectionBackgroundRgb, NEUTRAL_CARD_RGB, 0.28),
    input: mixRgb(tint.sectionBackgroundRgb, NEUTRAL_CARD_RGB, 0.42),
    interactive: mixRgb(tint.sectionAccentRgb, tint.sectionBackgroundRgb, 0.38),
    media: mixRgb(tint.pageBackgroundRgb, tint.sectionAlternateRgb, 0.28),
    overlay: mixRgb(tint.sectionBackgroundRgb, NEUTRAL_CARD_RGB, 0.18),
    border: mixRgb(tint.sectionAlternateRgb, NEUTRAL_BORDER_RGB, 0.5),
  };
}

export function derivedSurfaceCssVars(tint: StorefrontTintTokens): Record<string, string> {
  const light = deriveComponentSurfaces(tint, "PaletteTint", false);
  const dark = deriveComponentSurfaces(tint, "PaletteTint", true);
  return {
    "--color-card-derived": light.card,
    "--color-elevated-derived": light.elevated,
    "--color-input-derived": light.input,
    "--color-interactive-derived": light.interactive,
    "--color-media-derived": light.media,
    "--color-overlay-derived": light.overlay,
    "--color-border-derived": light.border,
    "--color-card-derived-dark": dark.card,
    "--color-elevated-derived-dark": dark.elevated,
    "--color-input-derived-dark": dark.input,
    "--color-interactive-derived-dark": dark.interactive,
    "--color-media-derived-dark": dark.media,
    "--color-overlay-derived-dark": dark.overlay,
    "--color-border-derived-dark": dark.border,
  };
}

export function isPureWhiteRgb(rgb: string): boolean {
  const [r, g, b] = rgb.split(" ").map((part) => Number.parseInt(part, 10));
  return (r ?? 0) >= 252 && (g ?? 0) >= 252 && (b ?? 0) >= 252;
}
