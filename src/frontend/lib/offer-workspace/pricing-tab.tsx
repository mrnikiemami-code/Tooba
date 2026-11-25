"use client";

import { Input } from "../../design-system";
import type { OfferWorkspaceMessages } from "./messages";
import type { OfferWorkspaceDetail } from "./types";
import { faMoney, parseDecimal } from "./model";
import { DirtyDot, Note, TabHeading } from "./tab-shared";

export interface PricingTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  detail: OfferWorkspaceDetail;
  amount: string;
  currency: string;
  dirty: boolean;
  onAmountChange: (value: string) => void;
}

export function PricingTab(props: PricingTabProps) {
  const { t, locale, detail } = props;
  const parsed = parseDecimal(props.amount);
  return (
    <div data-testid="offer-tab-pricing">
      <TabHeading title={t.pricingTab} hint={t.readonlySeam} />
      <div className="overflow-x-auto rounded-ds border border-border">
        <table className="w-full min-w-[40rem] text-sm">
          <thead className="bg-secondary/60 text-xs text-muted">
            <tr>
              <th className="px-3 py-2 text-start">{t.pricingCurrency}</th>
              <th className="px-3 py-2 text-start">{t.pricingAmount}</th>
              <th className="px-3 py-2 text-start">{t.market}</th>
              <th className="px-3 py-2 text-start">{t.channel}</th>
              <th className="px-3 py-2 text-start">{t.pricingType}</th>
              <th className="px-3 py-2 text-start">{t.pricingStatus}</th>
            </tr>
          </thead>
          <tbody>
            <tr data-testid="offer-price-row">
              <td className="px-3 py-2 font-mono text-xs" dir="ltr">
                {detail.currency || "IRR"}
              </td>
              <td className="px-3 py-2 tabular-nums">{faMoney(detail.amount, detail.currency, locale)}</td>
              <td className="px-3 py-2">IR</td>
              <td className="px-3 py-2">
                {detail.channel === "Marketplace" ? t.channelMarketplace : detail.channel}
              </td>
              <td className="px-3 py-2">{t.pricingTypeBase}</td>
              <td className="px-3 py-2">{detail.amount == null ? "—" : t.lifecycleActive}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div className="mt-4 grid max-w-md gap-2">
        <label className="flex flex-col gap-1 text-sm">
          <span className="flex items-center gap-2">
            {t.pricingAmount}
            <DirtyDot show={props.dirty} label={t.dirtyTab} />
          </span>
          <Input
            value={props.amount}
            onChange={(event) => props.onAmountChange(event.target.value)}
            inputMode="numeric"
            dir="ltr"
            invalid={parsed != null && parsed < 0}
            data-testid="offer-price-input"
          />
          <span className="text-xs text-muted">
            {props.currency || "IRR"} · {t.pricingIrradix}
          </span>
        </label>
      </div>
      <div className="mt-4">
        <Note>{t.pricingNotWired}</Note>
      </div>
    </div>
  );
}
