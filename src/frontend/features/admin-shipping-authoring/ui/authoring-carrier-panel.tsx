"use client";

import { useEffect, useMemo, useState } from "react";
import { Field, Input, Select } from "../../../design-system";
import type { AuthoringMessages } from "../messages/authoring-messages.ts";
import {
  createCarrierAccount,
  getCarrierBinding,
  listCarrierAccounts,
  listCarrierDestinationMappings,
  listCarrierReferences,
  replaceCarrierBinding,
  syncCarrierReferences,
  testCarrierConnection,
  upsertCarrierDestinationMapping,
  type CarrierAccountView,
  type CarrierBindingView,
  type CarrierDestinationMappingView,
  type CarrierReferenceView,
} from "../api/carrier-authoring-api.ts";

const REF_KIND = {
  service: 0,
  payment: 1,
  pickup: 2,
  distribution: 3,
  packing: 4,
  content: 5,
  packType: 6,
} as const;

export function AuthoringCarrierPanel({
  draftId,
  revision,
  readOnly,
  messages,
}: {
  draftId: string;
  revision: number;
  readOnly: boolean;
  messages: AuthoringMessages;
}) {
  const [accounts, setAccounts] = useState<readonly CarrierAccountView[]>([]);
  const [refs, setRefs] = useState<readonly CarrierReferenceView[]>([]);
  const [binding, setBinding] = useState<CarrierBindingView | null>(null);
  const [status, setStatus] = useState<string>("");
  const [busy, setBusy] = useState(false);
  const [displayName, setDisplayName] = useState("Tipax");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [accountId, setAccountId] = useState("");
  const [providerServiceId, setProviderServiceId] = useState("");
  const [paymentTypeId, setPaymentTypeId] = useState("");
  const [pickupTypeId, setPickupTypeId] = useState("");
  const [distributionTypeId, setDistributionTypeId] = useState("");
  const [packingId, setPackingId] = useState("");
  const [packageContentId, setPackageContentId] = useState("");
  const [packTypeId, setPackTypeId] = useState("");
  const [originExternalCityCode, setOriginExternalCityCode] = useState("");
  const [lengthCm, setLengthCm] = useState("20");
  const [widthCm, setWidthCm] = useState("15");
  const [heightCm, setHeightCm] = useState("10");
  const [mappings, setMappings] = useState<readonly CarrierDestinationMappingView[]>([]);
  const [geographyCityId, setGeographyCityId] = useState("");
  const [externalLocationCode, setExternalLocationCode] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void (async () => {
      const listed = await listCarrierAccounts(controller.signal);
      if (listed.kind === "ok") setAccounts(listed.value);
      const current = await getCarrierBinding(draftId, controller.signal);
      if (current.kind === "ok" && current.value) {
        applyBinding(current.value);
        await refreshRefs(current.value.carrierAccountId);
      }
    })();
    return () => controller.abort();
  }, [draftId]);

  function applyBinding(value: CarrierBindingView) {
    setBinding(value);
    setAccountId(value.carrierAccountId);
    setProviderServiceId(value.providerServiceId);
    setPaymentTypeId(value.paymentTypeId);
    setPickupTypeId(value.pickupTypeId);
    setDistributionTypeId(value.distributionTypeId);
    setPackingId(value.packingId);
    setPackageContentId(value.packageContentId);
    setPackTypeId(value.packTypeId);
    setOriginExternalCityCode(value.originExternalCityCode);
    setLengthCm(String(value.defaultLengthCentimeters));
    setWidthCm(String(value.defaultWidthCentimeters));
    setHeightCm(String(value.defaultHeightCentimeters));
  }

  async function refreshRefs(id: string) {
    const listed = await listCarrierReferences(id);
    if (listed.kind === "ok") setRefs(listed.value);
    const mapped = await listCarrierDestinationMappings(id);
    if (mapped.kind === "ok") setMappings(mapped.value);
  }

  const options = useMemo(() => {
    const byKind = (kind: number) => refs.filter((row) => row.kind === kind && row.isActive);
    return {
      service: byKind(REF_KIND.service),
      payment: byKind(REF_KIND.payment),
      pickup: byKind(REF_KIND.pickup),
      distribution: byKind(REF_KIND.distribution),
      packing: byKind(REF_KIND.packing),
      content: byKind(REF_KIND.content),
      packType: byKind(REF_KIND.packType),
    };
  }, [refs]);

  return (
    <section className="grid gap-3 rounded-2xl border border-border bg-surface p-3 md:p-4" data-testid="shipping-authoring-carrier-panel">
      <h3 className="text-sm font-black text-gray-900">{messages.carrierTitle}</h3>
      <p className="text-[11px] text-gray-500">{messages.carrierIntro}</p>
      {status ? <p className="text-xs font-semibold text-gray-700">{status}</p> : null}

      <div className="grid gap-2 md:grid-cols-2">
        <Field id="shipping-authoring-carrier-name" label={messages.carrierAccountName}>
          <Input value={displayName} disabled={readOnly || busy} onChange={(event) => setDisplayName(event.target.value)} data-testid="shipping-authoring-carrier-name" />
        </Field>
        <Field id="shipping-authoring-carrier-username" label={messages.carrierUsername}>
          <Input value={username} disabled={readOnly || busy} onChange={(event) => setUsername(event.target.value)} data-testid="shipping-authoring-carrier-username" />
        </Field>
        <Field id="shipping-authoring-carrier-password" label={messages.carrierPassword}>
          <Input type="password" value={password} disabled={readOnly || busy} onChange={(event) => setPassword(event.target.value)} data-testid="shipping-authoring-carrier-password" />
        </Field>
      </div>
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          className="min-h-11 rounded-lg bg-primary px-3 text-sm font-bold text-primary-foreground"
          disabled={readOnly || busy}
          data-testid="shipping-authoring-carrier-create"
          onClick={async () => {
            setBusy(true);
            const created = await createCarrierAccount({ displayName, username, password, environment: "Test", isActive: true });
            setBusy(false);
            if (created.kind !== "ok") {
              setStatus(
                created.kind === "failure" && "errorCode" in created.failure && created.failure.errorCode === "shipping.carrier.unconfigured"
                  ? messages.carrierUnconfigured
                  : messages.carrierSaveFailed,
              );
              return;
            }
            setAccounts((rows) => [...rows, created.value]);
            setAccountId(created.value.accountId);
            setStatus(messages.carrierAccountSaved);
          }}
        >
          {messages.carrierCreateAccount}
        </button>
        <button
          type="button"
          className="min-h-11 rounded-lg border border-border px-3 text-sm font-semibold"
          disabled={readOnly || busy || !accountId}
          data-testid="shipping-authoring-carrier-test"
          onClick={async () => {
            setBusy(true);
            const tested = await testCarrierConnection(accountId);
            setBusy(false);
            setStatus(
              tested.kind === "ok"
                ? `${messages.carrierTestResult}: ${tested.value}`
                : tested.kind === "failure" && "errorCode" in tested.failure && tested.failure.errorCode === "shipping.carrier.unconfigured"
                  ? messages.carrierUnconfigured
                  : messages.carrierSaveFailed,
            );
          }}
        >
          {messages.carrierTestConnection}
        </button>
        <button
          type="button"
          className="min-h-11 rounded-lg border border-border px-3 text-sm font-semibold"
          disabled={readOnly || busy || !accountId}
          data-testid="shipping-authoring-carrier-sync"
          onClick={async () => {
            setBusy(true);
            const synced = await syncCarrierReferences(accountId);
            setBusy(false);
            if (synced.kind !== "ok") {
              setStatus(messages.carrierSaveFailed);
              return;
            }
            setRefs(synced.value);
            setStatus(messages.carrierSynced);
          }}
        >
          {messages.carrierSyncReferences}
        </button>
      </div>

      <Field id="shipping-authoring-carrier-account" label={messages.carrierSelectAccount}>
        <Select
          value={accountId}
          disabled={readOnly || busy}
          data-testid="shipping-authoring-carrier-account"
          onChange={(event) => {
            const next = event.target.value;
            setAccountId(next);
            if (next) void refreshRefs(next);
          }}
        >
          <option value="">{messages.carrierSelectAccount}</option>
          {accounts.map((account) => (
            <option key={account.accountId} value={account.accountId}>
              {account.displayName}
            </option>
          ))}
        </Select>
      </Field>

      <RefField label={messages.carrierService} value={providerServiceId} options={options.service} disabled={readOnly || busy} onChange={setProviderServiceId} testId="shipping-authoring-carrier-service" />
      <RefField label={messages.carrierPayment} value={paymentTypeId} options={options.payment} disabled={readOnly || busy} onChange={setPaymentTypeId} testId="shipping-authoring-carrier-payment" />
      <RefField label={messages.carrierPickup} value={pickupTypeId} options={options.pickup} disabled={readOnly || busy} onChange={setPickupTypeId} testId="shipping-authoring-carrier-pickup" />
      <RefField label={messages.carrierDistribution} value={distributionTypeId} options={options.distribution} disabled={readOnly || busy} onChange={setDistributionTypeId} testId="shipping-authoring-carrier-distribution" />
      <RefField label={messages.carrierPacking} value={packingId} options={options.packing} disabled={readOnly || busy} onChange={setPackingId} testId="shipping-authoring-carrier-packing" />
      <RefField label={messages.carrierContent} value={packageContentId} options={options.content} disabled={readOnly || busy} onChange={setPackageContentId} testId="shipping-authoring-carrier-content" />
      <RefField label={messages.carrierPackType} value={packTypeId} options={options.packType} disabled={readOnly || busy} onChange={setPackTypeId} testId="shipping-authoring-carrier-pack-type" />

      <Field id="shipping-authoring-carrier-origin" label={messages.carrierOriginCity}>
        <Input value={originExternalCityCode} disabled={readOnly || busy} onChange={(event) => setOriginExternalCityCode(event.target.value)} data-testid="shipping-authoring-carrier-origin" />
      </Field>
      <div className="grid gap-2">
        <p className="text-xs font-bold text-gray-800">{messages.carrierMappingsTitle}</p>
        <div className="grid gap-2 md:grid-cols-2">
          <Field id="shipping-authoring-carrier-geo-city" label={messages.carrierDestinationCity}>
            <Input value={geographyCityId} disabled={readOnly || busy} onChange={(event) => setGeographyCityId(event.target.value)} data-testid="shipping-authoring-carrier-geo-city" />
          </Field>
          <Field id="shipping-authoring-carrier-dest-code" label={messages.carrierDestinationCode}>
            <Input value={externalLocationCode} disabled={readOnly || busy} onChange={(event) => setExternalLocationCode(event.target.value)} data-testid="shipping-authoring-carrier-dest-code" />
          </Field>
        </div>
        <button
          type="button"
          className="min-h-11 rounded-lg border border-border px-3 text-sm font-semibold"
          disabled={readOnly || busy || !accountId}
          data-testid="shipping-authoring-carrier-save-mapping"
          onClick={async () => {
            setBusy(true);
            const saved = await upsertCarrierDestinationMapping(accountId, { geographyCityId, externalLocationCode });
            setBusy(false);
            if (saved.kind !== "ok") {
              setStatus(messages.carrierSaveFailed);
              return;
            }
            setMappings((rows) => {
              const rest = rows.filter((row) => row.geographyCityId !== saved.value.geographyCityId);
              return [...rest, saved.value];
            });
            setStatus(messages.carrierMappingSaved);
          }}
        >
          {messages.carrierSaveMapping}
        </button>
        {mappings.length > 0 ? (
          <ul className="grid gap-1 text-[11px] text-gray-600" data-testid="shipping-authoring-carrier-mapping-list">
            {mappings.map((row) => (
              <li key={row.mappingId}>{`${row.geographyCityId} → ${row.externalLocationCode}`}</li>
            ))}
          </ul>
        ) : null}
      </div>
      <div className="grid grid-cols-3 gap-2">
        <Field id="shipping-authoring-carrier-length" label={messages.carrierLength}>
          <Input value={lengthCm} disabled={readOnly || busy} onChange={(event) => setLengthCm(event.target.value)} />
        </Field>
        <Field id="shipping-authoring-carrier-width" label={messages.carrierWidth}>
          <Input value={widthCm} disabled={readOnly || busy} onChange={(event) => setWidthCm(event.target.value)} />
        </Field>
        <Field id="shipping-authoring-carrier-height" label={messages.carrierHeight}>
          <Input value={heightCm} disabled={readOnly || busy} onChange={(event) => setHeightCm(event.target.value)} />
        </Field>
      </div>

      <button
        type="button"
        className="min-h-11 w-full rounded-lg bg-primary px-3 text-sm font-bold text-primary-foreground"
        disabled={readOnly || busy || !accountId}
        data-testid="shipping-authoring-carrier-save-binding"
        onClick={async () => {
          setBusy(true);
          const saved = await replaceCarrierBinding(draftId, {
            expectedRevision: revision,
            carrierAccountId: accountId,
            providerServiceId,
            paymentTypeId,
            pickupTypeId,
            distributionTypeId,
            packingId,
            packageContentId,
            packTypeId,
            originExternalCityCode,
            originAddressLine: null,
            defaultLengthCentimeters: Number(lengthCm),
            defaultWidthCentimeters: Number(widthCm),
            defaultHeightCentimeters: Number(heightCm),
          });
          setBusy(false);
          if (saved.kind !== "ok") {
            setStatus(messages.carrierSaveFailed);
            return;
          }
          applyBinding(saved.value);
          setStatus(messages.carrierBindingSaved);
        }}
      >
        {messages.carrierSaveBinding}
      </button>
      {binding ? <p className="text-[11px] text-gray-500">{messages.carrierBindingPresent}</p> : null}
    </section>
  );
}

function RefField({
  label,
  value,
  options,
  disabled,
  onChange,
  testId,
}: {
  label: string;
  value: string;
  options: readonly CarrierReferenceView[];
  disabled: boolean;
  onChange: (value: string) => void;
  testId: string;
}) {
  return (
    <Field id={testId} label={label}>
      {options.length > 0 ? (
        <Select value={value} disabled={disabled} data-testid={testId} onChange={(event) => onChange(event.target.value)}>
          <option value="">{label}</option>
          {options.map((option) => (
            <option key={option.externalId} value={option.externalId}>
              {option.title}
            </option>
          ))}
        </Select>
      ) : (
        <Input value={value} disabled={disabled} data-testid={testId} onChange={(event) => onChange(event.target.value)} />
      )}
    </Field>
  );
}
