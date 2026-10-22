/**
 * Strict decoder for the shipping authoring editor document response.
 *
 * Every required scalar/object/array is validated before the document is exposed. Nothing is
 * defaulted, coerced or filtered: a malformed collection, unknown enum variant, non-GUID id,
 * malformed decimal/time or missing default-language metadata fails the whole decode as untrusted
 * instead of yielding a partial document. Stored raw text (including inactive-language ids) is
 * retained exactly as received.
 */
import type { JsonNode } from "./wire-json.ts";
import { isDecimalText, isIntegerText } from "./wire-json.ts";
import type {
  AuthoringAreaKind,
  AuthoringCalculationMode,
  AuthoringOwnerKind,
  AuthoringTypeResolution,
  EditorContentLanguage,
  EditorDocument,
  EditorGroupTitle,
  EditorMethodSettings,
  EditorMethodTranslation,
  EditorOwnerView,
  EditorPresentation,
  EditorRateRow,
  EditorRegionGroup,
  EditorScheduleDay,
  EditorShippingType,
  EditorTypeAssignment,
  EditorTypeTranslation,
  EditorWeeklySchedule,
  FreightPaymentMode,
} from "../model/editor-document.ts";

/** Result of a strict decode: either the typed value or an untrusted-payload reason. */
export type Decoded<T> = { readonly ok: true; readonly value: T } | { readonly ok: false; readonly reason: string };

const AREA_KINDS: readonly AuthoringAreaKind[] = ["Country", "AdministrativeArea", "City", "ServiceArea"];
const OWNER_KINDS: readonly AuthoringOwnerKind[] = ["Store", "Marketplace", "Seller"];
const RESOLUTIONS: readonly AuthoringTypeResolution[] = ["Unconfigured", "Active", "Inactive", "Missing"];
const PAYER_MODES: readonly FreightPaymentMode[] = ["Prepaid", "CollectOnDelivery"];
const CALCULATION_MODES: readonly AuthoringCalculationMode[] = ["Manual", "CarrierApi", "Free"];
const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO_GUID = "00000000-0000-0000-0000-000000000000";
const TIMESTAMP = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/;
const TIME = /^(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9](?:\.[0-9]{1,7})?$/;

class DecodeFailure extends Error {}

function fail(reason: string): never {
  throw new DecodeFailure(reason);
}

function object(node: JsonNode | undefined, path: string): ReadonlyMap<string, JsonNode> {
  if (!node || node.kind !== "object") fail(`${path}:object`);
  return node.fields;
}

function array(node: JsonNode | undefined, path: string): readonly JsonNode[] {
  if (!node || node.kind !== "array") fail(`${path}:array`);
  return node.items;
}

function text(node: JsonNode | undefined, path: string): string {
  if (!node || node.kind !== "string") fail(`${path}:string`);
  return node.value;
}

function optionalText(node: JsonNode | undefined, path: string): string | null {
  if (node === undefined || node.kind === "null") return null;
  return text(node, path);
}

function flag(node: JsonNode | undefined, path: string): boolean {
  if (!node || node.kind !== "boolean") fail(`${path}:boolean`);
  return node.value;
}

function integer(node: JsonNode | undefined, path: string): number {
  if (!node || node.kind !== "number" || !isIntegerText(node.raw)) fail(`${path}:integer`);
  const value = Number(node.raw);
  if (!Number.isSafeInteger(value)) fail(`${path}:integer-range`);
  return value;
}

function optionalInteger(node: JsonNode | undefined, path: string): number | null {
  if (node === undefined || node.kind === "null") return null;
  return integer(node, path);
}

function decimal(node: JsonNode | undefined, path: string): string {
  if (!node || node.kind !== "number" || !isDecimalText(node.raw)) fail(`${path}:decimal`);
  return node.raw;
}

function optionalDecimal(node: JsonNode | undefined, path: string): string | null {
  if (node === undefined || node.kind === "null") return null;
  return decimal(node, path);
}

function guid(node: JsonNode | undefined, path: string): string {
  const value = text(node, path);
  if (!GUID.test(value) || value.toLowerCase() === ZERO_GUID) fail(`${path}:guid`);
  return value;
}

function optionalGuid(node: JsonNode | undefined, path: string): string | null {
  if (node === undefined || node.kind === "null") return null;
  return guid(node, path);
}

