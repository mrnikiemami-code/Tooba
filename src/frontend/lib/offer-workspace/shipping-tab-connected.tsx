"use client";

import type { OfferWorkspaceMessages } from "./messages";
import type {
  OfferFulfillmentResponsibility,
  OfferRole,
  OfferShippingMethodOption,
  OfferShippingSelectionMode,
  OfferShippingTypeOption,
  OfferWorkspaceDetail,
} from "./types";
import { ShippingTab } from "./shipping-tab";

interface ShippingTabConnectedProps {
  t: OfferWorkspaceMessages;
  role: OfferRole;
  detail: OfferWorkspaceDetail;
  shippingTypes: OfferShippingTypeOption[];
  typesError: string | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string;
  mode: OfferShippingSelectionMode;
  selectedMethodIds: string[];
  methodOptions: OfferShippingMethodOption[];
  methodsLoading: boolean;
  methodsError: string | null;
  saving: boolean;
  notice: string | null;
  error: string | null;
  setResponsibility: (value: OfferFulfillmentResponsibility | null) => void;
  setShippingTypeId: (value: string) => void;
  setMode: (value: OfferShippingSelectionMode) => void;
  setSelectedMethodIds: (value: string[]) => void;
  setNotice: (value: string | null) => void;
  setError: (value: string | null) => void;
}

/**
 * Presentation-only adapter between the workspace state and the approved Shipping tab. Owner/type changes
 * clear the specific set and return to Automatic so a stale method can never be carried across a scope change.
 */
export function ShippingTabConnected(props: ShippingTabConnectedProps) {
  return (
    <ShippingTab
      t={props.t}
      role={props.role}
      detail={props.detail}
      shippingTypes={props.shippingTypes}
      typesError={props.typesError}
      responsibility={props.responsibility}
      shippingTypeId={props.shippingTypeId}
      mode={props.mode}
      selectedMethodIds={props.selectedMethodIds}
      methodOptions={props.methodOptions}
      methodsLoading={props.methodsLoading}
      methodsError={props.methodsError}
      saving={props.saving}
      notice={props.notice}
      error={props.error}
      onResponsibilityChange={(value) => {
        props.setResponsibility(value);
        props.setMode("Automatic");
        props.setSelectedMethodIds([]);
        props.setNotice(null);
        props.setError(null);
        if (value == null) props.setShippingTypeId("");
      }}
      onShippingTypeChange={(value) => {
        props.setShippingTypeId(value);
        props.setMode("Automatic");
        props.setSelectedMethodIds([]);
        props.setNotice(null);
        props.setError(null);
      }}
      onModeChange={(value) => {
        props.setMode(value);
        if (value === "Automatic") props.setSelectedMethodIds([]);
        props.setNotice(null);
        props.setError(null);
      }}
      onSelectedMethodsChange={(value) => {
        props.setSelectedMethodIds(value);
        props.setNotice(null);
        props.setError(null);
      }}
    />
  );
}
