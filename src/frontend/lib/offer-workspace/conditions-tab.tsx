"use client";

import type { ReactNode } from "react";
import { Input, Select } from "../../design-system";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferWorkspaceDetail } from "./types";
import { allowedStatuses, deriveBuyability, faNumber } from "./model";
import { DirtyDot, Note, TabHeading } from "./tab-shared";

export interface ConditionsTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  detail: OfferWorkspaceDetail;
  returnPolicyChoice: string;
  customReturnWindowDays: string;
  minimumOrderQuantity: string;
  maximumOrderQuantity: string;
  status: string;
  dirty: boolean;
  onReturnPolicyChange: (value: string) => void;
  onCustomReturnWindowChange: (value: string) => void;
  onMinQtyChange: (value: string) => void;
  onMaxQtyChange: (value: string) => void;
  onStatusChange: (value: string) => void;
  extra?: ReactNode;
}

export function ConditionsTab(props: ConditionsTabProps) {
  const { t, locale, detail } = props;
  const buyability = deriveBuyability({
    status: props.status,
    amount: detail.amount,
    availableUnits: detail.availableUnits,
  });
  const statuses = allowedStatuses(detail.status);
  const archived = detail.status === "Archived";
  const returnOptions: Array<{ value: string; label: string }> = [
    { value: "Default", label: t.returnDefault },
  ];
  if (detail.sellerCanOverrideReturnPolicy) {
    returnOptions.push({ value: "Custom", label: t.returnCustom });
    if (detail.allowNonReturnableOffers) {
      returnOptions.push({ value: "NonReturnable", label: t.returnNonReturnable });
    }
  }

  return (
    <div data-testid="offer-tab-conditions" className="grid gap-6">
      <section>
        <TabHeading title={t.returnPolicy} hint={t.returnWindowHint + ` ${detail.defaultReturnWindowDays}`} />
        <label className="flex max-w-md flex-col gap-1 text-sm">
          <span className="flex items-center gap-2">
            {t.returnPolicy}
            <DirtyDot show={props.dirty} label={t.dirtyTab} />
          </span>
          <Select
            value={props.returnPolicyChoice}
            onChange={(event) => props.onReturnPolicyChange(event.target.value)}
            disabled={!detail.sellerCanOverrideReturnPolicy && props.returnPolicyChoice === "Default"}
            data-testid="offer-return-policy"
          >
            {returnOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
          <span className="text-xs text-muted">
            {detail.sellerCanOverrideReturnPolicy
              ? `${t.returnWindowHint}: ${detail.defaultReturnWindowDays} · ${detail.minReturnWindowDays}–${detail.maxReturnWindowDays}`
              : t.returnSellerCannotOverride}
          </span>
        </label>
        {props.returnPolicyChoice === "Custom" ? (
          <label className="mt-2 flex max-w-md flex-col gap-1 text-sm">
            {t.returnCustomDays}
            <Input
              value={props.customReturnWindowDays}
              onChange={(event) => props.onCustomReturnWindowChange(event.target.value)}
              inputMode="numeric"
              dir="ltr"
              data-testid="offer-custom-return-days"
            />
          </label>
        ) : null}
      </section>

      <section>
        <TabHeading title={`${t.minQty} / ${t.maxQty}`} hint={t.unitReadOnly} />
        <div className="grid max-w-lg gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            {t.minQty}
            <Input
              value={props.minimumOrderQuantity}
              onChange={(event) => props.onMinQtyChange(event.target.value)}
              inputMode="decimal"
              dir="ltr"
              data-testid="offer-min-qty"
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            {t.maxQty}
            <Input
              value={props.maximumOrderQuantity}
              onChange={(event) => props.onMaxQtyChange(event.target.value)}
              inputMode="decimal"
              dir="ltr"
              data-testid="offer-max-qty"
            />
          </label>
        </div>
        <p className="mt-2 text-xs text-muted" data-testid="offer-product-unit">
          {t.unit}: {detail.productUnitName ?? detail.productUnitCode ?? "—"}
          {detail.productUnitShortName ? ` (${detail.productUnitShortName})` : ""}
        </p>
        <Note>{t.roundingNeedsContract}</Note>
      </section>

      <section>
        <TabHeading title={t.lifecycle} />
        <div className="grid max-w-md gap-2">
          <Select
            value={props.status}
            onChange={(event) => props.onStatusChange(event.target.value)}
            disabled={archived}
            data-testid="offer-lifecycle"
          >
            {statuses.map((status) => (
              <option key={status} value={status}>
                {statusLabel(t, status)}
              </option>
            ))}
          </Select>
          {archived ? <Note tone="warning">{t.archivedLocked}</Note> : null}
          <div className="rounded-ds border border-border px-3 py-2 text-sm" data-testid="offer-buyability">
            <span className="text-muted">{buyability.buyable ? t.buyable : t.notBuyable}</span>
            {buyability.reason ? (
              <span className="ms-2 text-xs text-danger">{buyabilityReason(t, buyability.reason)}</span>
            ) : null}
          </div>
          <span className="text-xs text-muted">
            {t.inventoryAvailable}: {faNumber(detail.availableUnits, locale)}
          </span>
        </div>
      </section>
      {props.extra ? <div>{props.extra}</div> : null}
    </div>
  );
}

function statusLabel(t: OfferWorkspaceMessages, status: string): string {
  switch (status) {
    case "Draft":
      return t.lifecycleDraft;
    case "Active":
      return t.lifecycleActive;
    case "Suspended":
      return t.lifecycleSuspended;
    case "Archived":
      return t.lifecycleArchived;
    default:
      return status;
  }
}

function buyabilityReason(t: OfferWorkspaceMessages, reason: string): string {
  switch (reason) {
    case "offer.buyable.no_price":
      return t.buyableReasonNoPrice;
    case "offer.buyable.no_stock":
      return t.buyableReasonNoStock;
    case "offer.buyable.inactive":
      return t.buyableReasonInactive;
    default:
      return reason;
  }
}