function timestamp(node: JsonNode | undefined, path: string): string {
  const value = text(node, path);
  if (!TIMESTAMP.test(value)) fail(`${path}:timestamp`);
  return value;
}

function optionalTime(node: JsonNode | undefined, path: string): string | null {
  if (node === undefined || node.kind === "null") return null;
  const value = text(node, path);
  if (!TIME.test(value)) fail(`${path}:time`);
  return value;
}

function variant<T extends string>(node: JsonNode | undefined, path: string, allowed: readonly T[]): T {
  const value = text(node, path);
  const match = allowed.find((candidate) => candidate === value);
  if (match === undefined) fail(`${path}:variant`);
  return match;
}

function required(node: ReadonlyMap<string, JsonNode>, key: string): JsonNode {
  const value = node.get(key);
  if (value === undefined) fail(`${key}:missing`);
  return value;
}

/**
 * Decodes the composite editor-document payload.
 *
 * @param node Parsed response root.
 * @param expectedDraftId Draft id requested by the caller; the payload must describe the same draft.
 * @returns Typed document or an untrusted reason.
 */
export function decodeEditorDocument(node: JsonNode, expectedDraftId: string): Decoded<EditorDocument> {
  try {
    const root = object(node, "root");
    const draftId = guid(required(root, "draftId"), "draftId");
    if (draftId.toLowerCase() !== expectedDraftId.toLowerCase()) return { ok: false, reason: "draftId:mismatch" };
    const ownerNode = object(required(root, "owner"), "owner");
    const owner: EditorOwnerView = {
      kind: variant(ownerNode.get("kind"), "owner.kind", OWNER_KINDS),
      sellerPartyId: optionalGuid(ownerNode.get("sellerPartyId"), "owner.sellerPartyId"),
    };
    if (owner.kind === "Seller" && owner.sellerPartyId === null) fail("owner.sellerPartyId:required");
    if (owner.kind !== "Seller" && owner.sellerPartyId !== null) fail("owner.sellerPartyId:unexpected");

    const revision = integer(required(root, "revision"), "revision");
    if (revision < 1) fail("revision:range");

    const document: EditorDocument = {
      draftId,
      owner,
      serviceCode: text(required(root, "serviceCode"), "serviceCode"),
      optionCode: optionalText(root.get("optionCode"), "optionCode"),
      revision,
      createdAt: timestamp(required(root, "createdAt"), "createdAt"),
      updatedAt: timestamp(required(root, "updatedAt"), "updatedAt"),
      type: decodeType(object(required(root, "type"), "type"), draftId, revision),
      settings: decodeSettings(root.get("settings")),
      presentation: decodePresentation(object(required(root, "presentation"), "presentation")),
      weeklySchedule: decodeSchedule(root.get("weeklySchedule")),
      regionGroups: array(required(root, "regionGroups"), "regionGroups").map((group, index) =>
        decodeGroup(group, `regionGroups[${index}]`),
      ),
    };
    return { ok: true, value: document };
  } catch (error) {
    if (error instanceof DecodeFailure) return { ok: false, reason: error.message };
    throw error;
  }
}

function decodeType(
  node: ReadonlyMap<string, JsonNode>,
  rootDraftId: string,
  revision: number,
): EditorTypeAssignment {
  const draftId = guid(required(node, "draftId"), "type.draftId");
  if (!sameId(draftId, rootDraftId)) fail("type.draftId:mismatch");
  const typeRevision = integer(required(node, "revision"), "type.revision");
  if (typeRevision !== revision) fail("type.revision:mismatch");
  const resolution = variant(node.get("resolution"), "type.resolution", RESOLUTIONS);
  const shippingTypeId = optionalGuid(node.get("shippingTypeId"), "type.shippingTypeId");
  const typeNode = node.get("shippingType");
  const shippingType = typeNode === undefined || typeNode.kind === "null" ? null : decodeShippingType(typeNode);
  if (resolution === "Unconfigured" && (shippingTypeId !== null || shippingType !== null)) {
    fail("type:unconfigured-mismatch");
  }
  if (resolution !== "Unconfigured" && shippingTypeId === null) fail("type.shippingTypeId:required");
  if (resolution === "Missing" && shippingType !== null) fail("type.shippingType:unexpected");
  if (resolution !== "Missing" && resolution !== "Unconfigured") {
    if (!shippingType) fail("type.shippingType:required");
    if (shippingType.shippingTypeId.toLowerCase() !== shippingTypeId?.toLowerCase()) fail("type.shippingType:mismatch");
  }
  return { draftId, revision: typeRevision, shippingTypeId, resolution, shippingType };
}

