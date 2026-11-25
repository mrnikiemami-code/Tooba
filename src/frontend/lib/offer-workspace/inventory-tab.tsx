"use client";

import { Input } from "../../design-system";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferWorkspaceDetail } from "./types";
import { faNumber, parseDecimal } from "./model";
import { DirtyDot, Note, TabHeading } from "./tab-shared";

export interface InventoryTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  detail: OfferWorkspaceDetail;
  onHand: Record<string, string>;
  reason: string;
  dirty: boolean;
  onOnHandChange: (stockItemId: string, value: string) => void;
  onReasonChange: (value: string) => void;
}

export function InventoryTab(props: InventoryTabProps) {
  const { t, locale, detail } = props;
  const totalOnHand = detail.inventoryLocations.reduce((sum, row) => sum + row.onHand, 0);
  const totalReserved = detail.inventoryLocations.reduce((sum, row) => sum + row.reserved, 0);
  const totalAvailable = detail.inventoryLocations.reduce((sum, row) => sum + row.available, 0);

  return (
    <div data-testid="offer-tab-inventory">
      <TabHeading title={t.inventoryTab} hint={t.inventoryReservedReadOnly} />
      <dl className="mb-4 grid grid-cols-1 gap-2 sm:grid-cols-3">
        <Summary label={t.inventoryPhysical} value={faNumber(totalOnHand, locale)} testId="offer-inventory-total-onhand" />
        <Summary label={t.inventoryReserved} value={faNumber(totalReserved, locale)} testId="offer-inventory-total-reserved" />
        <Summary label={t.inventoryAvailable} value={faNumber(totalAvailable, locale)} testId="offer-inventory-total-available" />
      </dl>
      {detail.inventoryLocations.length === 0 ? (
        <Note tone="warning">{t.inventoryNoPositions}</Note>
      ) : (
        <div className="overflow-x-auto rounded-ds border border-border">
          <table className="w-full min-w-[36rem] text-sm">
            <thead className="bg-secondary/60 text-xs text-muted">
              <tr>
                <th className="px-3 py-2 text-start">{t.inventoryLocation}</th>
                <th className="px-3 py-2 text-start">{t.inventoryOnHand}</th>
                <th className="px-3 py-2 text-start">{t.inventoryReserved}</th>
                <th className="px-3 py-2 text-start">{t.inventoryAvailable}</th>
              </tr>
            </thead>
            <tbody>
              {detail.inventoryLocations.map((row) => {
                const value = parseDecimal(props.onHand[row.stockItemId]);
                const invalid = value != null && (value < 0 || value < row.reserved);
                return (
                  <tr key={row.stockItemId} data-testid="offer-inventory-row" data-stock-item-id={row.stockItemId}>
                    <td className="px-3 py-2">
                      <span className="block font-medium" dir="ltr">
                        {row.locationCode}
                      </span>
                      <span className="block text-[11px] text-muted" dir="ltr">
                        {row.locationId}
                      </span>
                    </td>
                    <td className="px-3 py-2">
                      <Input
                        className="max-w-32"
                        value={props.onHand[row.stockItemId] ?? ""}
                        onChange={(event) => props.onOnHandChange(row.stockItemId, event.target.value)}
                        inputMode="decimal"
                        dir="ltr"
                        invalid={invalid}
                        aria-label={`${t.inventoryOnHand} ${row.locationCode}`}
                        data-testid="offer-inventory-input"
                      />
                    </td>
                    <td className="px-3 py-2 tabular-nums text-muted" data-testid="offer-inventory-reserved">
                      {faNumber(row.reserved, locale)}
                    </td>
                    <td className="px-3 py-2 tabular-nums" data-testid="offer-inventory-available">
                      {faNumber(row.available, locale)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      <label className="mt-4 flex max-w-md flex-col gap-1 text-sm">
        <span className="flex items-center gap-2">
          {t.inventoryReason}
          <DirtyDot show={props.dirty} label={t.dirtyTab} />
        </span>
        <Input
          value={props.reason}
          onChange={(event) => props.onReasonChange(event.target.value)}
          data-testid="offer-inventory-reason"
        />
        <span className="text-xs text-muted">{t.inventoryReasonHint}</span>
      </label>
      <div className="mt-4 grid gap-2">
        <Note>{t.inventoryImmutableLocation}</Note>
        <Note tone="warning">{t.inventoryNeedsContract}</Note>
      </div>
    </div>
  );
}

function Summary({ label, value, testId }: { label: string; value: string; testId: string }) {
  return (
    <div className="rounded-ds border border-border bg-secondary/30 px-3 py-2">
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="mt-0.5 tabular-nums text-lg font-semibold" data-testid={testId}>
        {value}
      </dd>
    </div>
  );
}
