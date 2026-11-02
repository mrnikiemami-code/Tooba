"use client";

import { useCallback, useEffect, useState } from "react";
import { Field, Input } from "../../design-system";
import { authoringMessages } from "../../features/admin-shipping-authoring/messages/authoring-messages.ts";
import { listCarrierAccounts } from "../../features/admin-shipping-authoring/api/carrier-authoring-api.ts";
import {
  ADMIN_ACTOR_STORAGE_KEY,
  ADMIN_DEV_ACTOR_HEADER,
  DEFAULT_ADMIN_ACTOR_ID,
  adminRequestHeaders,
} from "../../lib/admin/admin-result.ts";

type ShipmentState = "Pending" | "Succeeded" | "Failed" | "Unknown" | number;

interface ShipmentView {
  operationId: string;
  state: ShipmentState;
  providerTrackingCode: string | null;
  providerStatus: string | null;
  lastErrorCode: string | null;
  canTrack: boolean;
  canCancel: boolean;
  canLabel: boolean;
}

function actorHeader(): Record<string, string> {
  const actor =
    typeof window === "undefined"
      ? DEFAULT_ADMIN_ACTOR_ID
      : window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
  return { ...adminRequestHeaders(), [ADMIN_DEV_ACTOR_HEADER]: actor };
}

function errorCode(payload: unknown): string | null {
  if (!payload || typeof payload !== "object") return null;
  const root = payload as Record<string, unknown>;
  const code = root.errorCode ?? root.ErrorCode;
  return typeof code === "string" ? code : null;
}

function asString(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value : null;
}

function decodeShipment(row: unknown): ShipmentView | null {
  if (!row || typeof row !== "object") return null;
  const item = row as Record<string, unknown>;
  const operationId = asString(item.operationId ?? item.OperationId);
  if (!operationId) return null;
  return {
    operationId,
    state: (item.state ?? item.State ?? "Unknown") as ShipmentState,
    providerTrackingCode: asString(item.providerTrackingCode ?? item.ProviderTrackingCode),
    providerStatus: asString(item.providerStatus ?? item.ProviderStatus),
    lastErrorCode: asString(item.lastErrorCode ?? item.LastErrorCode),
    canTrack: item.canTrack === true || item.CanTrack === true,
    canCancel: item.canCancel === true || item.CanCancel === true,
    canLabel: item.canLabel === true || item.CanLabel === true,
  };
}

async function readJson(response: Response): Promise<unknown> {
  return response.json().catch(() => null);
}