function decodeShippingType(node: JsonNode): EditorShippingType {
  const fields = object(node, "type.shippingType");
  return {
    shippingTypeId: guid(required(fields, "shippingTypeId"), "type.shippingType.shippingTypeId"),
    code: text(required(fields, "code"), "type.shippingType.code"),
    sortOrder: integer(required(fields, "sortOrder"), "type.shippingType.sortOrder"),
    revision: integer(required(fields, "revision"), "type.shippingType.revision"),
    translations: array(required(fields, "translations"), "type.shippingType.translations").map(
      (translation, index): EditorTypeTranslation => {
        const item = object(translation, `type.shippingType.translations[${index}]`);
        return {
          languageId: guid(required(item, "languageId"), "type.shippingType.translations.languageId"),
          name: text(required(item, "name"), "type.shippingType.translations.name"),
          description: optionalText(item.get("description"), "type.shippingType.translations.description"),
        };
      },
    ),
  };
}

function decodeSettings(node: JsonNode | undefined): EditorMethodSettings | null {
  if (node === undefined || node.kind === "null") return null;
  const fields = object(node, "settings");
  return {
    freightPaymentMode: variant(fields.get("freightPaymentMode"), "settings.freightPaymentMode", PAYER_MODES),
    calculationMode: variant(fields.get("calculationMode"), "settings.calculationMode", CALCULATION_MODES),
  };
}

function decodeSchedule(node: JsonNode | undefined): EditorWeeklySchedule | null {
  if (node === undefined || node.kind === "null") return null;
  const fields = object(node, "weeklySchedule");
  const days = array(required(fields, "days"), "weeklySchedule.days").map((day, index): EditorScheduleDay => {
    const item = object(day, `weeklySchedule.days[${index}]`);
    const weekday = integer(required(item, "weekday"), "weeklySchedule.days.weekday");
    if (weekday < 1 || weekday > 7) fail("weeklySchedule.days.weekday:range");
    return {
      weekday,
      isWorking: flag(required(item, "isWorking"), "weeklySchedule.days.isWorking"),
      startTime: optionalTime(item.get("startTime"), "weeklySchedule.days.startTime"),
      endTime: optionalTime(item.get("endTime"), "weeklySchedule.days.endTime"),
    };
  });
  const weekdays = new Set(days.map((day) => day.weekday));
  if (weekdays.size !== days.length) fail("weeklySchedule.days:duplicate");
  if (days.length !== 7) fail("weeklySchedule.days:count");
  return { timeZoneId: text(required(fields, "timeZoneId"), "weeklySchedule.timeZoneId"), days };
}

