/**
 * Authorized CarrierApi account/binding transport for the shipping authoring general tab.
 *
 * Uses the same admin headers as the editor document; the browser never supplies owner authority.
 */
import { authoringHeaders, editorTransportFailureFor, parseResponseRoot, type EditorTransportFailure } from "./editor-document-api.ts";
import type { JsonNode } from "./wire-json.ts";

export type CarrierEnvironment = "Test" | "Production";
export type CarrierConnectionOutcome =
  | "Succeeded"
  | "Disabled"
  | "SecretMissing"
  | "Unauthorized"
  | "TemporarilyUnavailable"
  | "ProviderRejected";

export interface CarrierAccountView {
  readonly accountId: string;
  readonly providerCode: string;
  readonly displayName: string;
  readonly environment: CarrierEnvironment;
  readonly isActive: boolean;
  readonly configurationVersion: number;
  readonly lastConnectionOutcome: CarrierConnectionOutcome | null;
}

export interface CarrierReferenceView {
  readonly kind: number;
  readonly externalId: string;
  readonly title: string;
  readonly isActive: boolean;
}

export interface CarrierBindingView {
  readonly bindingId: string;
  readonly draftId: string;
  readonly carrierAccountId: string;
  readonly providerCode: string;
  readonly providerServiceId: string;
  readonly paymentTypeId: string;
  readonly pickupTypeId: string;
  readonly distributionTypeId: string;
  readonly packingId: string;
  readonly packageContentId: string;
  readonly packTypeId: string;
  readonly originExternalCityCode: string;
  readonly originAddressLine: string | null;
  readonly defaultLengthCentimeters: number;
  readonly defaultWidthCentimeters: number;
  readonly defaultHeightCentimeters: number;
  readonly configurationVersion: number;
}

export type CarrierOutcome<T> =
  | { readonly kind: "ok"; readonly value: T }
  | { readonly kind: "empty" }
  | { readonly kind: "failure"; readonly failure: EditorTransportFailure };

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ENVIRONMENTS: readonly CarrierEnvironment[] = ["Test", "Production"];
const OUTCOMES: readonly CarrierConnectionOutcome[] = [
  "Succeeded",
  "Disabled",
  "SecretMissing",
  "Unauthorized",
  "TemporarilyUnavailable",
  "ProviderRejected",
];

function accountsPath(): string {
  return "/v1/admin/shipping-services/carrier-accounts";
}

function accountPath(accountId: string): string {
  return `${accountsPath()}/${encodeURIComponent(accountId)}`;
}

function bindingPath(draftId: string): string {
  return `/v1/admin/shipping-services/authoring-drafts/${encodeURIComponent(draftId)}/carrier-binding`;
}

function object(node: JsonNode | undefined): ReadonlyMap<string, JsonNode> | null {
  return node && node.kind === "object" ? node.fields : null;
}

function text(node: JsonNode | undefined): string | null {
  return node && node.kind === "string" ? node.value : null;
}

function number(node: JsonNode | undefined): number | null {
  return node && node.kind === "number" ? Number(node.raw) : null;
}

function bool(node: JsonNode | undefined): boolean | null {
  return node && node.kind === "boolean" ? node.value : null;
}

function enumText<T extends string>(node: JsonNode | undefined, allowed: readonly T[]): T | null {
  if (node && node.kind === "number") {
    const index = Number(node.raw);
    return index >= 0 && index < allowed.length ? allowed[index]! : null;
  }
  const value = text(node);
  return value && (allowed as readonly string[]).includes(value) ? (value as T) : null;
}

function decodeAccount(node: JsonNode): CarrierAccountView | null {
  const fields = object(node);
  if (!fields) return null;
  const accountId = text(fields.get("accountId"));
  const providerCode = text(fields.get("providerCode"));
  const displayName = text(fields.get("displayName"));
  const environment = enumText(fields.get("environment"), ENVIRONMENTS);
  const isActive = bool(fields.get("isActive"));
  const configurationVersion = number(fields.get("configurationVersion"));
  if (!accountId || !GUID.test(accountId) || !providerCode || !displayName || !environment || isActive === null || configurationVersion === null) {
    return null;
  }
  const last = fields.get("lastConnectionOutcome");
  const lastConnectionOutcome = last && last.kind !== "null" ? enumText(last, OUTCOMES) : null;
  return { accountId, providerCode, displayName, environment, isActive, configurationVersion, lastConnectionOutcome };
}

function decodeReference(node: JsonNode): CarrierReferenceView | null {
  const fields = object(node);
  if (!fields) return null;
  const kind = number(fields.get("kind"));
  const externalId = text(fields.get("externalId"));
  const title = text(fields.get("title"));
  const isActive = bool(fields.get("isActive"));
  if (kind === null || !externalId || title === null || isActive === null) return null;
  return { kind, externalId, title, isActive };
}

