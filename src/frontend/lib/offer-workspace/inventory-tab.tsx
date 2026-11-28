"use client";

import { useState } from "react";
import { Warehouse } from "lucide-react";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferInventoryLocationOption, OfferLocalInventoryDraft, OfferWorkspaceDetail } from "./types";
import { parseDecimal } from "./model";
import type { OfferFormatters } from "./i18n";
import styles from "./offer-workspace.module.css";
import { DirtyDot, SectionTitle } from "./tab-shared";

export interface InventoryTabProps {
  t: OfferWorkspaceMessages;
  format: OfferFormatters;
  detail: OfferWorkspaceDetail;
  onHand: Record<string, string>;
  reason: string;
  dirty: boolean;
  offeredQuantity?: string;
  onOfferedQuantityChange?: (value: string) => void;
  onOnHandChange: (stockItemId: string, value: string) => void;
  onReasonChange: (value: string) => void;
  mode: "create" | "edit";
  onProvision?: (locationId: string) => Promise<void>;
  provisionDisabled?: boolean;
  localLocations?: OfferInventoryLocationOption[];
  localInventory?: OfferLocalInventoryDraft[];
  onLocalInventoryChange?: (locationId: string, patch: { onHand?: string }) => void;
  onLocalInventoryAdd?: (locationId: string) => void;
  onLocalInventoryRemove?: (locationId: string) => void;
}

