"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Field, Input } from "../../design-system";
import { authoringMessages } from "../../features/admin-shipping-authoring/messages/authoring-messages.ts";

type ShipmentState = "Pending" | "Succeeded" | "Failed" | "Unknown" | number;

interface CapabilityRow {
  shippingGroupId: string;
  methodLabel: string;
  methodCode: string;
  transportMode: string;
  recipientName: string;
  recipientMobile: string;
  recipientAddress: string;
  postalCode: string | null;
  provinceName: string;
  cityName: string;
  originLocationCode: string | null;
  destinationLocationCode: string | null;
  weightGrams: number | null;
  declaredValue: number | null;
  lengthCentimeters: number | null;
  widthCentimeters: number | null;
  heightCentimeters: number | null;
  carrierAccountId: string | null;
  providerServiceCode: string | null;
  canExecuteCarrierShipment: boolean;
  ineligibilityReasonCode: string | null;
}

interface ShipmentView {
  operationId: string;
  shippingGroupId: string;
  state: ShipmentState;
  providerTrackingCode: string | null;
  providerStatus: string | null;
  lastErrorCode: string | null;
  canTrack: boolean;
  canCancel: boolean;
}

function asString(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value : null;
}

function asNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function decodeCapability(row: unknown): CapabilityRow | null {
  if (!row || typeof row !== "object") return null;
  const item = row as Record<string, unknown>;
  const shippingGroupId = asString(item.shippingGroupId ?? item.ShippingGroupId);
  if (!shippingGroupId) return null;
  return {
    shippingGroupId,
    methodLabel: asString(item.methodLabel ?? item.MethodLabel) ?? "—",
    methodCode: asString(item.methodCode ?? item.MethodCode) ?? "",
    transportMode: asString(item.transportMode ?? item.TransportMode) ?? "",
    recipientName: asString(item.recipientName ?? item.RecipientName) ?? "",
    recipientMobile: asString(item.recipientMobile ?? item.RecipientMobile) ?? "",
    recipientAddress: asString(item.recipientAddress ?? item.RecipientAddress) ?? "",
    postalCode: asString(item.postalCode ?? item.PostalCode),
    provinceName: asString(item.provinceName ?? item.ProvinceName) ?? "",
    cityName: asString(item.cityName ?? item.CityName) ?? "",
    originLocationCode: asString(item.originLocationCode ?? item.OriginLocationCode),
    destinationLocationCode: asString(item.destinationLocationCode ?? item.DestinationLocationCode),
    weightGrams: asNumber(item.weightGrams ?? item.WeightGrams),
    declaredValue: asNumber(item.declaredValue ?? item.DeclaredValue),
    lengthCentimeters: asNumber(item.lengthCentimeters ?? item.LengthCentimeters),
    widthCentimeters: asNumber(item.widthCentimeters ?? item.WidthCentimeters),
    heightCentimeters: asNumber(item.heightCentimeters ?? item.HeightCentimeters),
    carrierAccountId: asString(item.carrierAccountId ?? item.CarrierAccountId),
    providerServiceCode: asString(item.providerServiceCode ?? item.ProviderServiceCode),
    canExecuteCarrierShipment: item.canExecuteCarrierShipment === true || item.CanExecuteCarrierShipment === true,
    ineligibilityReasonCode: asString(item.ineligibilityReasonCode ?? item.IneligibilityReasonCode),
  };
}

function decodeShipment(row: unknown): ShipmentView | null {
  if (!row || typeof row !== "object") return null;
  const item = row as Record<string, unknown>;
  const operationId = asString(item.operationId ?? item.OperationId);
  const shippingGroupId = asString(item.shippingGroupId ?? item.ShippingGroupId);
  if (!operationId || !shippingGroupId) return null;
  return {
    operationId,
    shippingGroupId,
    state: (item.state ?? item.State ?? "Unknown") as ShipmentState,
    providerTrackingCode: asString(item.providerTrackingCode ?? item.ProviderTrackingCode),
    providerStatus: asString(item.providerStatus ?? item.ProviderStatus),
    lastErrorCode: asString(item.lastErrorCode ?? item.LastErrorCode),
    canTrack: item.canTrack === true || item.CanTrack === true,
    canCancel: item.canCancel === true || item.CanCancel === true,
  };
}

async function readJson(response: Response): Promise<unknown> {
  return response.json().catch(() => null);
}

function errorCode(payload: unknown): string | null {
  if (!payload || typeof payload !== "object") return null;
  const root = payload as Record<string, unknown>;
  const code = root.errorCode ?? root.ErrorCode;
  return typeof code === "string" ? code : null;
}

