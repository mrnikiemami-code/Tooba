/**
 * Exact request construction for the ONE atomic editor-document PUT.
 *
 * The body is always built from the CONFIRMED server baseline plus the intentional edits, never from
 * the edit intent alone: an untouched region collection is serialized as the complete retained
 * baseline tree (ids, order, areas, rows, exact decimal text) so a method/settings/type-only edit can
 * never delete persisted groups. Only an explicit `regionGroups: []` means whole-collection removal.
 *
 * Retained selections still need their authoritative ancestor chain, only ACTIVE registry languages
 * may be written, and a resolved ancestry must describe the SAME terminal destination. Anything else
 * returns an explicit typed failure instead of a body.
 */
import type { AreaAncestryResolver, EditorEdits, RegionGroupEdit, ResolvedAreaAncestry } from "../model/editor-edits.ts";
import type { EditorDocument, EditorRateRow, EditorRegionGroup, SelectedAreaRead, UnresolvedAreaSelection } from "../model/editor-document.ts";
import { RawNumber, writeJsonText, type JsonWrite } from "./wire-json.ts";

/** Why a typed intent cannot be serialized safely. Codes only; no user-facing prose here. */
export type EditorIntentIssue =
  | { readonly code: "languageUnavailable"; readonly languageId: string }
  | { readonly code: "ancestryMismatch"; readonly groupId: string | null; readonly area: SelectedAreaRead };

/** Outcome of building the PUT body. */
export type EditorRequestBody =
  | { readonly status: "ready"; readonly text: string }
  | { readonly status: "needsResolution"; readonly pending: readonly UnresolvedAreaSelection[] }
  | { readonly status: "invalidIntent"; readonly issues: readonly EditorIntentIssue[] };

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO_GUID = "00000000-0000-0000-0000-000000000000";
const DECIMAL = /^(?:0|[1-9][0-9]*)(?:\.[0-9]+)?$/;
const TIME = /^(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9](?:\.[0-9]{1,7})?$/;

