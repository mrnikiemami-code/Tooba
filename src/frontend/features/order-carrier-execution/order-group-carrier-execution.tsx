"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Field, Input } from "../../design-system/primitives/core";
import { authoringMessages } from "../admin-shipping-authoring/messages/authoring-messages.ts";
import { buildCarrierShipmentCreateBody } from "./build-carrier-shipment-create-body.ts";
import { ensurePackageDispatchOccurrence } from "./mint-package-dispatch-occurrence.ts";
import {
  decodeCapability,
  decodeShipment,
  isCarrierApiGroup,
  type CapabilityRow,
  type ShipmentView,
} from "./order-group-carrier-execution-decode.ts";
import {
  clearParcelOccurrenceLocator,
  readParcelOccurrenceLocator,
  writeParcelOccurrenceLocator,
} from "./parcel-occurrence-locator-store.ts";
import { resolveRetainedPackageDispatchId } from "./retain-package-dispatch-occurrence.ts";

export { buildCarrierShipmentCreateBody } from "./build-carrier-shipment-create-body.ts";
export { ensurePackageDispatchOccurrence, mintPackageDispatchOccurrenceId } from "./mint-package-dispatch-occurrence.ts";
export { decodeCapability, decodeShipment, isCarrierApiGroup, normalizeShipmentState } from "./order-group-carrier-execution-decode.ts";
export { resolveRetainedPackageDispatchId } from "./retain-package-dispatch-occurrence.ts";

async function readJson(response: Response): Promise<unknown> {
  return response.json().catch(() => null);
}

function errorCode(payload: unknown): string | null {
  if (!payload || typeof payload !== "object") return null;
  const root = payload as Record<string, unknown>;
  const code = root.errorCode ?? root.ErrorCode;
  return typeof code === "string" ? code : null;
}

type SenderDraft = { name: string; mobile: string; address: string; postalCode: string };
type PackageDraft = { weight: string; declared: string; length: string; width: string; height: string };