export function AdminCarrierShipmentPanel({
  checkoutId,
  recipientName,
  recipientMobile,
  recipientAddress,
  postalCode,
}: {
  checkoutId: string;
  recipientName: string;
  recipientMobile: string;
  recipientAddress: string;
  postalCode: string;
}) {
  const messages = authoringMessages("fa");
  const [rows, setRows] = useState<readonly ShipmentView[]>([]);
  const [status, setStatus] = useState("");
  const [busy, setBusy] = useState(false);
  const [accountId, setAccountId] = useState("");
  const [groupId, setGroupId] = useState("");
  const [serviceId, setServiceId] = useState("7");
  const [originCode, setOriginCode] = useState("");
  const [senderName, setSenderName] = useState("");
  const [senderMobile, setSenderMobile] = useState("");
  const [senderAddress, setSenderAddress] = useState("");

  const refresh = useCallback(async () => {
    const response = await fetch(
      `/v1/admin/shipping-services/shipments?checkoutId=${encodeURIComponent(checkoutId)}`,
      { headers: actorHeader() },
    );
    const payload = await readJson(response);
    if (response.status === 400 && errorCode(payload) === "shipping.carrier.unconfigured") {
      setStatus(messages.carrierUnconfigured);
      setRows([]);
      return;
    }
    if (!response.ok) {
      setStatus(errorCode(payload) === "shipping.carrier.unconfigured" ? messages.carrierUnconfigured : messages.carrierSaveFailed);
      return;
    }
    const list = Array.isArray(payload) ? payload : [];
    setRows(list.map(decodeShipment).filter((row): row is ShipmentView => row !== null));
  }, [checkoutId, messages.carrierSaveFailed, messages.carrierUnconfigured]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  async function post(path: string, body?: unknown): Promise<string | null> {
    const response = await fetch(path, {
      method: "POST",
      headers: { ...actorHeader(), ...(body ? { "Content-Type": "application/json" } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
    const payload = await readJson(response);
    if (!response.ok) {
      const code = errorCode(payload);
      if (code === "shipping.carrier.unconfigured") return messages.carrierUnconfigured;
      if (code === "shipping.carrier.shipment.unsupported") return messages.carrierShipmentUnsupported;
      if (code === "shipping.carrier.shipment.unknown") return messages.carrierShipmentUnsupported;
      return messages.carrierSaveFailed;
    }
    return null;
  }

  return (
    <section className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm" data-testid="admin-carrier-shipment-panel">
      <h2 className="text-sm font-black text-gray-900">{messages.carrierShipmentTitle}</h2>
      <p className="mt-1 text-[11px] text-gray-500">{messages.carrierShipmentHint}</p>
      {status ? (
        <p className="mt-2 text-xs font-semibold text-gray-700" data-testid="admin-carrier-shipment-status">
          {status}
        </p>
      ) : null}
      <div className="mt-3 grid gap-2 md:grid-cols-2">
        <Field id="admin-carrier-shipment-group" label={messages.carrierShipmentGroupId}>
          <Input value={groupId} onChange={(event) => setGroupId(event.target.value)} data-testid="admin-carrier-shipment-group" />
        </Field>
        <Field id="admin-carrier-shipment-origin" label={messages.carrierOriginCity}>
          <Input value={originCode} onChange={(event) => setOriginCode(event.target.value)} data-testid="admin-carrier-shipment-origin" />
        </Field>
        <Field id="admin-carrier-shipment-sender" label={messages.carrierAccountName}>
          <Input value={senderName} onChange={(event) => setSenderName(event.target.value)} />
        </Field>
        <Field id="admin-carrier-shipment-mobile" label={messages.carrierUsername}>
          <Input value={senderMobile} onChange={(event) => setSenderMobile(event.target.value)} />
        </Field>
        <Field id="admin-carrier-shipment-address" label={messages.carrierOriginCity}>
          <Input value={senderAddress} onChange={(event) => setSenderAddress(event.target.value)} />
        </Field>
        <Field id="admin-carrier-shipment-service" label={messages.carrierService}>
          <Input value={serviceId} onChange={(event) => setServiceId(event.target.value)} />
        </Field>
      </div>
      <div className="mt-3 flex flex-wrap gap-2">
        <button
          type="button"
          className="min-h-11 rounded-lg bg-primary px-3 text-sm font-bold text-primary-foreground"
          disabled={busy}
          data-testid="admin-carrier-shipment-create"
          onClick={async () => {
            setBusy(true);
            const listed = await listCarrierAccounts();
            const account = listed.kind === "ok" ? listed.value.find((row) => row.isActive) ?? listed.value[0] : null;
            setAccountId(account?.accountId ?? "");
            if (!account) {
              setBusy(false);
              setStatus(messages.carrierUnconfigured);
              return;
            }
            const failed = await post("/v1/admin/shipping-services/shipments", {
              checkoutId,
              shippingGroupId: groupId,
              carrierAccountId: account.accountId,
              providerServiceId: serviceId,
              sender: {
                fullName: senderName,
                mobile: senderMobile,
                addressLine: senderAddress,
                locationCode: originCode,
                postalCode,
              },
              recipient: {
                fullName: recipientName,
                mobile: recipientMobile,
                addressLine: recipientAddress,
                locationCode: originCode,
                postalCode,
              },
              package: {
                weightGrams: 500,
                declaredValue: 0,
                lengthCentimeters: 20,
                widthCentimeters: 15,
                heightCentimeters: 10,
                collectAmount: 0,
              },
            });
            setBusy(false);
            setStatus(failed ?? messages.carrierAccountSaved);
            if (!failed) await refresh();
          }}
        >
          {messages.carrierShipmentCreate}
        </button>
      </div>
      {rows.length === 0 ? (
        <p className="mt-3 text-xs text-gray-500">{messages.carrierShipmentEmpty}</p>
      ) : (
        <ul className="mt-3 space-y-2">
          {rows.map((row) => (
            <li key={row.operationId} className="rounded-lg border border-gray-100 p-2 text-xs">
              <p dir="ltr">{row.providerTrackingCode ?? row.operationId}</p>
              <p>{String(row.state)} {row.providerStatus ?? row.lastErrorCode ?? ""}</p>
              <div className="mt-2 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="min-h-11 rounded-lg border px-3"
                  disabled={busy || !row.canTrack}
                  onClick={async () => {
                    setBusy(true);
                    const failed = await post(`/v1/admin/shipping-services/shipments/${row.operationId}/track`);
                    setBusy(false);
                    setStatus(failed ?? messages.carrierTestResult);
                    if (!failed) await refresh();
                  }}
                >
                  {messages.carrierShipmentTrack}
                </button>
                <button
                  type="button"
                  className="min-h-11 rounded-lg border px-3"
                  disabled={busy || !row.canCancel}
                  onClick={async () => {
                    setBusy(true);
                    const failed = await post(`/v1/admin/shipping-services/shipments/${row.operationId}/cancel`);
                    setBusy(false);
                    setStatus(failed ?? messages.carrierTestResult);
                    if (!failed) await refresh();
                  }}
                >
                  {messages.carrierShipmentCancel}
                </button>
                <span className="self-center text-[11px] text-gray-500">{messages.carrierLabelUnsupported}</span>
              </div>
            </li>
          ))}
        </ul>
      )}
      {accountId ? <p className="mt-2 hidden">{accountId}</p> : null}
    </section>
  );
}
