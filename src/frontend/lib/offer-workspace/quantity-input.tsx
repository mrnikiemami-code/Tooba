"use client";

import type { OfferFormatters } from "./i18n";
import { normalizeDigits } from "./model";
import styles from "./offer-workspace.module.css";

/** Preserve empty and partial decimal input; never silently truncate or round. */
export function quantityInputValue(raw: string, scale: number, culture: string): string | null {
  const separator = new Intl.NumberFormat(culture).formatToParts(1.1).find(part => part.type === "decimal")?.value ?? ".";
  const normalized = normalizeDigits(separator === "." ? raw : raw.split(separator).join("."));
  const syntax = scale > 0 ? /^\d*(?:\.\d*)?$/ : /^\d*$/;
  if (!syntax.test(normalized)) return null;
  // Extra fractional precision remains invalid until corrected; domain validation owns precision/step.
  return normalized;
}

/** Shared Offer quantity editor, localized display and canonical decimal draft. */
export function QuantityInput({ value, onChange, scale, format, testId, disabled, invalid, label, compact }: {
  value: string;
  onChange: (value: string) => void;
  scale: number;
  format: OfferFormatters;
  testId: string;
  disabled?: boolean;
  invalid?: boolean;
  label?: string;
  compact?: boolean;
}) {
  const separator = new Intl.NumberFormat(format.locale.culture).formatToParts(1.1).find(part => part.type === "decimal")?.value ?? ".";
  return <input className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
    style={compact ? { maxWidth: 128 } : undefined}
    value={format.digitsIn(value.split(".").join(separator))}
    onChange={event => {
      const next = quantityInputValue(event.target.value, scale, format.locale.culture);
      if (next !== null) onChange(next);
    }}
    inputMode={scale > 0 ? "decimal" : "numeric"} dir="ltr" autoComplete="off"
    disabled={disabled} aria-invalid={invalid || undefined} aria-label={label} data-testid={testId} />;
}
