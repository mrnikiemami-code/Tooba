/**
 * Read-only presentation of ONE stored rate row's conditions (no policy, no calculation).
 *
 * A stored row can constrain weight, item count and merchandise amount independently, in any
 * combination, and each axis may be open-ended. The read view must show EVERY stored axis; showing
 * only weight would silently claim "no condition" for a count-only or amount-only row. Decimals are
 * rendered from the exact stored token text (never re-rounded).
 */
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import type { DecimalText, EditorRateRow } from "../model/editor-document.ts";
import type { RateRowDraft } from "../model/editor-rates.ts";
import { formatDecimalText, formatNumber } from "./authoring-format.ts";

function integerBound(value: number | null, locale: AuthoringLocale): string {
  return value === null ? "—" : formatNumber(value, locale);
}

function amountBound(value: string | null, locale: AuthoringLocale): string {
  return value === null ? "—" : formatDecimalText(value, locale);
}

/** `label: from–to` for one axis (the open upper bound is shown as `—`, never invented). */
function axis(label: string, min: string, max: string): string {
  return `${label}: ${min}–${max}`;
}

/** Truthful condition text of one stored row: all three real axes, then `noCondition` when none. */
export function rateConditionText(row: EditorRateRow, locale: AuthoringLocale, messages: AuthoringMessages): string {
  const parts: string[] = [];
  if (row.minWeightGrams !== null || row.maxWeightGrams !== null) {
    parts.push(
      axis(
        messages.weightLabel,
        integerBound(row.minWeightGrams, locale),
        integerBound(row.maxWeightGrams, locale),
      ),
    );
  }
  if (row.minItemCount !== null || row.maxItemCount !== null) {
    parts.push(
      axis(messages.countLabel, integerBound(row.minItemCount, locale), integerBound(row.maxItemCount, locale)),
    );
  }
  if (row.minMerchandiseAmount !== null || row.maxMerchandiseAmount !== null) {
    parts.push(
      axis(
        messages.amountLabel,
        amountBound(row.minMerchandiseAmount, locale),
        amountBound(row.maxMerchandiseAmount, locale),
      ),
    );
  }
  return parts.length > 0 ? parts.join(" · ") : messages.noCondition;
}

/** Same truthful text straight from an editable draft (stored text for untouched rows). */
export function rateDraftConditionText(
  row: RateRowDraft,
  locale: AuthoringLocale,
  messages: AuthoringMessages,
): string {
  return rateConditionText(
    {
      rateRowId: row.rateRowId ?? "",
      minWeightGrams: row.minWeightGrams === "" ? null : Number(row.minWeightGrams),
      maxWeightGrams: row.maxWeightGrams === "" ? null : Number(row.maxWeightGrams),
      minItemCount: row.minItemCount === "" ? null : Number(row.minItemCount),
      maxItemCount: row.maxItemCount === "" ? null : Number(row.maxItemCount),
      minMerchandiseAmount: row.minMerchandiseAmount === "" ? null : (row.minMerchandiseAmount as DecimalText),
      maxMerchandiseAmount: row.maxMerchandiseAmount === "" ? null : (row.maxMerchandiseAmount as DecimalText),
      freightAmount: row.freightAmount as DecimalText,
      packagingAmount: row.packagingAmount as DecimalText,
      minDeliveryDays: 0,
      maxDeliveryDays: 0,
    },
    locale,
    messages,
  );
}