function decodeBinding(node: JsonNode): CarrierBindingView | null {
  const fields = object(node);
  if (!fields) return null;
  const bindingId = text(fields.get("bindingId"));
  const draftId = text(fields.get("draftId"));
  const carrierAccountId = text(fields.get("carrierAccountId"));
  const providerCode = text(fields.get("providerCode"));
  const providerServiceId = text(fields.get("providerServiceId"));
  const paymentTypeId = text(fields.get("paymentTypeId"));
  const pickupTypeId = text(fields.get("pickupTypeId"));
  const distributionTypeId = text(fields.get("distributionTypeId"));
  const packingId = text(fields.get("packingId"));
  const packageContentId = text(fields.get("packageContentId"));
  const packTypeId = text(fields.get("packTypeId"));
  const originExternalCityCode = text(fields.get("originExternalCityCode"));
  const originAddress = fields.get("originAddressLine");
  const defaultLengthCentimeters = number(fields.get("defaultLengthCentimeters"));
  const defaultWidthCentimeters = number(fields.get("defaultWidthCentimeters"));
  const defaultHeightCentimeters = number(fields.get("defaultHeightCentimeters"));
  const configurationVersion = number(fields.get("configurationVersion"));
  if (
    !bindingId ||
    !draftId ||
    !carrierAccountId ||
    !providerCode ||
    !providerServiceId ||
    !paymentTypeId ||
    !pickupTypeId ||
    !distributionTypeId ||
    !packingId ||
    !packageContentId ||
    !packTypeId ||
    !originExternalCityCode ||
    defaultLengthCentimeters === null ||
    defaultWidthCentimeters === null ||
    defaultHeightCentimeters === null ||
    configurationVersion === null
  ) {
    return null;
  }
  return {
    bindingId,
    draftId,
    carrierAccountId,
    providerCode,
    providerServiceId,
    paymentTypeId,
    pickupTypeId,
    distributionTypeId,
    packingId,
    packageContentId,
    packTypeId,
    originExternalCityCode,
    originAddressLine: originAddress && originAddress.kind === "string" ? originAddress.value : null,
    defaultLengthCentimeters,
    defaultWidthCentimeters,
    defaultHeightCentimeters,
    configurationVersion,
  };
}

async function send(path: string, init: RequestInit, signal?: AbortSignal): Promise<{ status: number; root: JsonNode | null }> {
  const response = await fetch(path, { ...init, headers: authoringHeaders(init.headers as Record<string, string> | undefined), signal, cache: "no-store" });
  const textBody = await response.text().then((value) => value, () => null);
  return { status: response.status, root: parseResponseRoot(textBody) };
}

function failure(status: number, root: JsonNode | null): CarrierOutcome<never> {
  return { kind: "failure", failure: editorTransportFailureFor(status, root) };
}