function requireId(value: string, field: string): string {
  if (!GUID.test(value) || value.toLowerCase() === ZERO_GUID) throw new Error(`editor_request_invalid:${field}`);
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

function presentId(value: string | null | undefined, field: string): string | null {
  return value === null || value === undefined ? null : requireId(value, field);
}

/** Canonical ancestor shape per destination kind and the terminal id it must resolve to. */
function writesArea(area: SelectedAreaRead, ancestry: ResolvedAreaAncestry): JsonWrite | null {
  if (ancestry.kind !== area.kind) return null;
  const countryId = presentId(ancestry.countryId, "countryId");
  const administrativeAreaId = presentId(ancestry.administrativeAreaId, "administrativeAreaId");
  const cityId = presentId(ancestry.cityId, "cityId");
  const serviceAreaId = presentId(ancestry.serviceAreaId, "serviceAreaId");
  if (countryId === null) return null;
  // Deeper ancestors that this destination kind must not carry are rejected, never dropped.
  if (area.kind === "Country" && (administrativeAreaId !== null || cityId !== null || serviceAreaId !== null)) {
    return null;
  }
  if (area.kind === "AdministrativeArea" && (cityId !== null || serviceAreaId !== null)) return null;
  if (area.kind === "City" && serviceAreaId !== null) return null;

  const shape =
    area.kind === "Country"
      ? { administrativeAreaId: null, cityId: null, serviceAreaId: null, terminal: countryId }
      : area.kind === "AdministrativeArea"
        ? { administrativeAreaId, cityId: null, serviceAreaId: null, terminal: administrativeAreaId }
        : area.kind === "City"
          ? { administrativeAreaId, cityId, serviceAreaId: null, terminal: cityId }
          : { administrativeAreaId, cityId, serviceAreaId, terminal: serviceAreaId };

  // Canonical backend rule: only an AdministrativeArea selection itself must carry an admin area id.
  // A City (and a ServiceArea under it) may legitimately have NO administrative parent at all, so a
  // null admin is lawful for those kinds and must not be rejected here.
  if (area.kind === "AdministrativeArea" && shape.administrativeAreaId === null) return null;
  if (area.kind === "City" && shape.cityId === null) return null;
  if (area.kind === "ServiceArea" && (shape.cityId === null || shape.serviceAreaId === null)) return null;
  if (shape.terminal === null || shape.terminal.toLowerCase() !== area.id.toLowerCase()) return null;

  return {
    kind: area.kind,
    countryId,
    administrativeAreaId: shape.administrativeAreaId,
    cityId: shape.cityId,
    serviceAreaId: shape.serviceAreaId,
  };
}

function areaWrites(
  groupId: string | null,
  areas: readonly SelectedAreaRead[],
  resolver: AreaAncestryResolver,
  pending: UnresolvedAreaSelection[],
  issues: EditorIntentIssue[],
): JsonWrite[] {
  const writes: JsonWrite[] = [];
  for (const area of areas) {
    const ancestry = resolver.resolve(area);
    if (ancestry === null) {
      pending.push({ groupId, area });
      continue;
    }
    const write = writesArea(area, ancestry);
    if (write === null) {
      issues.push({ code: "ancestryMismatch", groupId, area });
      continue;
    }
    writes.push(write);
  }
  return writes;
}

function rowWrites(rows: readonly (EditorRateRow | RegionGroupEdit["rows"][number])[]): JsonWrite[] {
  return rows.map((row) => ({
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
  }));
}

/** Retained baseline group: the whole saved tree is re-sent, titles stay PATCH-FREE. */
function retainedGroupWrite(
  group: EditorRegionGroup,
  resolver: AreaAncestryResolver,
  pending: UnresolvedAreaSelection[],
  issues: EditorIntentIssue[],
): JsonWrite {
  return {
    groupId: requireId(group.groupId, "groupId"),
    areas: areaWrites(group.groupId, group.areas, resolver, pending, issues),
    rows: rowWrites(group.rows),
    titles: [],
  };
}

/** Explicitly edited group: the operator's ordered replacement, with active-language title patches. */
function editedGroupWrite(
  group: RegionGroupEdit,
  resolver: AreaAncestryResolver,
  pending: UnresolvedAreaSelection[],
  issues: EditorIntentIssue[],
  activeLanguages: ReadonlySet<string>,
): JsonWrite {
  for (const title of group.titles) {
    if (!activeLanguages.has(title.languageId.toLowerCase())) {
      issues.push({ code: "languageUnavailable", languageId: title.languageId });
    }
  }
  return {
    groupId: group.groupId === null ? null : requireId(group.groupId, "groupId"),
    areas: areaWrites(group.groupId, group.areas, resolver, pending, issues),
    rows: rowWrites(group.rows),
    titles: group.titles.map((title) => ({
      languageId: requireId(title.languageId, "languageId"),
      title: title.title,
    })),
  };
}

/**
 * Builds the exact PUT body text for one atomic editor-document replace.
 *
 * @param baseline The confirmed server document this session is editing (also the revision source).
 * @param edits The intentionally changed parts of the document.
 * @param resolver Authoritative Geography ancestry resolution (never guessed here).
 * @returns Ready body text, the selections still needing resolution, or the rejected intents.
 */
export function buildEditorDocumentBody(
  baseline: EditorDocument,
  edits: EditorEdits,
  resolver: AreaAncestryResolver,
): EditorRequestBody {
  const expectedRevision = baseline.revision;
  if (!Number.isSafeInteger(expectedRevision) || expectedRevision < 1) {
    throw new Error("editor_request_invalid:expectedRevision");
  }

  const activeLanguages = new Set(
    baseline.presentation.contentLanguages.map((language) => language.languageId.toLowerCase()),
  );
  const pending: UnresolvedAreaSelection[] = [];
  const issues: EditorIntentIssue[] = [];

  const regionGroups =
    edits.regionGroups === undefined
      ? baseline.regionGroups.map((group) => retainedGroupWrite(group, resolver, pending, issues))
      : edits.regionGroups.map((group) => editedGroupWrite(group, resolver, pending, issues, activeLanguages));

  const methodTranslations = (edits.methodTranslations ?? []).map((translation) => {
    if (!activeLanguages.has(translation.languageId.toLowerCase())) {
      issues.push({ code: "languageUnavailable", languageId: translation.languageId });
    }
    return {
      languageId: requireId(translation.languageId, "languageId"),
      name: translation.name,
      description: translation.description,
    };
  });

  if (pending.length > 0) return { status: "needsResolution", pending };
  if (issues.length > 0) return { status: "invalidIntent", issues };

  const body: { [key: string]: JsonWrite } = { expectedRevision, regionGroups, methodTranslations };

  if (edits.weeklySchedule) {
    if (edits.weeklySchedule.days.length !== 7) throw new Error("editor_request_invalid:schedule-days");
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
