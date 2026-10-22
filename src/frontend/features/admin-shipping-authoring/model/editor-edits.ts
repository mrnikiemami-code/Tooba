/**
 * Editable intent for ONE existing shipping authoring draft.
 *
 * This is deliberately NOT a copy of the read document: a write may only carry what the editor
 * intentionally changed. Retained groups/rows keep their server ids; new children carry a
 * client-local key that is never serialized as an id. Destination selections stay read-shaped
 * (`kind` + `id`) until an authoritative Geography resolution supplies the ancestor chain.
 */
import type {
  AuthoringAreaKind,
  AuthoringCalculationMode,
  DecimalText,
  FreightPaymentMode,
  SelectedAreaRead,
  TimeText,
} from "./editor-document.ts";

/**
 * Canonical write selection for one destination, produced by the existing authoritative Geography
 * resolution path. The adapter never derives parents from ids, labels or catalog codes.
 */
export interface ResolvedAreaAncestry {
  readonly kind: AuthoringAreaKind;
  readonly countryId: string;
  readonly administrativeAreaId?: string | null;
  readonly cityId?: string | null;
  readonly serviceAreaId?: string | null;
}

/**
 * Port the later UI supplies from the real Geography picker.
 *
 * @returns The canonical ancestry for a stored/selected area, or `null` when it cannot be resolved.
 */
export interface AreaAncestryResolver {
  resolve(area: SelectedAreaRead): ResolvedAreaAncestry | null;
}

/** One rate row as edited. `rateRowId === null` marks a NEW row (no invented server id). */
export interface RateRowEdit {
  readonly rowKey: string;
  readonly rateRowId: string | null;
  readonly minWeightGrams: number | null;
  readonly maxWeightGrams: number | null;
  readonly minItemCount: number | null;
  readonly maxItemCount: number | null;
  readonly minMerchandiseAmount: DecimalText | null;
  readonly maxMerchandiseAmount: DecimalText | null;
  readonly freightAmount: DecimalText | null;
  readonly packagingAmount: DecimalText | null;
  readonly minDeliveryDays: number | null;
  readonly maxDeliveryDays: number | null;
}

/** One intentionally touched (group, language) title. `title === null` clears exactly that pair. */
export interface GroupTitleEdit {
  readonly languageId: string;
  readonly title: string | null;
}

/** One group as edited: the full ordered replacement of its areas, rows and touched titles. */
export interface RegionGroupEdit {
  readonly groupKey: string;
  readonly groupId: string | null;
  readonly areas: readonly SelectedAreaRead[];
  readonly rows: readonly RateRowEdit[];
  readonly titles: readonly GroupTitleEdit[];
}

/** One intentionally touched active-language method name/description. */
export interface MethodTranslationEdit {
  readonly languageId: string;
  readonly name: string;
  readonly description: string | null;
}

/** One weekday of an explicitly supplied schedule. */
export interface ScheduleDayEdit {
  readonly weekday: number;
  readonly isWorking: boolean;
  readonly startTime: TimeText | null;
  readonly endTime: TimeText | null;
}

/** Explicit full replacement of the stored weekly schedule. */
export interface WeeklyScheduleEdit {
  readonly timeZoneId: string;
  readonly days: readonly ScheduleDayEdit[];
}

/** Explicit full replacement of both independent settings. */
export interface MethodSettingsEdit {
  readonly freightPaymentMode: FreightPaymentMode;
  readonly calculationMode: AuthoringCalculationMode;
}

/** Explicit type selection intent: `{shippingTypeId: null}` clears, a value selects. */
export interface TypeAssignmentEdit {
  readonly shippingTypeId: string | null;
}

/**
 * Local edit intent of one editor session.
 *
 * Absence (`undefined`) of `weeklySchedule`, `methodSettings` or `typeAssignment` means PRESERVE;
 * a supplied object replaces it, and only `typeAssignment: {shippingTypeId: null}` clears a type.
 */
export interface EditorEdits {
  readonly regionGroups?: readonly RegionGroupEdit[];
  readonly methodTranslations?: readonly MethodTranslationEdit[];
  readonly weeklySchedule?: WeeklyScheduleEdit;
  readonly methodSettings?: MethodSettingsEdit;
  readonly typeAssignment?: TypeAssignmentEdit;
}
