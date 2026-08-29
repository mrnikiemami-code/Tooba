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

export interface ProductAttributeEditorOption {
  optionId: string;
  localizedLabel: string;
  isActive: boolean;
}

export interface ProductAttributeReadiness {
  isComplete: boolean;
  missingRequiredCodes: string[];
  invalidValues: string[];
}

export interface ProductAttributeEditorField {
  definitionId: string;
  code: string;
  localizedName: string;
  valueKind: CatalogAttributeValueKind;
  unit: string | null;
  isRequired: boolean;
  isVariantAxis: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isMultivalue: boolean;
  displayOrder: number;
  options: ProductAttributeEditorOption[];
  currentCanonicalValue: string | null;
  currentEnumOptionId: string | null;
  displayValue: string | null;
  isMissingRequired: boolean;
}

export interface ProductAttributeEditorState {
  productId: string;
  categoryId: string | null;
  categoryPath: string | null;
  fields: ProductAttributeEditorField[];
  readiness: ProductAttributeReadiness;
}

export interface ProductAttributeValueInput {
  definitionId: string;
  rawValue?: string | null;
  enumOptionId?: string | null;
  clear?: boolean;
}

export interface CategoryChangeOrphanSummary {
  definitionId: string;
  localizedName: string;
  displayValue: string;
}

export interface CategoryChangeImpactReport {
  productId: string;
  newCategoryId: string;
  compatiblePreservedCount: number;
  orphanCount: number;
  newlyRequiredMissingCount: number;
  orphanSummaries: CategoryChangeOrphanSummary[];
  newlyRequiredLabels: string[];
  invalidVariantAxisDefinitionIds: string[];
  messageFa: string;
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

function parseEditorOption(raw: unknown): ProductAttributeEditorOption | null {
  const item = recordOf(raw);
  if (!item) return null;
  const optionId = text(prop(item, "optionId", "OptionId"));
  if (!optionId) return null;
  return {
    optionId,
    localizedLabel: text(prop(item, "localizedLabel", "LocalizedLabel"), optionId),
    isActive: bool(prop(item, "isActive", "IsActive"), true),
  };
}

function parseEditorField(raw: unknown): ProductAttributeEditorField | null {
  const item = recordOf(raw);
  if (!item) return null;
  const definitionId = text(prop(item, "definitionId", "DefinitionId"));
  if (!definitionId) return null;
  const optionsRaw = prop(item, "options", "Options");
  const options = Array.isArray(optionsRaw)
    ? optionsRaw.map(parseEditorOption).filter((x): x is ProductAttributeEditorOption => x != null)
    : [];
  const enumRaw = prop(item, "currentEnumOptionId", "CurrentEnumOptionId");
  return {
    definitionId,
    code: text(prop(item, "code", "Code")),
    localizedName: text(prop(item, "localizedName", "LocalizedName")),
    valueKind: parseValueKind(prop(item, "valueKind", "ValueKind")),
    unit: text(prop(item, "unit", "Unit")) || null,
    isRequired: bool(prop(item, "isRequired", "IsRequired")),
    isVariantAxis: bool(prop(item, "isVariantAxis", "IsVariantAxis")),
    isFilterable: bool(prop(item, "isFilterable", "IsFilterable")),
    isComparable: bool(prop(item, "isComparable", "IsComparable")),
    isMultivalue: bool(prop(item, "isMultivalue", "IsMultivalue")),
    displayOrder: intOr(prop(item, "displayOrder", "DisplayOrder"), 0),
    options,
    currentCanonicalValue: text(prop(item, "currentCanonicalValue", "CurrentCanonicalValue")) || null,
    currentEnumOptionId: enumRaw == null || enumRaw === "" ? null : text(enumRaw),
    displayValue: text(prop(item, "displayValue", "DisplayValue")) || null,
    isMissingRequired: bool(prop(item, "isMissingRequired", "IsMissingRequired")),
  };
}

function parseReadiness(raw: unknown): ProductAttributeReadiness {
  const item = recordOf(raw);
  if (!item) {
    return { isComplete: true, missingRequiredCodes: [], invalidValues: [] };
  }
  const missing = prop(item, "missingRequiredCodes", "MissingRequiredCodes");
  const invalid = prop(item, "invalidValues", "InvalidValues");
  return {
    isComplete: bool(prop(item, "isComplete", "IsComplete"), true),
    missingRequiredCodes: Array.isArray(missing) ? missing.map((x) => text(x)).filter(Boolean) : [],
    invalidValues: Array.isArray(invalid) ? invalid.map((x) => text(x)).filter(Boolean) : [],
  };
}

function parseEditorState(raw: unknown): ProductAttributeEditorState | null {
  const item = recordOf(raw);
  if (!item) return null;
  const productId = text(prop(item, "productId", "ProductId"));
  if (!productId) return null;
  const fieldsRaw = prop(item, "fields", "Fields");
  const fields = Array.isArray(fieldsRaw)
    ? fieldsRaw.map(parseEditorField).filter((x): x is ProductAttributeEditorField => x != null)
    : [];
  const categoryRaw = prop(item, "categoryId", "CategoryId");
  return {
    productId,
    categoryId: categoryRaw == null || categoryRaw === "" ? null : text(categoryRaw),
    categoryPath: text(prop(item, "categoryPath", "CategoryPath")) || null,
    fields,
    readiness: parseReadiness(prop(item, "readiness", "Readiness")),
  };
}

/** بارگذاری حالت ویرایشگر ویژگی محصول از schema مؤثر. */
export async function getProductAttributeEditorState(
  productId: string,
  locale = "fa-IR",
): Promise<AdminResult<ProductAttributeEditorState>> {
  const q = new URLSearchParams({ locale });
  const response = await adminRead(`/v1/admin/catalog/products/${productId}/attributes?${q}`);
  if (response.state !== "ok") return { ...response, data: null };
  const state = parseEditorState(response.data);
  if (!state) {
    return { state: "error", data: null, status: response.status, message: "catalog.attribute.editor.parse" };
  }
  return { ...response, data: state };
}

/** ذخیرهٔ دسته‌ای مقادیر ویژگی محصول. */
export async function setProductAttributes(
  productId: string,
  values: ProductAttributeValueInput[],
  locale = "fa-IR",
): Promise<AdminResult<ProductAttributeEditorState>> {
  const response = await adminWrite(`/v1/admin/catalog/products/${productId}/attributes`, "PUT", {
    locale,
    values: values.map((v) => ({
      definitionId: v.definitionId,
      rawValue: v.rawValue ?? null,
      enumOptionId: v.enumOptionId ?? null,
      clear: Boolean(v.clear),
    })),
  });
  if (response.state !== "ok") return { ...response, data: null };
  const state = parseEditorState(response.data);
  if (!state) {
    return { state: "error", data: null, status: response.status, message: "catalog.attribute.editor.parse" };
  }
  return { ...response, data: state };
}

/** آمادگی ویژگی‌های الزامی محصول. */
export async function getProductAttributeReadiness(
  productId: string,
): Promise<AdminResult<ProductAttributeReadiness>> {
  const response = await adminRead(`/v1/admin/catalog/products/${productId}/attributes/readiness`);
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: parseReadiness(response.data) };
}

