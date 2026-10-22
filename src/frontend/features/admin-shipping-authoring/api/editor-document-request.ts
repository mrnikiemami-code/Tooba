/**
 * Exact request construction for the ONE atomic editor-document PUT.
 *
 * The builder emits the accepted contract shape (`ShippingAuthoringEditorDocumentReplaceRequest`)
 * with the exact decimal/time text preserved: money amounts are emitted as unquoted number tokens,
 * never through `toFixed`/`parseInt`. It refuses to produce a body while any destination selection
 * still lacks its authoritative ancestor chain, so a read-shaped `(kind, id)` can never be echoed
 * into a destructive replace.
 */
import type { AreaAncestryResolver, EditorEdits, RegionGroupEdit } from "../model/editor-edits.ts";
import type { SelectedAreaRead, UnresolvedAreaSelection } from "../model/editor-document.ts";
import { RawNumber, writeJsonText, type JsonWrite } from "./wire-json.ts";

/** Outcome of building the PUT body. */
export type EditorRequestBody =
  | { readonly status: "ready"; readonly text: string }
  | { readonly status: "needsResolution"; readonly pending: readonly UnresolvedAreaSelection[] };

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const DECIMAL = /^(?:0|[1-9][0-9]*)(?:\.[0-9]+)?$/;
const INTEGER = /^(?:0|[1-9][0-9]*)$/;
const TIME = /^(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9](?:\.[0-9]{1,7})?$/;

function requireId(value: string, field: string): string {
  if (!GUID.test(value)) throw new Error(`editor_request_invalid:${field}`);
  return value;
}

function amount(value: string | null, field: string): JsonWrite {
  if (value === null) return null;
  if (!DECIMAL.test(value)) throw new Error(`editor_request_invalid:${field}`);
  return new RawNumber(value);
}

function count(value: number | null, field: string): JsonWrite {
  if (value === null) return null;
  if (!Number.isSafeInteger(value) || value < 0) throw new Error(`editor_request_invalid:${field}`);
  return value;
}

function time(value: string | null, field: string): JsonWrite {
  if (value === null) return null;
  if (!TIME.test(value)) throw new Error(`editor_request_invalid:${field}`);
  return value;
}

function areaWrite(area: SelectedAreaRead, resolver: AreaAncestryResolver): JsonWrite | null {
  const ancestry = resolver.resolve(area);
  if (!ancestry) return null;
  if (ancestry.kind !== area.kind) throw new Error("editor_request_invalid:area-kind");
  return {
    kind: ancestry.kind,
    countryId: requireId(ancestry.countryId, "countryId"),
    administrativeAreaId: ancestry.administrativeAreaId ? requireId(ancestry.administrativeAreaId, "administrativeAreaId") : null,
    cityId: ancestry.cityId ? requireId(ancestry.cityId, "cityId") : null,
    serviceAreaId: ancestry.serviceAreaId ? requireId(ancestry.serviceAreaId, "serviceAreaId") : null,
  };
}

function groupWrite(group: RegionGroupEdit, resolver: AreaAncestryResolver, pending: UnresolvedAreaSelection[]): JsonWrite {
  const areas: JsonWrite[] = [];
  for (const area of group.areas) {
    const write = areaWrite(area, resolver);
    if (write === null) {
      pending.push({ groupId: group.groupId, area });
      continue;
    }
    areas.push(write);
  }
  return {
    groupId: group.groupId === null ? null : requireId(group.groupId, "groupId"),
    areas,
    rows: group.rows.map((row) => ({
      rateRowId: row.rateRowId === null ? null : requireId(row.rateRowId, "rateRowId"),
      minWeightGrams: count(row.minWeightGrams, "minWeightGrams"),
      maxWeightGrams: count(row.maxWeightGrams, "maxWeightGrams"),
      minItemCount: count(row.minItemCount, "minItemCount"),
      maxItemCount: count(row.maxItemCount, "maxItemCount"),
      minMerchandiseAmount: amount(row.minMerchandiseAmount, "minMerchandiseAmount"),
      maxMerchandiseAmount: amount(row.maxMerchandiseAmount, "maxMerchandiseAmount"),
      freightAmount: amount(row.freightAmount, "freightAmount"),
      packagingAmount: amount(row.packagingAmount, "packagingAmount"),
      minDeliveryDays: count(row.minDeliveryDays, "minDeliveryDays"),
      maxDeliveryDays: count(row.maxDeliveryDays, "maxDeliveryDays"),
    })),
    titles: group.titles.map((title) => ({
      languageId: requireId(title.languageId, "languageId"),
      title: title.title,
    })),
  };
}

/**
 * Builds the exact PUT body text for one atomic editor-document replace.
 *
 * @param expectedRevision Root revision the editor actually saw (must be a positive integer).
 * @param edits The intentionally changed parts of the document.
 * @param resolver Authoritative Geography ancestry resolution (never guessed here).
 * @returns Ready body text, or the exact selections that still need resolution.
 */
export function buildEditorDocumentBody(
  expectedRevision: number,
  edits: EditorEdits,
  resolver: AreaAncestryResolver,
): EditorRequestBody {
  if (!Number.isSafeInteger(expectedRevision) || expectedRevision < 1) {
    throw new Error("editor_request_invalid:expectedRevision");
  }

  const pending: UnresolvedAreaSelection[] = [];
  const body: { [key: string]: JsonWrite } = {
    expectedRevision,
    regionGroups: (edits.regionGroups ?? []).map((group) => groupWrite(group, resolver, pending)),
    methodTranslations: (edits.methodTranslations ?? []).map((translation) => ({
      languageId: requireId(translation.languageId, "languageId"),
      name: translation.name,
      description: translation.description,
    })),
  };
  if (pending.length > 0) return { status: "needsResolution", pending };

  if (edits.weeklySchedule) {
    if (!INTEGER.test(String(edits.weeklySchedule.days.length)) || edits.weeklySchedule.days.length !== 7) {
      throw new Error("editor_request_invalid:schedule-days");
    }
    body.weeklySchedule = {
      timeZoneId: edits.weeklySchedule.timeZoneId,
      days: edits.weeklySchedule.days.map((day) => ({
        weekday: day.weekday,
        isWorking: day.isWorking,
        startTime: time(day.startTime, "startTime"),
        endTime: time(day.endTime, "endTime"),
      })),
    };
  }
  if (edits.methodSettings) {
    body.methodSettings = {
      freightPaymentMode: edits.methodSettings.freightPaymentMode,
      calculationMode: edits.methodSettings.calculationMode,
    };
  }
  if (edits.typeAssignment) {
    // `shippingTypeId` is [JsonRequired] on the server: an explicit null MUST stay in the body.
    body.typeAssignment = {
      shippingTypeId:
        edits.typeAssignment.shippingTypeId === null
          ? null
          : requireId(edits.typeAssignment.shippingTypeId, "shippingTypeId"),
    };
  }

  return { status: "ready", text: writeJsonText(body) };
}