export function AdminOrderGroupCarrierExecution({
  checkoutId,
  apiPrefix,
  requestHeaders,
}: {
  checkoutId: string;
  apiPrefix: "/v1/admin" | "/v1/seller";
  requestHeaders: () => Record<string, string>;
}) {
  const messages = authoringMessages("fa");
  const [capabilities, setCapabilities] = useState<readonly CapabilityRow[]>([]);
  const [shipments, setShipments] = useState<readonly ShipmentView[]>([]);
  const [status, setStatus] = useState("");
  const [busyGroupId, setBusyGroupId] = useState<string | null>(null);
  const [senderByGroup, setSenderByGroup] = useState<
    Record<string, { name: string; mobile: string; address: string; postalCode: string }>
  >({});

  const carrierGroups = useMemo(
    () => capabilities.filter((row) => row.transportMode === "CarrierApi" || row.methodCode.includes("carrier")),
    [capabilities],
  );

  const blockMessage = useCallback(
    (code: string | null | undefined) => {
      switch (code) {
        case "payment_locked":
          return messages.carrierExecutionBlockPaymentLocked;
        case "cancelled":
          return messages.carrierExecutionBlockCancelled;
        case "legacy_incomplete":
          return messages.carrierExecutionBlockLegacyIncomplete;
        case "not_carrier_api":
          return messages.carrierExecutionBlockNotCarrierApi;
        case "package_incomplete":
          return messages.carrierExecutionBlockPackageIncomplete;
        case "owner_missing":
          return messages.carrierExecutionBlockOwnerMissing;
        default:
          return messages.carrierExecutionBlockGeneric;
      }
    },
    [messages],
  );

  const refresh = useCallback(async () => {
    if (!checkoutId) return;
    const headers = requestHeaders();
    const query = encodeURIComponent(checkoutId);
    const [capRes, shipRes] = await Promise.all([
      fetch(`${apiPrefix}/shipping-services/shipments/capabilities?checkoutId=${query}`, { headers }),
      fetch(`${apiPrefix}/shipping-services/shipments?checkoutId=${query}`, { headers }),
    ]);
    const capPayload = await readJson(capRes);
    const shipPayload = await readJson(shipRes);
    if (!capRes.ok) {
      setStatus(errorCode(capPayload) === "shipping.carrier.unconfigured" ? messages.carrierUnconfigured : messages.carrierSaveFailed);
      return;
    }
    const caps = (Array.isArray(capPayload) ? capPayload : [])
      .map(decodeCapability)
      .filter((row): row is CapabilityRow => row !== null);
    setCapabilities(caps);
    const ships = (Array.isArray(shipPayload) ? shipPayload : [])
      .map(decodeShipment)
      .filter((row): row is ShipmentView => row !== null);
    setShipments(ships);
    setStatus("");
  }, [apiPrefix, checkoutId, messages.carrierSaveFailed, messages.carrierUnconfigured, requestHeaders]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  async function post(path: string, body?: unknown): Promise<string | null> {
    const response = await fetch(path, {
      method: "POST",
      headers: { ...requestHeaders(), ...(body ? { "Content-Type": "application/json" } : {}) },
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

  if (carrierGroups.length === 0) {
    return null;
  }

  return (
    <section
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid="admin-order-group-carrier-execution"
      dir="rtl"
    >
      <div className="border-b border-gray-200 bg-gray-50/70 px-3 py-2.5">
        <h2 className="text-sm font-black text-gray-900">{messages.carrierExecutionTitle}</h2>
        <p className="mt-0.5 text-[11px] text-gray-500">{messages.carrierExecutionHint}</p>
      </div>
      {status ? (
        <p className="px-3 pt-2 text-xs font-semibold text-gray-700" data-testid="admin-order-carrier-execution-status">
          {status}
        </p>
      ) : null}
      <div className="space-y-3 p-3">
        {carrierGroups.map((group) => {
          const sender = senderByGroup[group.shippingGroupId] ?? {
            name: "",
            mobile: "",
            address: "",
            postalCode: group.postalCode ?? "",
          };
          const groupShipments = shipments.filter((s) => s.shippingGroupId === group.shippingGroupId);
          const busy = busyGroupId === group.shippingGroupId;
          return (
            <article
              key={group.shippingGroupId}
              className="rounded-lg border border-gray-100 bg-gray-50/50 p-3"
              data-testid={`admin-order-carrier-group-${group.shippingGroupId}`}
            >
              <h3 className="text-xs font-black text-gray-900">{group.methodLabel}</h3>
              <p className="mt-1 text-[11px] text-gray-600">
                {group.recipientName} · {group.cityName} · {group.originLocationCode ?? "—"} →{" "}
                {group.destinationLocationCode ?? "—"}
              </p>
              <p className="text-[11px] text-gray-500">
                {group.weightGrams?.toLocaleString("fa-IR") ?? "—"} g · {group.lengthCentimeters ?? "—"}×
                {group.widthCentimeters ?? "—"}×{group.heightCentimeters ?? "—"} cm
              </p>
              {!group.canExecuteCarrierShipment ? (
                <p
                  className="mt-2 rounded border border-amber-100 bg-amber-50 px-2 py-1.5 text-[11px] font-semibold text-amber-900"
                  data-testid={`admin-order-carrier-block-${group.shippingGroupId}`}
                >
                  {blockMessage(group.ineligibilityReasonCode)}
                </p>
              ) : (
                <div className="mt-2 grid gap-2 md:grid-cols-2">
                  <Field id={`sender-name-${group.shippingGroupId}`} label={messages.carrierAccountName}>
                    <Input
                      value={sender.name}
                      onChange={(e) =>
                        setSenderByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...sender, name: e.target.value },
                        }))
                      }
                    />
                  </Field>
                  <Field id={`sender-mobile-${group.shippingGroupId}`} label={messages.carrierUsername}>
                    <Input
                      value={sender.mobile}
                      onChange={(e) =>
                        setSenderByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...sender, mobile: e.target.value },
                        }))
                      }
                    />
                  </Field>
                  <Field id={`sender-address-${group.shippingGroupId}`} label={messages.carrierOriginCity}>
                    <Input
                      value={sender.address}
                      onChange={(e) =>
                        setSenderByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...sender, address: e.target.value },
                        }))
                      }
                    />
                  </Field>
                  <Field id={`sender-origin-${group.shippingGroupId}`} label={messages.carrierOriginCity}>
                    <Input value={group.originLocationCode ?? ""} readOnly dir="ltr" />
                  </Field>
                </div>
              )}
              {group.canExecuteCarrierShipment ? (
                <button
                  type="button"
                  className="mt-2 min-h-11 rounded-lg bg-[#2563EB] px-3 text-xs font-bold text-white disabled:opacity-50"
                  disabled={busy}
                  data-testid={`admin-order-carrier-create-${group.shippingGroupId}`}
                  onClick={async () => {
                    setBusyGroupId(group.shippingGroupId);
                    try {
                      const failed = await post(`${apiPrefix}/shipping-services/shipments`, {
                        checkoutId,
                        shippingGroupId: group.shippingGroupId,
                        carrierAccountId: group.carrierAccountId,
                        providerServiceId: group.providerServiceCode,
                        sender: {
                          fullName: sender.name,
                          mobile: sender.mobile,
                          addressLine: sender.address,
                          locationCode: group.originLocationCode,
                          postalCode: sender.postalCode,
                        },
                        recipient: {
                          fullName: group.recipientName,
                          mobile: group.recipientMobile,
                          addressLine: group.recipientAddress,
                          locationCode: group.destinationLocationCode,
                          postalCode: group.postalCode,
                        },
                        package: {
                          weightGrams: group.weightGrams,
                          declaredValue: group.declaredValue,
                          lengthCentimeters: group.lengthCentimeters,
                          widthCentimeters: group.widthCentimeters,
                          heightCentimeters: group.heightCentimeters,
                          collectAmount: 0,
                        },
                      });
                      setStatus(failed ?? messages.carrierAccountSaved);
                      if (!failed) await refresh();
                    } finally {
                      setBusyGroupId(null);
                    }
                  }}
                >
                  {messages.carrierShipmentCreate}
                </button>
              ) : null}
              {groupShipments.length === 0 ? (
                <p className="mt-2 text-[11px] text-gray-500">{messages.carrierShipmentEmpty}</p>
              ) : (
                <ul className="mt-2 space-y-2">
                  {groupShipments.map((row) => (
                    <li key={row.operationId} className="rounded-lg border border-gray-200 bg-white p-2 text-xs">
                      <p dir="ltr">{row.providerTrackingCode ?? row.operationId}</p>
                      <p>
                        {String(row.state)} {row.providerStatus ?? row.lastErrorCode ?? ""}
                      </p>
                      <div className="mt-2 flex flex-wrap gap-2">
                        <button
                          type="button"
                          className="min-h-11 rounded-lg border px-3"
                          disabled={busy || !row.canTrack || row.state === "Unknown"}
                          onClick={async () => {
                            setBusyGroupId(group.shippingGroupId);
                            try {
                              const failed = await post(
                                `${apiPrefix}/shipping-services/shipments/${row.operationId}/track`,
                              );
                              setStatus(failed ?? messages.carrierTestResult);
                              if (!failed) await refresh();
                            } finally {
                              setBusyGroupId(null);
                            }
                          }}
                        >
                          {messages.carrierShipmentTrack}
                        </button>
                        <button
                          type="button"
                          className="min-h-11 rounded-lg border px-3"
                          disabled={busy || !row.canCancel || row.state === "Unknown"}
                          onClick={async () => {
                            setBusyGroupId(group.shippingGroupId);
                            try {
                              const failed = await post(
                                `${apiPrefix}/shipping-services/shipments/${row.operationId}/cancel`,
                              );
                              setStatus(failed ?? messages.carrierTestResult);
                              if (!failed) await refresh();
                            } finally {
                              setBusyGroupId(null);
                            }
                          }}
                        >
                          {messages.carrierShipmentCancel}
                        </button>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}