function decodePresentation(node: ReadonlyMap<string, JsonNode>): EditorPresentation {
  const contentLanguages = array(required(node, "contentLanguages"), "presentation.contentLanguages").map(
    (language, index): EditorContentLanguage => {
      const path = `presentation.contentLanguages[${index}]`;
      const item = object(language, path);
      return {
        languageId: guid(required(item, "languageId"), `${path}.languageId`),
        code: text(required(item, "code"), `${path}.code`),
        culture: text(required(item, "culture"), `${path}.culture`),
        urlPrefix: text(required(item, "urlPrefix"), `${path}.urlPrefix`),
        direction: variant(item.get("direction"), `${path}.direction`, ["rtl", "ltr"] as const),
        displayName: text(required(item, "displayName"), `${path}.displayName`),
        nativeName: text(required(item, "nativeName"), `${path}.nativeName`),
        isDefault: flag(required(item, "isDefault"), `${path}.isDefault`),
        sortOrder: integer(required(item, "sortOrder"), `${path}.sortOrder`),
      };
    },
  );
  const defaultContentLanguageId = guid(
    required(node, "defaultContentLanguageId"),
    "presentation.defaultContentLanguageId",
  );
  const languageIds = new Set<string>();
  for (const language of contentLanguages) {
    const key = language.languageId.toLowerCase();
    if (languageIds.has(key)) fail("presentation.contentLanguages:duplicate");
    languageIds.add(key);
  }
  const declaredDefaults = contentLanguages.filter((language) => language.isDefault);
  if (declaredDefaults.length !== 1) fail("presentation.contentLanguages:default-count");
  if (!sameId(declaredDefaults[0]?.languageId ?? null, defaultContentLanguageId)) {
    fail("presentation.defaultContentLanguageId:unresolved");
  }
  return {
    methodTranslations: array(required(node, "methodTranslations"), "presentation.methodTranslations").map(
      (translation, index): EditorMethodTranslation => {
        const path = `presentation.methodTranslations[${index}]`;
        const item = object(translation, path);
        return {
          languageId: guid(required(item, "languageId"), `${path}.languageId`),
          name: text(required(item, "name"), `${path}.name`),
          description: optionalText(item.get("description"), `${path}.description`),
        };
      },
    ),
    groupTitles: array(required(node, "groupTitles"), "presentation.groupTitles").map(
      (title, index): EditorGroupTitle => {
        const path = `presentation.groupTitles[${index}]`;
        const item = object(title, path);
        return {
          languageId: guid(required(item, "languageId"), `${path}.languageId`),
          title: text(required(item, "title"), `${path}.title`),
        };
      },
    ),
    contentLanguages,
    defaultContentLanguageId,
    hasDefaultLanguageName: flag(required(node, "hasDefaultLanguageName"), "presentation.hasDefaultLanguageName"),
  };
}

function decodeGroup(node: JsonNode, path: string): EditorRegionGroup {
  const fields = object(node, path);
  const order = integer(required(fields, "order"), `${path}.order`);
  if (order < 0) fail(`${path}.order:range`);
  return {
    groupId: guid(required(fields, "groupId"), `${path}.groupId`),
    order,
    currencyCode: optionalText(fields.get("currencyCode"), `${path}.currencyCode`),
    areas: array(required(fields, "areas"), `${path}.areas`).map((area, index) => {
      const item = object(area, `${path}.areas[${index}]`);
      return {
        kind: variant(item.get("kind"), `${path}.areas[${index}].kind`, AREA_KINDS),
        id: guid(required(item, "id"), `${path}.areas[${index}].id`),
      };
    }),
    rows: array(required(fields, "rows"), `${path}.rows`).map((row, index) => decodeRow(row, `${path}.rows[${index}]`)),
    titles: array(required(fields, "titles"), `${path}.titles`).map((title, index) => {
      const item = object(title, `${path}.titles[${index}]`);
      return {
        languageId: guid(required(item, "languageId"), `${path}.titles[${index}].languageId`),
        title: text(required(item, "title"), `${path}.titles[${index}].title`),
      };
    }),
  };
}

function decodeRow(node: JsonNode, path: string): EditorRateRow {
  const fields = object(node, path);
  return {
    rateRowId: guid(required(fields, "rateRowId"), `${path}.rateRowId`),
    minWeightGrams: optionalInteger(fields.get("minWeightGrams"), `${path}.minWeightGrams`),
    maxWeightGrams: optionalInteger(fields.get("maxWeightGrams"), `${path}.maxWeightGrams`),
    minItemCount: optionalInteger(fields.get("minItemCount"), `${path}.minItemCount`),
    maxItemCount: optionalInteger(fields.get("maxItemCount"), `${path}.maxItemCount`),
    minMerchandiseAmount: optionalDecimal(fields.get("minMerchandiseAmount"), `${path}.minMerchandiseAmount`),
    maxMerchandiseAmount: optionalDecimal(fields.get("maxMerchandiseAmount"), `${path}.maxMerchandiseAmount`),
    freightAmount: decimal(required(fields, "freightAmount"), `${path}.freightAmount`),
    packagingAmount: decimal(required(fields, "packagingAmount"), `${path}.packagingAmount`),
    minDeliveryDays: integer(required(fields, "minDeliveryDays"), `${path}.minDeliveryDays`),
    maxDeliveryDays: integer(required(fields, "maxDeliveryDays"), `${path}.maxDeliveryDays`),
  };
}

/** Case-insensitive identity comparison for server GUIDs. */
export function sameId(left: string | null, right: string | null): boolean {
  if (left === null || right === null) return left === right;
  return left.toLowerCase() === right.toLowerCase();
}
