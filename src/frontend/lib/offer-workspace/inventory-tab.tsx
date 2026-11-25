"use client";

import { Warehouse } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferInventoryLocationOption, OfferLocalInventoryDraft, OfferWorkspaceDetail } from "./types";
import { parseDecimal } from "./model";
import type { OfferFormatters } from "./i18n";
import styles from "./offer-workspace.module.css";
import { DirtyDot, Note, SectionTitle } from "./tab-shared";

export interface InventoryTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  detail: OfferWorkspaceDetail;
  onHand: Record<string, string>;
  reason: string;
  dirty: boolean;
  onOnHandChange: (stockItemId: string, value: string) => void;
  onReasonChange: (value: string) => void;
  localLocations?: OfferInventoryLocationOption[];
  localInventory?: OfferLocalInventoryDraft[];
  onLocalInventoryChange?: (locationId: string, patch: { onHand?: string }) => void;
}

export function InventoryTab(props: InventoryTabProps) {
  const { t, format, detail } = props;
  const totalOnHand = detail.inventoryLocations.reduce((sum, row) => sum + row.onHand, 0);
  const totalReserved = detail.inventoryLocations.reduce((sum, row) => sum + row.reserved, 0);
  const totalAvailable = detail.inventoryLocations.reduce((sum, row) => sum + row.available, 0);
  const noPositions = detail.inventoryLocations.length === 0;
  const preCreate = noPositions && Boolean(props.localLocations?.length && props.localInventory);

  if (preCreate) {
    return (
      <div className={styles.stack} data-testid="offer-tab-inventory">
        <section className={styles.card}>
          <header className={styles.cardHd}>
            <SectionTitle title={t.inventoryTitle} desc={t.inventoryDesc} />
          </header>
          <div className={styles.cardBd}>
            <Note tone="info">{t.inventoryProvisionNote}</Note>
            <div className={styles.tableWrap} style={{ marginTop: 14 }}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{t.inventoryLocation}</th>
                    <th>{t.inventoryOnHand}</th>
                    <th>{t.inventoryReserved}</th>
                    <th>{t.inventoryAvailable}</th>
                  </tr>
                </thead>
                <tbody>
                  {(props.localLocations ?? []).map((location) => {
                    const draft = props.localInventory?.find((row) => row.locationId === location.locationId);
                    return (
                      <tr key={location.locationId} data-testid="offer-inventory-row" data-location-id={location.locationId}>
                        <td data-label={t.inventoryLocation}>
                          <span style={{ display: "block", fontWeight: 600 }}>{location.name || location.code}</span>
                          <span className={`${styles.resultMeta} hidden md:block`} dir="ltr">{location.code}</span>
                        </td>
                        <td data-label={t.inventoryOnHand}>
                          <input
                            className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
                            style={{ maxWidth: 128 }}
                            value={draft?.onHand ?? ""}
                            onChange={(event) => props.onLocalInventoryChange?.(location.locationId, { onHand: event.target.value })}
                            inputMode="decimal"
                            dir="ltr"
                            data-testid="offer-inventory-input"
                          />
                        </td>
                        <td data-label={t.inventoryReserved} className={styles.numCell}>—</td>
                        <td data-label={t.inventoryAvailable} className={styles.numCell}>—</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <label className={styles.field} style={{ marginTop: 14, maxWidth: 420 }}>
              <span className={styles.fieldLabel}>{t.inventoryReason}</span>
              <input className={styles.ctl} value={props.reason} onChange={(event) => props.onReasonChange(event.target.value)} data-testid="offer-inventory-reason" />
              <span className={styles.fieldHint}>{t.inventoryReasonHint}</span>
            </label>
          </div>
        </section>
      </div>
    );
  }
  return (
    <div className={styles.stack} data-testid="offer-tab-inventory">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.inventoryTitle} desc={t.inventoryDesc} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.tiles} data-testid="offer-inventory-summary">
            <Tile label={t.inventoryPhysical} value={format.number(totalOnHand)} testId="offer-inventory-total-onhand" />
            <Tile label={t.inventoryReserved} value={format.number(totalReserved)} testId="offer-inventory-total-reserved" />
            <Tile label={t.inventoryAvailable} value={format.number(totalAvailable)} testId="offer-inventory-total-available" />
          </div>

          <div style={{ marginTop: 14 }}>
            {noPositions ? (
              <div className={styles.empty} data-testid="offer-inventory-empty">
                <span className={styles.emptyIc}><Warehouse size={16} aria-hidden /></span>
                <span className={styles.emptyT}>{t.inventoryEmpty}</span>
                <span className={styles.emptyD}>{t.inventoryUnavailable}</span>
              </div>
            ) : (
              <div className={styles.tableWrap}>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>{t.inventoryLocation}</th>
                      <th>{t.inventoryOnHand}</th>
                      <th>{t.inventoryReserved}</th>
                      <th>{t.inventoryAvailable}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.inventoryLocations.map((row) => {
                      const value = parseDecimal(props.onHand[row.stockItemId]);
                      const invalid = value != null && (value < 0 || value < row.reserved);
                      return (
                        <tr key={row.stockItemId} data-testid="offer-inventory-row" data-stock-item-id={row.stockItemId}>
                          <td data-label={t.inventoryLocation}>
                            <span style={{ display: "block", fontWeight: 600 }}>{row.locationName || row.locationCode}</span>
                            <span className={`${styles.resultMeta} hidden md:block`} dir="ltr">{row.locationCode}</span>
                          </td>
                          <td data-label={t.inventoryOnHand}>
                            <input
                              className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`}
                              style={{ maxWidth: 128 }}
                              value={props.onHand[row.stockItemId] ?? ""}
                              onChange={(event) => props.onOnHandChange(row.stockItemId, event.target.value)}
                              inputMode="decimal"
                              dir="ltr"
                              aria-invalid={invalid}
                              aria-label={`${t.inventoryOnHand} ${row.locationName || row.locationCode}`}
                              data-testid="offer-inventory-input"
                            />
                          </td>
                          <td data-label={t.inventoryReserved} className={styles.numCell} data-testid="offer-inventory-reserved">
                            {format.number(row.reserved)}
                          </td>
                          <td data-label={t.inventoryAvailable} className={styles.numCell} data-testid="offer-inventory-available">
                            {format.number(row.available)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <label className={styles.field} style={{ marginTop: 14, maxWidth: 420 }}>
            <span className={styles.fieldLabel}>
              {t.inventoryReason}
              <DirtyDot show={props.dirty} label={t.dirtyTab} />
            </span>
            <input
              className={styles.ctl}
              value={props.reason}
              onChange={(event) => props.onReasonChange(event.target.value)}
              data-testid="offer-inventory-reason"
            />
            <span className={styles.fieldHint}>{t.inventoryReasonHint}</span>
          </label>

          <div className={styles.stack} style={{ marginTop: 12 }}>
            <Note>{t.inventoryReservedReadOnly}</Note>
            <Note tone="warning">{t.inventoryUnavailable}</Note>
          </div>
        </div>
      </section>
    </div>
  );
}

function Tile({ label, value, testId }: { label: string; value: string; testId: string }) {
  return (
    <div className={styles.tile}>
      <div className={styles.tileK}>{label}</div>
      <div className={styles.tileV} data-testid={testId}>{value}</div>
    </div>
  );
}