export function OrderGroupCarrierExecution({
  checkoutId,
  apiPrefix,
  requestHeaders,
  testIdPrefix = "order-group-carrier-execution",
}: {
  checkoutId: string;
  apiPrefix: "/v1/admin" | "/v1/seller";
  requestHeaders: () => Record<string, string>;
  testIdPrefix?: string;
}) {
  const messages = authoringMessages("fa");
  const [capabilities, setCapabilities] = useState<readonly CapabilityRow[]>([]);
  const [shipments, setShipments] = useState<readonly ShipmentView[]>([]);
  const [actionStatus, setActionStatus] = useState("");
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [busyLocks, setBusyLocks] = useState<Record<string, boolean>>({});
  const inFlightRef = useRef(new Set<string>());
  const refreshSeqRef = useRef(0);
  const requestHeadersRef = useRef(requestHeaders);
  requestHeadersRef.current = requestHeaders;
  const packageDispatchIdRef = useRef<Record<string, string>>({});
  const createUncertainRef = useRef<Record<string, boolean>>({});
  const shipmentsListOkRef = useRef(true);
  const [senderByGroup, setSenderByGroup] = useState<Record<string, SenderDraft>>({});
  const [packageByGroup, setPackageByGroup] = useState<Record<string, PackageDraft>>({});
  /** Parcel occurrence locator (client UuidV7 prebind or server-held) retained across retry / remount. */
  const [packageDispatchIdByGroup, setPackageDispatchIdByGroup] = useState<Record<string, string>>({});
  /** Uncertain create: response lost / ambiguous — block blind resend until explicit reconcile. */
  const [createUncertainByGroup, setCreateUncertainByGroup] = useState<Record<string, boolean>>({});
  const [shipmentsListOk, setShipmentsListOk] = useState(true);

  const carrierGroups = useMemo(
    () => capabilities.filter(isCarrierApiGroup),
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
        case "package_completion_required":
          return messages.carrierExecutionBlockPackageIncomplete;
        case "owner_missing":
          return messages.carrierExecutionBlockOwnerMissing;
        case "account_unconfigured":
        case "account_inactive":
          return messages.carrierUnconfigured;
        case "config_reconciliation_required":
          return messages.carrierExecutionBlockConfigReconciliation;
        case "sender_incomplete":
          return messages.carrierExecutionBlockSenderIncomplete;
        case "callbacks_missing":
          return messages.carrierExecutionBlockCallbacksMissing;
        case "operation_pending":
          return messages.carrierExecutionBlockOperationPending;
        case "operation_succeeded":
          return messages.carrierExecutionBlockOperationSucceeded;
        case "operation_unknown":
          return messages.carrierExecutionBlockOperationUnknown;
        case "fulfillment_unavailable":
          return messages.carrierExecutionBlockFulfillmentUnavailable;
        case "fulfillment_exhausted":
          return messages.carrierExecutionBlockFulfillmentExhausted;
        default:
          return messages.carrierExecutionBlockGeneric;
      }
    },
    [messages],
  );

  const shipmentStateLabel = useCallback(
    (state: ShipmentView["state"]) => {
      switch (state) {
        case "Pending":
          return messages.carrierShipmentStatePending;
        case "Succeeded":
          return messages.carrierShipmentStateSucceeded;
        case "Failed":
          return messages.carrierShipmentStateFailed;
        case "Unknown":
          return messages.carrierShipmentUnknown;
        default:
          return messages.carrierShipmentUnknown;
      }
    },
    [messages],
  );

  const syncBusy = useCallback((groupId: string, busy: boolean) => {
    if (busy) {
      inFlightRef.current.add(groupId);
    } else {
      inFlightRef.current.delete(groupId);
    }
    setBusyLocks((prev) => {
      const next = { ...prev };
      if (busy) next[groupId] = true;
      else delete next[groupId];
      return next;
    });
  }, []);

  const refresh = useCallback(async (signal?: AbortSignal): Promise<{ listOk: boolean }> => {
    if (!checkoutId) return { listOk: false };
    const seq = ++refreshSeqRef.current;
    setLoading(true);
    try {
      const headers = requestHeadersRef.current();
      const query = encodeURIComponent(checkoutId);
      const [capRes, shipRes] = await Promise.all([
        fetch(`${apiPrefix}/shipping-services/shipments/capabilities?checkoutId=${query}`, { headers, signal }),
        fetch(`${apiPrefix}/shipping-services/shipments?checkoutId=${query}`, { headers, signal }),
      ]);
      if (seq !== refreshSeqRef.current) return { listOk: shipmentsListOkRef.current };
      const capPayload = await readJson(capRes);
      const shipPayload = await readJson(shipRes);
      if (!capRes.ok) {
        setLoadError(
          errorCode(capPayload) === "shipping.carrier.unconfigured"
            ? messages.carrierUnconfigured
            : messages.carrierSaveFailed,
        );
        shipmentsListOkRef.current = false;
        setShipmentsListOk(false);
        return { listOk: false };
      }
      setLoadError(null);
      const caps = (Array.isArray(capPayload) ? capPayload : [])
        .map(decodeCapability)
        .filter((row): row is CapabilityRow => row !== null);
      setCapabilities(caps);
      setSenderByGroup((prev) => {
        const next = { ...prev };
        for (const row of caps) {
          if (next[row.shippingGroupId]) continue;
          next[row.shippingGroupId] = {
            name: row.configuredSender.fullName ?? "",
            mobile: row.configuredSender.mobile ?? "",
            address: row.configuredSender.addressLine ?? "",
            postalCode: row.configuredSender.postalCode ?? "",
          };
        }
        return next;
      });
      const listOk = shipRes.ok;
      shipmentsListOkRef.current = listOk;
      setShipmentsListOk(listOk);
      const ships = listOk
        ? (Array.isArray(shipPayload) ? shipPayload : [])
            .map(decodeShipment)
            .filter((row): row is ShipmentView => row !== null)
        : [];
      if (!listOk) {
        setActionStatus(messages.carrierShipmentListFailed);
      } else {
        setShipments(ships);
      }
      const prevIds = packageDispatchIdRef.current;
      const nextIds: Record<string, string> = { ...prevIds };
      const nextUncertain: Record<string, boolean> = { ...createUncertainRef.current };
      for (const row of caps) {
        const groupId = row.shippingGroupId;
        const stored = readParcelOccurrenceLocator(checkoutId, groupId);
        const seededCurrent =
          (typeof prevIds[groupId] === "string" && prevIds[groupId].trim().length > 0
            ? prevIds[groupId]
            : null)
          ?? stored?.occurrenceId
          ?? null;
        const priorUncertain =
          createUncertainRef.current[groupId] === true || stored?.uncertain === true;
        const groupShips = ships.filter((s) => s.shippingGroupId === groupId);
        const openShip =
          groupShips.find((s) => s.state === "Pending" || s.state === "Unknown")
          ?? groupShips[groupShips.length - 1]
          ?? null;
        const latest = row.latestCreateOperation;
        const retained = resolveRetainedPackageDispatchId({
          current: seededCurrent,
          canCreate: row.canCreate,
          openOccurrenceId:
            openShip?.packageDispatchId
            ?? latest?.packageDispatchId
            ?? null,
          openState: openShip?.state ?? latest?.state ?? null,
          createUncertain: priorUncertain,
          listReadOk: listOk,
        });
        // Uncertain survives remount/auto-refresh until explicit reconcile — list failure is not required.
        const stillUncertain = priorUncertain;
        if (stillUncertain) nextUncertain[groupId] = true;
        else delete nextUncertain[groupId];
        if (retained) {
          nextIds[groupId] = retained;
          writeParcelOccurrenceLocator(checkoutId, groupId, {
            occurrenceId: retained,
            uncertain: stillUncertain,
          });
        } else if (stillUncertain && seededCurrent) {
          // Never drop a prebound locator while uncertain, even if Succeeded+canCreate would clear.
          nextIds[groupId] = seededCurrent;
          writeParcelOccurrenceLocator(checkoutId, groupId, {
            occurrenceId: seededCurrent,
            uncertain: true,
          });
        } else {
          delete nextIds[groupId];
          clearParcelOccurrenceLocator(checkoutId, groupId);
        }
      }
      packageDispatchIdRef.current = nextIds;
      createUncertainRef.current = nextUncertain;
      setPackageDispatchIdByGroup(nextIds);
      setCreateUncertainByGroup(nextUncertain);
      return { listOk };
    } catch (err) {
      if ((err as { name?: string }).name !== "AbortError" && seq === refreshSeqRef.current) {
        setLoadError(messages.carrierSaveFailed);
        shipmentsListOkRef.current = false;
        setShipmentsListOk(false);
      }
      return { listOk: false };
    } finally {
      if (seq === refreshSeqRef.current) {
        setLoading(false);
      }
    }
  }, [apiPrefix, checkoutId, messages]);

  useEffect(() => {
    const controller = new AbortController();
    void refresh(controller.signal);
    return () => {
      controller.abort();
      refreshSeqRef.current += 1;
    };
  }, [refresh]);

  async function post(
    path: string,
    body?: unknown,
  ): Promise<{
    error: string | null;
    packageDispatchId: string | null;
    outcome: "succeeded" | "refused" | "unknown";
  }> {
    const response = await fetch(path, {
      method: "POST",
      headers: { ...requestHeaders(), ...(body ? { "Content-Type": "application/json" } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
    const payload = await readJson(response);
    if (!response.ok) {
      const code = errorCode(payload);
      if (code === "shipping.carrier.unconfigured") {
        return { error: messages.carrierUnconfigured, packageDispatchId: null, outcome: "refused" };
      }
      if (code === "shipping.carrier.shipment.unsupported") {
        return { error: messages.carrierShipmentUnsupported, packageDispatchId: null, outcome: "refused" };
      }
      if (code === "shipping.carrier.shipment.unknown") {
        return { error: messages.carrierShipmentUnknown, packageDispatchId: null, outcome: "unknown" };
      }
      if (code === "shipping.carrier.shipment.intent_conflict") {
        return { error: messages.carrierSaveFailed, packageDispatchId: null, outcome: "refused" };
      }
      return { error: messages.carrierSaveFailed, packageDispatchId: null, outcome: "refused" };
    }
    const decoded = decodeShipment(payload);
    return {
      error: null,
      packageDispatchId: decoded?.packageDispatchId ?? null,
      outcome: "succeeded",
    };
  }

  function markCreateUncertain(groupId: string, occurrenceId: string) {
    createUncertainRef.current = { ...createUncertainRef.current, [groupId]: true };
    packageDispatchIdRef.current = { ...packageDispatchIdRef.current, [groupId]: occurrenceId };
    setCreateUncertainByGroup(createUncertainRef.current);
    setPackageDispatchIdByGroup(packageDispatchIdRef.current);
    writeParcelOccurrenceLocator(checkoutId, groupId, { occurrenceId, uncertain: true });
  }

  function clearCreateUncertain(groupId: string, occurrenceId: string | null) {
    const nextUncertain = { ...createUncertainRef.current };
    delete nextUncertain[groupId];
    createUncertainRef.current = nextUncertain;
    setCreateUncertainByGroup(nextUncertain);
    if (occurrenceId) {
      writeParcelOccurrenceLocator(checkoutId, groupId, { occurrenceId, uncertain: false });
    } else {
      clearParcelOccurrenceLocator(checkoutId, groupId);
    }
  }

  if (loadError) {
    return (
      <section
        className="overflow-hidden rounded-xl border border-red-200 bg-red-50 p-3 text-xs font-semibold text-red-900"
        data-testid={`${testIdPrefix}-error`}
        dir="rtl"
      >
        {loadError}
      </section>
    );
  }

  if (loading && carrierGroups.length === 0) {
    return (
      <section className="rounded-xl border border-gray-200 bg-white p-3 text-xs text-gray-600" data-testid={`${testIdPrefix}-loading`} dir="rtl">
        {messages.carrierExecutionLoading}
      </section>
    );
  }

  if (carrierGroups.length === 0) {
    return null;
  }

  return (
    <section
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid={testIdPrefix}
      dir="rtl"
    >
      <div className="border-b border-gray-200 bg-gray-50/70 px-3 py-2.5">
        <h2 className="text-sm font-black text-gray-900">{messages.carrierExecutionTitle}</h2>
        <p className="mt-0.5 text-[11px] text-gray-500">{messages.carrierExecutionHint}</p>
      </div>
      {actionStatus ? (
        <p className="px-3 pt-2 text-xs font-semibold text-gray-700" data-testid={`${testIdPrefix}-status`}>
          {actionStatus}
        </p>
      ) : null}
      <div className="space-y-3 p-3">
        {carrierGroups.map((group) => {
          const sender = senderByGroup[group.shippingGroupId] ?? {
            name: group.configuredSender.fullName ?? "",
            mobile: group.configuredSender.mobile ?? "",
            address: group.configuredSender.addressLine ?? "",
            postalCode: group.configuredSender.postalCode ?? "",
          };
          const pkg = packageByGroup[group.shippingGroupId] ?? {
            weight: group.weightGrams?.toString() ?? "",
            declared: group.declaredValue?.toString() ?? "",
            length: group.lengthCentimeters?.toString() ?? "",
            width: group.widthCentimeters?.toString() ?? "",
            height: group.heightCentimeters?.toString() ?? "",
          };
          const groupShipments = shipments.filter((s) => s.shippingGroupId === group.shippingGroupId);
          const busy = busyLocks[group.shippingGroupId] === true;
          const primaryBlock = group.ineligibilityReasonCode ?? group.blockReasonCodes[0] ?? null;
          return (
            <article
              key={group.shippingGroupId}
              className="rounded-lg border border-gray-100 bg-gray-50/50 p-3"
              data-testid={`${testIdPrefix}-group-${group.shippingGroupId}`}
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
              {!group.canCreate ? (
                <p
                  className="mt-2 rounded border border-amber-100 bg-amber-50 px-2 py-1.5 text-[11px] font-semibold text-amber-900"
                  data-testid={`${testIdPrefix}-block-${group.shippingGroupId}`}
                >
                  {blockMessage(primaryBlock)}
                </p>
              ) : null}
              {group.allowPackageCompletion ? (
                <div className="mt-2 grid gap-2 md:grid-cols-3">
                  <Field id={`pkg-weight-${group.shippingGroupId}`} label={messages.carrierPackageWeight}>
                    <Input
                      value={pkg.weight}
                      onChange={(e) =>
                        setPackageByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...pkg, weight: e.target.value },
                        }))
                      }
                      dir="ltr"
                    />
                  </Field>
                  <Field id={`pkg-l-${group.shippingGroupId}`} label={messages.carrierPackageLength}>
                    <Input
                      value={pkg.length}
                      onChange={(e) =>
                        setPackageByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...pkg, length: e.target.value },
                        }))
                      }
                      dir="ltr"
                    />
                  </Field>
                  <Field id={`pkg-w-${group.shippingGroupId}`} label={messages.carrierPackageWidth}>
                    <Input
                      value={pkg.width}
                      onChange={(e) =>
                        setPackageByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...pkg, width: e.target.value },
                        }))
                      }
                      dir="ltr"
                    />
                  </Field>
                  <Field id={`pkg-h-${group.shippingGroupId}`} label={messages.carrierPackageHeight}>
                    <Input
                      value={pkg.height}
                      onChange={(e) =>
                        setPackageByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...pkg, height: e.target.value },
                        }))
                      }
                      dir="ltr"
                    />
                  </Field>
                  <Field id={`pkg-val-${group.shippingGroupId}`} label={messages.carrierPackageDeclaredValue}>
                    <Input
                      value={pkg.declared}
                      onChange={(e) =>
                        setPackageByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...pkg, declared: e.target.value },
                        }))
                      }
                      dir="ltr"
                    />
                  </Field>
                  <button
                    type="button"
                    className="min-h-11 self-end rounded-lg border px-3 text-xs font-bold"
                    disabled={busy}
                    data-testid={`${testIdPrefix}-package-completion-${group.shippingGroupId}`}
                    onClick={async () => {
                      const groupId = group.shippingGroupId;
                      if (inFlightRef.current.has(groupId)) return;
                      syncBusy(groupId, true);
                      try {
                        const result = await post(`${apiPrefix}/shipping-services/shipments/package-completion`, {
                          checkoutId,
                          shippingGroupId: groupId,
                          weightGrams: Number(pkg.weight),
                          declaredValue: Number(pkg.declared || 0),
                          lengthCentimeters: Number(pkg.length),
                          widthCentimeters: Number(pkg.width),
                          heightCentimeters: Number(pkg.height),
                        });
                        setActionStatus(result.error ?? messages.carrierPackageCompletionSaved);
                        if (!result.error) await refresh();
                      } catch {
                        setActionStatus(messages.carrierActionFailed);
                      } finally {
                        syncBusy(groupId, false);
                      }
                    }}
                  >
                    {messages.carrierPackageCompletionSave}
                  </button>
                </div>
              ) : null}
              {group.canCreate || group.hasEditableSenderContact || group.hasEditableSenderPostal ? (
                <div className="mt-2 grid gap-2 md:grid-cols-2">
                  <Field id={`sender-name-${group.shippingGroupId}`} label={messages.carrierSenderName}>
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
                  <Field id={`sender-mobile-${group.shippingGroupId}`} label={messages.carrierSenderMobile}>
                    <Input
                      value={sender.mobile}
                      onChange={(e) =>
                        setSenderByGroup((prev) => ({
                          ...prev,
                          [group.shippingGroupId]: { ...sender, mobile: e.target.value },
                        }))
                      }
                      dir="ltr"
                    />
                  </Field>
                  <Field id={`sender-address-${group.shippingGroupId}`} label={messages.carrierSenderAddress}>
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
                  {group.hasEditableSenderPostal ? (
                    <Field id={`sender-postal-${group.shippingGroupId}`} label={messages.carrierSenderPostalCode}>
                      <Input
                        value={sender.postalCode}
                        onChange={(e) =>
                          setSenderByGroup((prev) => ({
                            ...prev,
                            [group.shippingGroupId]: { ...sender, postalCode: e.target.value },
                          }))
                        }
                        dir="ltr"
                      />
                    </Field>
                  ) : null}
                  <Field id={`sender-origin-${group.shippingGroupId}`} label={messages.carrierOriginCity}>
                    <Input value={group.originLocationCode ?? ""} readOnly dir="ltr" />
                  </Field>
                  {group.hasEditableSenderContact || group.hasEditableSenderPostal ? (
                    <button
                      type="button"
                      className="min-h-11 self-end rounded-lg border px-3 text-xs font-bold"
                      disabled={busy}
                      data-testid={`${testIdPrefix}-sender-completion-${group.shippingGroupId}`}
                      onClick={async () => {
                        const groupId = group.shippingGroupId;
                        if (inFlightRef.current.has(groupId)) return;
                        syncBusy(groupId, true);
                        try {
                          const result = await post(`${apiPrefix}/shipping-services/shipments/sender-completion`, {
                            checkoutId,
                            shippingGroupId: groupId,
                            fullName: sender.name,
                            mobile: sender.mobile,
                            addressLine: sender.address,
                            postalCode: sender.postalCode,
                          });
                          setActionStatus(result.error ?? messages.carrierSenderCompletionSaved);
                          if (!result.error) await refresh();
                        } catch {
                          setActionStatus(messages.carrierActionFailed);
                        } finally {
                          syncBusy(groupId, false);
                        }
                      }}
                    >
                      {messages.carrierSenderCompletionSave}
                    </button>
                  ) : null}
                </div>
              ) : null}
              {group.canCreate ? (
                <div className="mt-2 space-y-2">
                  {!group.allowPackageCompletion ? (
                    <div className="grid gap-2 md:grid-cols-4" data-testid={`${testIdPrefix}-dispatch-package-${group.shippingGroupId}`}>
                      <Field id={`dispatch-weight-${group.shippingGroupId}`} label={messages.carrierPackageWeight}>
                        <Input
                          value={pkg.weight}
                          onChange={(e) =>
                            setPackageByGroup((prev) => ({
                              ...prev,
                              [group.shippingGroupId]: { ...pkg, weight: e.target.value },
                            }))
                          }
                          dir="ltr"
                          data-testid={`${testIdPrefix}-dispatch-weight-${group.shippingGroupId}`}
                        />
                      </Field>
                      <Field id={`dispatch-l-${group.shippingGroupId}`} label={messages.carrierPackageLength}>
                        <Input
                          value={pkg.length}
                          onChange={(e) =>
                            setPackageByGroup((prev) => ({
                              ...prev,
                              [group.shippingGroupId]: { ...pkg, length: e.target.value },
                            }))
                          }
                          dir="ltr"
                        />
                      </Field>
                      <Field id={`dispatch-w-${group.shippingGroupId}`} label={messages.carrierPackageWidth}>
                        <Input
                          value={pkg.width}
                          onChange={(e) =>
                            setPackageByGroup((prev) => ({
                              ...prev,
                              [group.shippingGroupId]: { ...pkg, width: e.target.value },
                            }))
                          }
                          dir="ltr"
                        />
                      </Field>
                      <Field id={`dispatch-h-${group.shippingGroupId}`} label={messages.carrierPackageHeight}>
                        <Input
                          value={pkg.height}
                          onChange={(e) =>
                            setPackageByGroup((prev) => ({
                              ...prev,
                              [group.shippingGroupId]: { ...pkg, height: e.target.value },
                            }))
                          }
                          dir="ltr"
                        />
                      </Field>
                    </div>
                  ) : null}
                  <input
                    type="hidden"
                    data-testid={`${testIdPrefix}-package-dispatch-id-${group.shippingGroupId}`}
                    value={packageDispatchIdByGroup[group.shippingGroupId] ?? ""}
                    readOnly
                  />
                  {createUncertainByGroup[group.shippingGroupId] ? (
                    <div className="space-y-2" data-testid={`${testIdPrefix}-create-uncertain-${group.shippingGroupId}`}>
                      <p className="text-[11px] font-semibold text-amber-900">
                        {messages.carrierCreateOutcomeUncertain}
                      </p>
                      <p className="text-[11px] text-gray-500">{messages.carrierCreateReconcileHint}</p>
                      <button
                        type="button"
                        className="min-h-11 rounded-lg border px-3 text-xs font-bold"
                        disabled={busy}
                        data-testid={`${testIdPrefix}-reconcile-${group.shippingGroupId}`}
                        onClick={async () => {
                          const groupId = group.shippingGroupId;
                          if (inFlightRef.current.has(groupId)) return;
                          syncBusy(groupId, true);
                          try {
                            const result = await refresh();
                            if (!result.listOk) {
                              setActionStatus(messages.carrierShipmentListFailed);
                              return;
                            }
                            const kept =
                              packageDispatchIdRef.current[groupId]
                              ?? packageDispatchIdByGroup[groupId]
                              ?? null;
                            clearCreateUncertain(groupId, kept);
                            setActionStatus(messages.carrierCreateReconcile);
                          } catch {
                            setActionStatus(messages.carrierShipmentListFailed);
                          } finally {
                            syncBusy(groupId, false);
                          }
                        }}
                      >
                        {messages.carrierCreateReconcile}
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      className="min-h-11 rounded-lg bg-[#2563EB] px-3 text-xs font-bold text-white disabled:opacity-50"
                      disabled={busy}
                      data-testid={`${testIdPrefix}-create-${group.shippingGroupId}`}
                      onClick={async () => {
                        const groupId = group.shippingGroupId;
                        if (inFlightRef.current.has(groupId)) return;
                        if (createUncertainRef.current[groupId]) return;
                        syncBusy(groupId, true);
                        const occurrence = ensurePackageDispatchOccurrence(
                          packageDispatchIdRef.current[groupId]
                            ?? packageDispatchIdByGroup[groupId]
                            ?? readParcelOccurrenceLocator(checkoutId, groupId)?.occurrenceId,
                        );
                        packageDispatchIdRef.current = {
                          ...packageDispatchIdRef.current,
                          [groupId]: occurrence,
                        };
                        setPackageDispatchIdByGroup(packageDispatchIdRef.current);
                        writeParcelOccurrenceLocator(checkoutId, groupId, {
                          occurrenceId: occurrence,
                          uncertain: false,
                        });
                        try {
                          const executable = group.executableLines ?? [];
                          const lines = executable
                            .filter((line) => typeof line.orderLineId === "string" && Number(line.remainingPackedQuantity) > 0)
                            .map((line) => ({
                              orderLineId: line.orderLineId,
                              quantity: line.remainingPackedQuantity,
                            }));
                          const weight = Number(pkg.weight);
                          const length = Number(pkg.length);
                          const width = Number(pkg.width);
                          const height = Number(pkg.height);
                          const body = buildCarrierShipmentCreateBody({
                            checkoutId,
                            shippingGroupId: groupId,
                            lines,
                            executableLines: executable,
                            package:
                              weight > 0 && length > 0 && width > 0 && height > 0
                                ? {
                                    weightGrams: weight,
                                    lengthCentimeters: length,
                                    widthCentimeters: width,
                                    heightCentimeters: height,
                                  }
                                : null,
                            packageDispatchId: occurrence,
                          });
                          delete body.partialWithoutPackage;
                          const result = await post(`${apiPrefix}/shipping-services/shipments`, body);
                          if (result.outcome === "unknown") {
                            markCreateUncertain(groupId, occurrence);
                            setActionStatus(result.error ?? messages.carrierCreateOutcomeUncertain);
                            return;
                          }
                          if (result.packageDispatchId) {
                            packageDispatchIdRef.current = {
                              ...packageDispatchIdRef.current,
                              [groupId]: result.packageDispatchId,
                            };
                            setPackageDispatchIdByGroup(packageDispatchIdRef.current);
                            writeParcelOccurrenceLocator(checkoutId, groupId, {
                              occurrenceId: result.packageDispatchId,
                              uncertain: false,
                            });
                          }
                          clearCreateUncertain(groupId, result.packageDispatchId ?? occurrence);
                          setActionStatus(result.error ?? messages.carrierAccountSaved);
                          if (!result.error) await refresh();
                        } catch {
                          markCreateUncertain(groupId, occurrence);
                          setActionStatus(messages.carrierCreateOutcomeUncertain);
                        } finally {
                          syncBusy(groupId, false);
                        }
                      }}
                    >
                      {messages.carrierShipmentCreate}
                    </button>
                  )}
                </div>
              ) : null}
              {groupShipments.length === 0 ? (
                <p className="mt-2 text-[11px] text-gray-500">{messages.carrierShipmentEmpty}</p>
              ) : (
                <ul className="mt-2 space-y-2">
                  {groupShipments.map((row) => (
                    <li key={row.operationId} className="rounded-lg border border-gray-200 bg-white p-2 text-xs">
                      <p dir="ltr">{row.providerTrackingCode ?? row.operationId}</p>
                      <p>
                        {shipmentStateLabel(row.state)}{" "}
                        {row.providerStatus ?? row.lastErrorCode ?? ""}
                      </p>
                      <div className="mt-2 flex flex-wrap gap-2">
                        <button
                          type="button"
                          className="min-h-11 rounded-lg border px-3"
                          disabled={busy || !row.canTrack}
                          onClick={async () => {
                            const groupId = group.shippingGroupId;
                            if (inFlightRef.current.has(groupId)) return;
                            syncBusy(groupId, true);
                            try {
                              const result = await post(
                                `${apiPrefix}/shipping-services/shipments/${row.operationId}/track`,
                              );
                              setActionStatus(result.error ?? messages.carrierTestResult);
                              if (!result.error) await refresh();
                            } catch {
                              setActionStatus(messages.carrierActionFailed);
                            } finally {
                              syncBusy(groupId, false);
                            }
                          }}
                        >
                          {messages.carrierShipmentTrack}
                        </button>
                        <button
                          type="button"
                          className="min-h-11 rounded-lg border px-3"
                          disabled={busy || !row.canCancel}
                          onClick={async () => {
                            const groupId = group.shippingGroupId;
                            if (inFlightRef.current.has(groupId)) return;
                            syncBusy(groupId, true);
                            try {
                              const result = await post(
                                `${apiPrefix}/shipping-services/shipments/${row.operationId}/cancel`,
                              );
                              setActionStatus(result.error ?? messages.carrierTestResult);
                              if (!result.error) await refresh();
                            } catch {
                              setActionStatus(messages.carrierActionFailed);
                            } finally {
                              syncBusy(groupId, false);
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
