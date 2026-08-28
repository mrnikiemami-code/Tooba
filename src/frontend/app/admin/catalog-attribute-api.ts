/**
 * کلاینت Admin برای schema ویژگی Catalog و محورهای Variant.
 * بدنهٔ POST/PATCH با رکوردهای Host در CatalogAttributeEndpoints هم‌تراز است.
 */

import {
  adminHeaders,
  type AdminResult,
} from "./admin-api.ts";
import {
  ACTOR_STORAGE_KEY,
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "../vendor-panel/seller-api.ts";

export type CatalogAttributeValueKind =
  | "Text"
  | "Number"
  | "Boolean"
  | "Enumeration"
  | "Instant";

const VALUE_KIND_BY_NUMBER: Record<number, CatalogAttributeValueKind> = {
  0: "Text",
  1: "Number",
  2: "Boolean",
  3: "Enumeration",
  4: "Instant",
};

export interface AttributeDefinition {
  definitionId: string;
  code: string;
  valueKind: CatalogAttributeValueKind;
  isVariantAxisAllowed: boolean;
  unit: string | null;
  isRequired: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isMultivalue: boolean;
  displayOrder: number;
  validationMin: number | null;
  validationMax: number | null;
  validationMaxLength: number | null;
  isActive: boolean;
  createdAt: string;
}

export interface EffectiveSchemaEntry {
  definitionId: string;
  code: string;
  valueKind: CatalogAttributeValueKind;
  /** capability: آیا این نوع ویژگی اصلاً می‌تواند محور تنوع باشد */
  isVariantAxisAllowed: boolean;
  /** effective: آیا در این رده به‌عنوان محور تنوع فعال است */
  isVariantAxis: boolean;
  unit: string | null;
  isRequired: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isMultivalue: boolean;
  displayOrder: number;
  inheritedFromCategoryId: string;
  definitionIsActive: boolean;
}

/** فرادادهٔ PATCH مطابق UpdateAttributeDefinitionRequest. */
export interface UpdateAttributeDefinitionInput {
  unit?: string | null;
  isRequired: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isMultivalue: boolean;
  displayOrder: number;
  validationMin?: number | null;
  validationMax?: number | null;
  validationMaxLength?: number | null;
  isActive: boolean;
}

/** بدنهٔ ایجاد مطابق CreateAttributeDefinitionRequest. */
export interface CreateAttributeDefinitionInput {
  code: string;
  valueKind: CatalogAttributeValueKind;
  isVariantAxisAllowed: boolean;
  localizedNames: Record<string, string>;
  metadata?: UpdateAttributeDefinitionInput | null;
}

export interface BindCategoryAttributeInput {
  definitionId: string;
  displayOrder: number;
  isRequired: boolean;
  isFilterable: boolean;
  isVariantAxis: boolean;
  isComparable: boolean;
}

export interface UpdateCategoryAttributeBindingInput {
  isRequired: boolean;
  isFilterable: boolean;
  isVariantAxis: boolean;
  isComparable: boolean;
}

export interface SetProductAttributeInput {
  rawValue: string;
  enumOptionId?: string | null;
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function bool(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function num(value: unknown): number | null {
  if (value == null || value === "") return null;
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : null;
}

function intOr(value: unknown, fallback: number): number {
  const n = num(value);
  return n == null ? fallback : Math.trunc(n);
}

function parseValueKind(raw: unknown): CatalogAttributeValueKind {
  if (typeof raw === "number" && VALUE_KIND_BY_NUMBER[raw]) {
    return VALUE_KIND_BY_NUMBER[raw];
  }
  const s = text(raw);
  if (
    s === "Text" ||
    s === "Number" ||
    s === "Boolean" ||
    s === "Enumeration" ||
    s === "Instant"
  ) {
    return s;
  }
  const asNum = Number(s);
  if (Number.isFinite(asNum) && VALUE_KIND_BY_NUMBER[asNum]) {
    return VALUE_KIND_BY_NUMBER[asNum];
  }
  return "Text";
}

function errorMessage(payload: unknown, status: number): string {
  const item = recordOf(payload);
  if (item) {
    const title = text(prop(item, "title", "Title"));
    const code = text(prop(item, "errorCode", "ErrorCode"));
    if (title) return code ? `${title} (${code})` : title;
    if (code) return code;
  }
  return `admin.http.${status}`;
}

async function adminRead(path: string): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function adminWrite(
  path: string,
  method: string,
  body?: unknown,
): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, {
      method,
      headers: adminHeaders(body === undefined ? undefined : { "Content-Type": "application/json" }),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

function sellerWriteHeaders(sellerPartyId: string): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    "Content-Type": "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
  };
  const actor = readActorUserId() ?? (typeof window !== "undefined" ? window.localStorage.getItem(ACTOR_STORAGE_KEY) : null);
  if (actor) {
    headers[DEV_ACTOR_HEADER] = actor;
  }
  return headers;
}

async function sellerWrite(
  path: string,
  body: unknown,
): Promise<AdminResult<unknown>> {
  const sellerPartyId = readSellerPartyId(typeof window !== "undefined" ? window.location.search : undefined);
  if (!sellerPartyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(path, {
      method: "PUT",
      headers: sellerWriteHeaders(sellerPartyId),
      body: JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "seller.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** نگاشت تعریف ویژگی از Host (camel/Pascal). */
export function mapAttributeDefinition(payload: unknown): AttributeDefinition | null {
  const item = recordOf(payload);
  if (!item) return null;
  const definitionId = text(prop(item, "definitionId", "DefinitionId"));
  const code = text(prop(item, "code", "Code"));
  if (!definitionId || !code) return null;
  return {
    definitionId,
    code,
    valueKind: parseValueKind(prop(item, "valueKind", "ValueKind")),
    isVariantAxisAllowed: bool(prop(item, "isVariantAxisAllowed", "IsVariantAxisAllowed")),
    unit: (() => {
      const u = prop(item, "unit", "Unit");
      return u == null || u === "" ? null : text(u);
    })(),
    isRequired: bool(prop(item, "isRequired", "IsRequired")),
    isFilterable: bool(prop(item, "isFilterable", "IsFilterable")),
    isComparable: bool(prop(item, "isComparable", "IsComparable")),
    isMultivalue: bool(prop(item, "isMultivalue", "IsMultivalue")),
    displayOrder: intOr(prop(item, "displayOrder", "DisplayOrder"), 0),
    validationMin: num(prop(item, "validationMin", "ValidationMin")),
    validationMax: num(prop(item, "validationMax", "ValidationMax")),
    validationMaxLength: num(prop(item, "validationMaxLength", "ValidationMaxLength")),
    isActive: bool(prop(item, "isActive", "IsActive"), true),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
  };
}

/** نگاشت ردیف schema مؤثر رده. */
export function mapEffectiveSchemaEntry(payload: unknown): EffectiveSchemaEntry | null {
  const item = recordOf(payload);
  if (!item) return null;
  const definitionId = text(prop(item, "definitionId", "DefinitionId"));
  const code = text(prop(item, "code", "Code"));
  if (!definitionId || !code) return null;
  return {
    definitionId,
    code,
    valueKind: parseValueKind(prop(item, "valueKind", "ValueKind")),
    isVariantAxisAllowed: bool(prop(item, "isVariantAxisAllowed", "IsVariantAxisAllowed")),
    isVariantAxis: bool(prop(item, "isVariantAxis", "IsVariantAxis")),
    unit: (() => {
      const u = prop(item, "unit", "Unit");
      return u == null || u === "" ? null : text(u);
    })(),
    isRequired: bool(prop(item, "isRequired", "IsRequired")),
    isFilterable: bool(prop(item, "isFilterable", "IsFilterable")),
    isComparable: bool(prop(item, "isComparable", "IsComparable")),
    isMultivalue: bool(prop(item, "isMultivalue", "IsMultivalue")),
    displayOrder: intOr(prop(item, "displayOrder", "DisplayOrder"), 0),
    inheritedFromCategoryId: text(prop(item, "inheritedFromCategoryId", "InheritedFromCategoryId")),
    definitionIsActive: bool(prop(item, "definitionIsActive", "DefinitionIsActive"), true),
  };
}

function mapDefinitionList(payload: unknown): AttributeDefinition[] {
  const rows = Array.isArray(payload) ? payload : [];
  return rows.map(mapAttributeDefinition).filter((row): row is AttributeDefinition => row != null);
}

function mapSchemaList(payload: unknown): EffectiveSchemaEntry[] {
  const rows = Array.isArray(payload) ? payload : [];
  return rows.map(mapEffectiveSchemaEntry).filter((row): row is EffectiveSchemaEntry => row != null);
}

/** فهرست تعاریف ویژگی Admin. */
export async function listAttributeDefinitions(): Promise<AdminResult<AttributeDefinition[]>> {
  const response = await adminRead("/v1/admin/catalog/attribute-definitions");
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: mapDefinitionList(response.data) };
}

/** یک تعریف ویژگی. */
export async function getAttributeDefinition(
  definitionId: string,
): Promise<AdminResult<AttributeDefinition>> {
  const response = await adminRead(`/v1/admin/catalog/attribute-definitions/${definitionId}`);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapAttributeDefinition(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** ایجاد تعریف ویژگی. */
export async function createAttributeDefinition(
  input: CreateAttributeDefinitionInput,
): Promise<AdminResult<{ definitionId: string }>> {
  const body = {
    code: input.code,
    valueKind: input.valueKind,
    isVariantAxisAllowed: input.isVariantAxisAllowed,
    localizedNames: input.localizedNames,
    metadata: input.metadata ?? null,
  };
  const response = await adminWrite("/v1/admin/catalog/attribute-definitions", "POST", body);
  if (response.state !== "ok") return { ...response, data: null };
  const item = recordOf(response.data);
  const definitionId = item ? text(prop(item, "definitionId", "DefinitionId")) : "";
  return definitionId
    ? { ...response, data: { definitionId } }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** به‌روزرسانی فرادادهٔ تعریف. */
export async function updateAttributeDefinition(
  definitionId: string,
  input: UpdateAttributeDefinitionInput,
): Promise<AdminResult<AttributeDefinition>> {
  const response = await adminWrite(
    `/v1/admin/catalog/attribute-definitions/${definitionId}`,
    "PATCH",
    input,
  );
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapAttributeDefinition(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** افزودن گزینهٔ شمارشی. */
export async function addAttributeOption(
  definitionId: string,
  code: string,
  localizedNames: Record<string, string>,
): Promise<AdminResult<{ optionId: string }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/attribute-definitions/${definitionId}/options`,
    "POST",
    { code, localizedNames },
  );
  if (response.state !== "ok") return { ...response, data: null };
  const item = recordOf(response.data);
  const optionId = item ? text(prop(item, "optionId", "OptionId")) : "";
  return optionId
    ? { ...response, data: { optionId } }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** schema مؤثر رده. */
export async function loadEffectiveCategorySchema(
  categoryId: string,
): Promise<AdminResult<EffectiveSchemaEntry[]>> {
  const response = await adminRead(
    `/v1/admin/catalog/categories/${categoryId}/attribute-schema/effective`,
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: mapSchemaList(response.data) };
}

/** پیوند تعریف به رده. */
export async function bindCategoryAttribute(
  categoryId: string,
  input: BindCategoryAttributeInput,
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/attribute-schema/bindings`,
    "POST",
    {
      definitionId: input.definitionId,
      displayOrder: input.displayOrder,
      isRequired: input.isRequired,
      isFilterable: input.isFilterable,
      isVariantAxis: input.isVariantAxis,
      isComparable: input.isComparable,
    },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** به‌روزرسانی assignment محلی رده. */
export async function updateCategoryAttributeBinding(
  categoryId: string,
  definitionId: string,
  input: UpdateCategoryAttributeBindingInput,
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/attribute-schema/bindings/${definitionId}`,
    "PATCH",
    {
      isRequired: input.isRequired,
      isFilterable: input.isFilterable,
      isVariantAxis: input.isVariantAxis,
      isComparable: input.isComparable,
    },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** حذف پیوند تعریف از رده. */
export async function unbindCategoryAttribute(
  categoryId: string,
  definitionId: string,
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/attribute-schema/bindings/${definitionId}`,
    "DELETE",
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** ترتیب پیوندهای رده. */
export async function reorderCategoryBindings(
  categoryId: string,
  orderedDefinitionIds: string[],
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/attribute-schema/bindings/order`,
    "PUT",
    { orderedDefinitionIds },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** تنظیم مقدار ویژگی محصول (Admin). */
export async function setAdminProductAttribute(
  productId: string,
  definitionId: string,
  input: SetProductAttributeInput,
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/products/${productId}/attributes/${definitionId}`,
    "PUT",
    {
      rawValue: input.rawValue,
      enumOptionId: input.enumOptionId || null,
    },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** تنظیم محورهای Variant محصول (Admin). */
export async function setAdminProductVariantAxes(
  productId: string,
  orderedDefinitionIds: string[],
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite(
    `/v1/admin/catalog/products/${productId}/variant-axes`,
    "PUT",
    { orderedDefinitionIds },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** تنظیم مقدار ویژگی محصول (Seller). */
export async function setSellerProductAttribute(
  productId: string,
  definitionId: string,
  input: SetProductAttributeInput,
): Promise<AdminResult<{ ok: true }>> {
  const response = await sellerWrite(
    `/v1/seller/products/${productId}/attributes/${definitionId}`,
    {
      rawValue: input.rawValue,
      enumOptionId: input.enumOptionId || null,
    },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** تنظیم محورهای Variant محصول (Seller). */
export async function setSellerProductVariantAxes(
  productId: string,
  orderedDefinitionIds: string[],
): Promise<AdminResult<{ ok: true }>> {
  const response = await sellerWrite(
    `/v1/seller/products/${productId}/variant-axes`,
    { orderedDefinitionIds },
  );
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** برچسب فارسی گونهٔ مقدار. */
export function valueKindLabel(kind: CatalogAttributeValueKind): string {
  switch (kind) {
    case "Text":
      return "متن";
    case "Number":
      return "عدد";
    case "Boolean":
      return "بولین";
    case "Enumeration":
      return "شمارشی";
    case "Instant":
      return "زمان";
    default:
      return kind;
  }
}