export async function listCarrierAccounts(signal?: AbortSignal): Promise<CarrierOutcome<readonly CarrierAccountView[]>> {
  try {
    const { status, root } = await send(accountsPath(), { method: "GET" }, signal);
    if (status < 200 || status >= 300) return failure(status, root);
    if (!root || root.kind !== "array") return { kind: "failure", failure: { kind: "untrusted", status, reason: "accounts:array" } };
    const rows: CarrierAccountView[] = [];
    for (const item of root.items) {
      const decoded = decodeAccount(item);
      if (!decoded) return { kind: "failure", failure: { kind: "untrusted", status, reason: "account" } };
      rows.push(decoded);
    }
    return { kind: "ok", value: rows };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function createCarrierAccount(
  body: {
    readonly displayName: string;
    readonly username: string;
    readonly password: string;
    readonly environment: CarrierEnvironment;
    readonly isActive: boolean;
  },
  signal?: AbortSignal,
): Promise<CarrierOutcome<CarrierAccountView>> {
  try {
    const { status, root } = await send(
      accountsPath(),
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          providerCode: "tipax",
          displayName: body.displayName,
          environment: body.environment === "Production" ? 1 : 0,
          username: body.username,
          password: body.password,
          isActive: body.isActive,
        }),
      },
      signal,
    );
    if (status < 200 || status >= 300) return failure(status, root);
    const decoded = root ? decodeAccount(root) : null;
    return decoded ? { kind: "ok", value: decoded } : { kind: "failure", failure: { kind: "untrusted", status, reason: "account" } };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function testCarrierConnection(accountId: string, signal?: AbortSignal): Promise<CarrierOutcome<CarrierConnectionOutcome>> {
  try {
    const { status, root } = await send(`${accountPath(accountId)}/test-connection`, { method: "POST" }, signal);
    if (status < 200 || status >= 300) return failure(status, root);
    const outcome = enumText(root ?? undefined, OUTCOMES);
    return outcome ? { kind: "ok", value: outcome } : { kind: "failure", failure: { kind: "untrusted", status, reason: "outcome" } };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function syncCarrierReferences(accountId: string, signal?: AbortSignal): Promise<CarrierOutcome<readonly CarrierReferenceView[]>> {
  try {
    const { status, root } = await send(`${accountPath(accountId)}/sync-references`, { method: "POST" }, signal);
    if (status < 200 || status >= 300) return failure(status, root);
    if (!root || root.kind !== "array") return { kind: "failure", failure: { kind: "untrusted", status, reason: "refs:array" } };
    const rows: CarrierReferenceView[] = [];
    for (const item of root.items) {
      const decoded = decodeReference(item);
      if (!decoded) return { kind: "failure", failure: { kind: "untrusted", status, reason: "ref" } };
      rows.push(decoded);
    }
    return { kind: "ok", value: rows };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export interface CarrierDestinationMappingView {
  readonly mappingId: string;
  readonly geographyCityId: string;
  readonly externalLocationCode: string;
  readonly provenance: string;
}

function decodeMapping(node: JsonNode): CarrierDestinationMappingView | null {
  const fields = object(node);
  if (!fields) return null;
  const mappingId = text(fields.get("mappingId"));
  const geographyCityId = text(fields.get("geographyCityId"));
  const externalLocationCode = text(fields.get("externalLocationCode"));
  const provenance = text(fields.get("provenance"));
  if (!mappingId || !GUID.test(mappingId) || !geographyCityId || !GUID.test(geographyCityId) || !externalLocationCode || provenance === null) {
    return null;
  }
  return { mappingId, geographyCityId, externalLocationCode, provenance };
}

export async function listCarrierDestinationMappings(
  accountId: string,
  signal?: AbortSignal,
): Promise<CarrierOutcome<readonly CarrierDestinationMappingView[]>> {
  try {
    const { status, root } = await send(`${accountPath(accountId)}/destination-mappings`, { method: "GET" }, signal);
    if (status < 200 || status >= 300) return failure(status, root);
    if (!root || root.kind !== "array") return { kind: "failure", failure: { kind: "untrusted", status, reason: "maps:array" } };
    const rows: CarrierDestinationMappingView[] = [];
    for (const item of root.items) {
      const decoded = decodeMapping(item);
      if (!decoded) return { kind: "failure", failure: { kind: "untrusted", status, reason: "map" } };
      rows.push(decoded);
    }
    return { kind: "ok", value: rows };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function upsertCarrierDestinationMapping(
  accountId: string,
  body: { readonly geographyCityId: string; readonly externalLocationCode: string },
  signal?: AbortSignal,
): Promise<CarrierOutcome<CarrierDestinationMappingView>> {
  try {
    const { status, root } = await send(
      `${accountPath(accountId)}/destination-mappings`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          geographyCityId: body.geographyCityId,
          externalLocationCode: body.externalLocationCode,
          provenance: "merchant",
        }),
      },
      signal,
    );
    if (status < 200 || status >= 300) return failure(status, root);
    const decoded = root ? decodeMapping(root) : null;
    return decoded ? { kind: "ok", value: decoded } : { kind: "failure", failure: { kind: "untrusted", status, reason: "map" } };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function listCarrierReferences(accountId: string, signal?: AbortSignal): Promise<CarrierOutcome<readonly CarrierReferenceView[]>> {
  try {
    const { status, root } = await send(`${accountPath(accountId)}/references`, { method: "GET" }, signal);
    if (status < 200 || status >= 300) return failure(status, root);
    if (!root || root.kind !== "array") return { kind: "failure", failure: { kind: "untrusted", status, reason: "refs:array" } };
    const rows: CarrierReferenceView[] = [];
    for (const item of root.items) {
      const decoded = decodeReference(item);
      if (!decoded) return { kind: "failure", failure: { kind: "untrusted", status, reason: "ref" } };
      rows.push(decoded);
    }
    return { kind: "ok", value: rows };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function getCarrierBinding(draftId: string, signal?: AbortSignal): Promise<CarrierOutcome<CarrierBindingView | null>> {
  try {
    const { status, root } = await send(bindingPath(draftId), { method: "GET" }, signal);
    if (status < 200 || status >= 300) return failure(status, root);
    if (!root || (root.kind === "null")) return { kind: "empty" };
    const decoded = decodeBinding(root);
    return decoded ? { kind: "ok", value: decoded } : { kind: "failure", failure: { kind: "untrusted", status, reason: "binding" } };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}

export async function replaceCarrierBinding(
  draftId: string,
  body: {
    readonly expectedRevision: number;
    readonly carrierAccountId: string;
    readonly providerServiceId: string;
    readonly paymentTypeId: string;
    readonly pickupTypeId: string;
    readonly distributionTypeId: string;
    readonly packingId: string;
    readonly packageContentId: string;
    readonly packTypeId: string;
    readonly originExternalCityCode: string;
    readonly originAddressLine: string | null;
    readonly defaultLengthCentimeters: number;
    readonly defaultWidthCentimeters: number;
    readonly defaultHeightCentimeters: number;
  },
  signal?: AbortSignal,
): Promise<CarrierOutcome<CarrierBindingView>> {
  try {
    const { status, root } = await send(
      bindingPath(draftId),
      { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) },
      signal,
    );
    if (status < 200 || status >= 300) return failure(status, root);
    const decoded = root ? decodeBinding(root) : null;
    return decoded ? { kind: "ok", value: decoded } : { kind: "failure", failure: { kind: "untrusted", status, reason: "binding" } };
  } catch {
    return { kind: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
}
