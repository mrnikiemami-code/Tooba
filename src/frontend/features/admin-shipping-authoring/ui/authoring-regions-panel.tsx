"use client";

/**
 * Truthful READ-ONLY regions of one existing draft: stored group identity/order/currency, proven
 * geography with real labels, and every stored rate row. Nothing here is editable in this slice and
 * no group/row/geography is ever rebuilt from the general form.
 */
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { authoringFormat } from "../messages/authoring-messages.ts";
import type { EditorDocument, EditorRateRow } from "../model/editor-document.ts";
import type { DestinationFactIndex } from "../model/editor-destination-ancestry.ts";
import { formatDecimalText, formatNumber } from "./authoring-format.ts";

const AREA_KIND_LABEL = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;

function conditionText(row: EditorRateRow, locale: AuthoringLocale, messages: AuthoringMessages): string {
  const parts: string[] = [];
  if (row.minWeightGrams !== null || row.maxWeightGrams !== null) {
    parts.push(
      `${messages.weightLabel}: ${row.minWeightGrams === null ? "—" : formatNumber(row.minWeightGrams, locale)}–${
        row.maxWeightGrams === null ? "—" : formatNumber(row.maxWeightGrams, locale)
      }`,
    );
  }
  if (row.minItemCount !== null || row.maxItemCount !== null) {
    parts.push(
      `${messages.countLabel}: ${row.minItemCount === null ? "—" : formatNumber(row.minItemCount, locale)}–${
        row.maxItemCount === null ? "—" : formatNumber(row.maxItemCount, locale)
      }`,
    );
  }
  if (row.minMerchandiseAmount !== null || row.maxMerchandiseAmount !== null) {
    parts.push(
      `${messages.amountLabel}: ${row.minMerchandiseAmount === null ? "—" : formatDecimalText(row.minMerchandiseAmount, locale)}–${
        row.maxMerchandiseAmount === null ? "—" : formatDecimalText(row.maxMerchandiseAmount, locale)
      }`,
    );
  }
  return parts.length > 0 ? parts.join(" · ") : messages.noCondition;
}

export function AuthoringRegionsPanel({
  document,
  index,
  locale,
  messages,
  contentLanguageId,
}: {
  document: EditorDocument;
  index: DestinationFactIndex;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  contentLanguageId: string;
}) {
  return (
    <div className="grid gap-3" data-testid="shipping-authoring-regions-panel">
      <p className="rounded-xl border border-border bg-surface px-3 py-2 text-xs text-gray-600">{messages.regionsNote}</p>

      {document.regionGroups.length === 0 ? (
        <p className="text-xs text-gray-500">{messages.noGroups}</p>
      ) : (
        document.regionGroups.map((group, position) => {
          const title = group.titles.find(
            (entry) => entry.languageId.toLowerCase() === contentLanguageId.toLowerCase(),
          );
          return (
            <section
              key={group.groupId}
              className="rounded-xl border border-border bg-surface-elevated p-3"
              data-testid={`shipping-authoring-group-${group.order}`}
            >
              <header className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-sm font-black text-gray-900">
                  {title && title.title.trim().length > 0
                    ? title.title
                    : authoringFormat(messages.groupTitle, { index: formatNumber(position + 1, locale) })}
                </h3>
                <span className="text-[11px] text-gray-500">
                  {authoringFormat(messages.regionsGroupOrder, { order: formatNumber(group.order, locale) })}
                  {" · "}
                  {messages.currencyLabel}: <span dir="ltr">{group.currencyCode ?? "—"}</span>
                </span>
              </header>

              <div className="mt-2">
                <p className="text-[11px] font-bold text-gray-800">{messages.areasLabel}</p>
                {group.areas.length === 0 ? (
                  <p className="text-[11px] text-gray-500">{messages.noAreas}</p>
                ) : (
                  <ul className="mt-1 flex flex-wrap gap-1">
                    {group.areas.map((area) => {
                      const label = index.labelOf(area);
                      return (
                        <li
                          key={`${area.kind}:${area.id}`}
                          className="rounded-full bg-surface px-2 py-0.5 text-[11px] text-gray-700"
                          data-testid={`shipping-authoring-area-${area.kind}`}
                        >
                          <span className="text-gray-500">{messages[AREA_KIND_LABEL[area.kind]]}: </span>
                          {label ?? <span className="font-semibold text-amber-700">{messages.unavailableName}</span>}
                        </li>
                      );
                    })}
                  </ul>
                )}
              </div>

              <div className="mt-2">
                <p className="text-[11px] font-bold text-gray-800">{messages.ratesTitle}</p>
                {group.rows.length === 0 ? (
                  <p className="text-[11px] text-gray-500">{messages.noRows}</p>
                ) : (
                  <ul className="mt-1 grid gap-1">
                    {group.rows.map((row) => (
                      <li
                        key={row.rateRowId}
                        className="grid gap-1 rounded-lg bg-surface px-2 py-1 text-[11px] text-gray-700 md:grid-cols-4"
                        data-testid="shipping-authoring-rate-row"
                      >
                        <span className="md:col-span-2">{conditionText(row, locale, messages)}</span>
                        <span>
                          {messages.freightLabel}:{" "}
                          <span dir="ltr">{formatDecimalText(row.freightAmount, locale)}</span>
                        </span>
                        <span>
                          {messages.packagingLabel}:{" "}
                          <span dir="ltr">{formatDecimalText(row.packagingAmount, locale)}</span>
                          {" · "}
                          {authoringFormat(messages.deliveryRange, {
                            min: formatNumber(row.minDeliveryDays, locale),
                            max: formatNumber(row.maxDeliveryDays, locale),
                          })}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </section>
          );
        })
      )}
    </div>
  );
}