export function InventoryTab(props: InventoryTabProps) {
  const { t, format, detail } = props;
  const totalOnHand = detail.onHand;
  const totalReserved = detail.reserved;
  const totalAvailable = detail.availableUnits;
  const noPositions = detail.inventoryLocations.length === 0;
  const preCreate = props.mode === "create" && noPositions;
  const selectedLocations = (props.localLocations ?? []).filter(location => props.localInventory?.some(row => row.locationId === location.locationId));

  if (preCreate) {
    return (
      <div className={styles.stack} data-testid="offer-tab-inventory">
        <section className={styles.card}>
          <header className={styles.cardHd}>
            <SectionTitle title={t.inventoryTitle} />
          </header>
          <div className={styles.cardBd}>
            {selectedLocations.length > 0 ? <div className={styles.tableWrap}>
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
                  {selectedLocations.map((location) => {
                    const draft = props.localInventory?.find((row) => row.locationId === location.locationId);
                    return (
                      <tr key={location.locationId} data-testid="offer-inventory-row" data-location-id={location.locationId}>
                        <td data-label={t.inventoryLocation}>
                          <span style={{ display: "block", fontWeight: 600 }}>{location.name || location.code}</span>
                          <span className={`${styles.resultMeta} hidden md:block`} dir="ltr">{location.code}</span>
                          <button type="button" className={`${styles.btn} ${styles.btnSm} ${styles.btnQuiet}`} onClick={() => props.onLocalInventoryRemove?.(location.locationId)} data-testid="offer-inventory-remove-draft">{t.cancel}</button>
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
            </div> : <div className={styles.empty} data-testid="offer-inventory-empty"><span className={styles.emptyIc}><Warehouse size={16} aria-hidden /></span><span className={styles.emptyT}>{t.inventoryEmpty}</span></div>}
            <AddInventoryPosition t={t} locations={(props.localLocations ?? []).filter(location => !selectedLocations.some(row => row.locationId === location.locationId))} onProvision={async locationId => { props.onLocalInventoryAdd?.(locationId); }} disabled={Boolean(props.provisionDisabled)} empty={selectedLocations.length === 0} />
            {selectedLocations.length > 0 ? <label className={styles.field} style={{ marginTop: 14, maxWidth: 420 }}>
              <span className={styles.fieldLabel}>{t.inventoryReason}</span>
              <input className={styles.ctl} value={props.reason} onChange={(event) => props.onReasonChange(event.target.value)} data-testid="offer-inventory-reason" />
              <span className={styles.fieldHint}>{t.inventoryReasonHint}</span>
            </label> : null}
          </div>
        </section>
      </div>
    );
  }
  return (
    <div className={styles.stack} data-testid="offer-tab-inventory">
      <section className={styles.card}>
        <header className={styles.cardHd}>
          <SectionTitle title={t.inventoryTitle} />
        </header>
        <div className={styles.cardBd}>
          <div className={styles.tiles} data-testid="offer-inventory-summary">
            <Tile label={t.inventoryPhysical} value={format.number(totalOnHand)} testId="offer-inventory-total-onhand" />
            <Tile label={t.inventoryReserved} value={format.number(totalReserved)} testId="offer-inventory-total-reserved" />
            <Tile label={t.inventoryAvailable} value={format.number(totalAvailable)} testId="offer-inventory-total-available" />
          </div>

          {detail.capabilities?.canEditOfferedQuantity ? (
            <label className={styles.field} style={{ marginTop: 14, maxWidth: 420 }}>
              <span className={styles.fieldLabel}>{t.inventoryOfferedQuantity}</span>
              <input className={`${styles.ctl} ${styles.ctlNum} ${styles.ctlLtr}`} value={props.offeredQuantity ?? ""} onChange={(event) => props.onOfferedQuantityChange?.(event.target.value)} inputMode="decimal" dir="ltr" data-testid="offer-offered-quantity-input" />
              <span className={styles.fieldHint}>{t.inventoryOfferedHint}</span>
            </label>
          ) : null}
          <div style={{ marginTop: 14 }}>
            {noPositions ? (
              <div className={styles.empty} data-testid="offer-inventory-empty">
                <span className={styles.emptyIc}><Warehouse size={16} aria-hidden /></span>
                <span className={styles.emptyT}>{t.inventoryEmpty}</span>
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
                              disabled={detail.capabilities?.canEditPhysicalInventory !== true}
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

          {props.mode === "edit" && props.onProvision && detail.capabilities?.canEditPhysicalInventory === true && detail.status !== "Archived" ? (
            <AddInventoryPosition t={t} locations={(props.localLocations ?? []).filter(location => !detail.inventoryLocations.some(row => row.locationId === location.locationId))} onProvision={props.onProvision} disabled={Boolean(props.provisionDisabled)} empty={noPositions} />
          ) : null}
          {detail.capabilities?.canEditPhysicalInventory === true && !noPositions ? <label className={styles.field} style={{ marginTop: 14, maxWidth: 420 }}>
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
          </label> : null}

        </div>
      </section>
    </div>
  );
}

function AddInventoryPosition({ t, locations, onProvision, disabled, empty }: {
  t: OfferWorkspaceMessages;
  locations: OfferInventoryLocationOption[];
  onProvision: (locationId: string) => Promise<void>;
  disabled: boolean;
  empty: boolean;
}) {
  const [selected, setSelected] = useState("");
  const [search, setSearch] = useState("");
  const query = search.trim().toLocaleLowerCase();
  const filtered = locations.filter(location => `${location.name} ${location.code}`.toLocaleLowerCase().includes(query) || location.locationId === selected);
  const valid = locations.some(location => location.locationId === selected);
  if (!locations.length) return empty ? <p className={styles.resultMeta}>{t.inventoryNoEligible}</p> : null;
  return (
    <div className={styles.row} style={{ marginTop: 14, alignItems: "end" }}>
      <label className={styles.field} style={{ flex: "1 1 220px", maxWidth: 420 }}>
        <span className={styles.fieldLabel}>{t.inventoryLocation}</span>
        <input type="search" className={styles.ctl} aria-label={`${t.search} ${t.inventoryLocation}`} placeholder={t.search} value={search} onChange={event => setSearch(event.target.value)} disabled={disabled} data-testid="offer-inventory-location-search" />
        <select className={`${styles.ctl} ${styles.select}`} value={valid ? selected : ""} onChange={event => setSelected(event.target.value)} disabled={disabled} data-testid="offer-inventory-location-select">
          <option value="">{t.select}</option>
          {filtered.map(location => <option key={location.locationId} value={location.locationId}>{location.name && location.code ? `${location.name} — ${location.code}` : location.name || location.code}</option>)}
        </select>
      </label>
      <button type="button" className={`${styles.btn} ${styles.btnPrimary}`} disabled={disabled || !valid} onClick={() => void onProvision(selected).then(() => { setSelected(""); setSearch(""); })} data-testid="offer-inventory-add-position">{t.inventoryAddLocation}</button>
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