/** پیش‌نمایش تأثیر تغییر رده با خلاصهٔ فارسی. */
export async function previewProductCategoryChange(
  productId: string,
  newCategoryId: string,
  locale = "fa-IR",
): Promise<AdminResult<CategoryChangeImpactReport>> {
  const response = await adminWrite(
    `/v1/admin/catalog/products/${productId}/category-change-preview`,
    "POST",
    { newCategoryId, locale },
  );
  if (response.state !== "ok") return { ...response, data: null };
  const item = recordOf(response.data);
  if (!item) {
    return { state: "error", data: null, status: response.status, message: "catalog.category_change.parse" };
  }
  const orphansRaw = prop(item, "orphanSummaries", "OrphanSummaries");
  const orphanSummaries = Array.isArray(orphansRaw)
    ? orphansRaw
        .map((row) => {
          const r = recordOf(row);
          if (!r) return null;
          return {
            definitionId: text(prop(r, "definitionId", "DefinitionId")),
            localizedName: text(prop(r, "localizedName", "LocalizedName")),
            displayValue: text(prop(r, "displayValue", "DisplayValue")),
          } satisfies CategoryChangeOrphanSummary;
        })
        .filter((x): x is CategoryChangeOrphanSummary => x != null && Boolean(x.definitionId))
    : [];
  const labelsRaw = prop(item, "newlyRequiredLabels", "NewlyRequiredLabels");
  const axesRaw = prop(item, "invalidVariantAxisDefinitionIds", "InvalidVariantAxisDefinitionIds");
  return {
    ...response,
    data: {
      productId: text(prop(item, "productId", "ProductId")),
      newCategoryId: text(prop(item, "newCategoryId", "NewCategoryId")),
      compatiblePreservedCount: intOr(prop(item, "compatiblePreservedCount", "CompatiblePreservedCount"), 0),
      orphanCount: intOr(prop(item, "orphanCount", "OrphanCount"), 0),
      newlyRequiredMissingCount: intOr(prop(item, "newlyRequiredMissingCount", "NewlyRequiredMissingCount"), 0),
      orphanSummaries,
      newlyRequiredLabels: Array.isArray(labelsRaw) ? labelsRaw.map((x) => text(x)).filter(Boolean) : [],
      invalidVariantAxisDefinitionIds: Array.isArray(axesRaw)
        ? axesRaw.map((x) => text(x)).filter(Boolean)
        : [],
      messageFa: text(prop(item, "messageFa", "MessageFa")),
    },
  };
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
