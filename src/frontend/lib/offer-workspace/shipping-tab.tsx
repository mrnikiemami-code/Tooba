"use client";

import { Button, Input, Select } from "../../design-system";
import type { OfferWorkspaceMessages } from "./messages";
import type {
  OfferFulfillmentResponsibility,
  OfferRole,
  OfferShippingTypeOption,
  OfferWorkspaceDetail,
} from "./types";
import { Note, TabHeading } from "./tab-shared";

export interface ShippingTabProps {
  t: OfferWorkspaceMessages;
  locale: "fa" | "en";
  role: OfferRole;
  detail: OfferWorkspaceDetail;
  shippingTypes: OfferShippingTypeOption[];
  typesError: string | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string;
  serviceId: string;
  saving: boolean;
  notice: string | null;
  error: string | null;
  onResponsibilityChange: (value: OfferFulfillmentResponsibility | null) => void;
  onShippingTypeChange: (value: string) => void;
  onServiceChange: (value: string) => void;
  onSave: () => void;
}

export function ShippingTab(props: ShippingTabProps) {
  const { t, locale, role } = props;
  const responsibilityLabel =
    props.responsibility === "Seller"
      ? t.shippingSeller
      : props.responsibility === "Platform"
        ? t.shippingPlatform
        : t.shippingUnassigned;

  if (role === "seller") {
    return (
      <div data-testid="offer-tab-shipping">
        <TabHeading title={t.shippingTab} hint={t.shippingAdminOnly} />
        <div className="grid gap-2">
          <Note>{t.shippingCurrentUnknown}</Note>
          <Note tone="warning">{t.shippingServiceGated}</Note>
        </div>
        {props.shippingTypes.length > 0 ? (
          <div className="mt-4 rounded-ds border border-border p-3 text-sm">
            <p className="mb-2 text-xs text-muted">{t.shippingType}</p>
            <ul className="grid gap-1">
              {props.shippingTypes
                .filter((type) => type.isActive)
                .map((type) => (
                  <li key={type.shippingTypeId} className="flex items-center justify-between gap-2" dir="ltr">
                    <span>{type.label}</span>
                    <span className="font-mono text-[11px] text-muted">{type.code}</span>
                  </li>
                ))}
            </ul>
          </div>
        ) : null}
      </div>
    );
  }

  return (
    <div data-testid="offer-tab-shipping">
      <TabHeading title={t.shippingTab} hint={t.shippingCurrentUnknown} />
      <div className="grid max-w-lg gap-3">
        <label className="flex flex-col gap-1 text-sm">
          {t.shippingResponsibility}
          <Select
            value={props.responsibility ?? ""}
            onChange={(event) => {
              const value = event.target.value;
              props.onResponsibilityChange(value === "" ? null : (value as OfferFulfillmentResponsibility));
            }}
            data-testid="offer-shipping-responsibility"
          >
            <option value="">{t.shippingUnassigned}</option>
            <option value="Seller">{t.shippingSeller}</option>
            <option value="Platform">{t.shippingPlatform}</option>
          </Select>
          <span className="text-xs text-muted">{responsibilityLabel}</span>
        </label>

        <label className="flex flex-col gap-1 text-sm">
          {t.shippingType}
          <Select
            value={props.shippingTypeId}
            onChange={(event) => props.onShippingTypeChange(event.target.value)}
            disabled={props.responsibility == null}
            data-testid="offer-shipping-type"
          >
            <option value="">{t.shippingUnassigned}</option>
            {props.shippingTypes
              .filter((type) => type.isActive)
              .map((type) => (
                <option key={type.shippingTypeId} value={type.shippingTypeId}>
                  {type.label} ({type.code})
                </option>
              ))}
          </Select>
          {props.typesError ? <span className="text-xs text-danger">{props.typesError}</span> : null}
        </label>

        <label className="flex flex-col gap-1 text-sm">
          {t.shippingService}
          <Input
            value={props.serviceId}
            onChange={(event) => props.onServiceChange(event.target.value)}
            disabled={props.responsibility == null}
            placeholder={t.shippingServiceDisabled}
            dir="ltr"
            data-testid="offer-shipping-service"
          />
          <span className="text-xs text-muted">{t.shippingServiceGated}</span>
        </label>

        <div className="flex flex-wrap items-center gap-3">
          <Button
            type="button"
            onClick={props.onSave}
            disabled={props.saving}
            data-testid="offer-shipping-save"
          >
            {props.saving ? t.saving : t.shippingSave}
          </Button>
          {props.notice ? <span className="text-sm text-success" data-testid="offer-shipping-notice">{props.notice}</span> : null}
          {props.error ? <span className="text-sm text-danger" data-testid="offer-shipping-error">{props.error}</span> : null}
        </div>
        {locale === "fa" ? <Note tone="warning">{t.shippingServiceGated}</Note> : null}
      </div>
    </div>
  );
}
