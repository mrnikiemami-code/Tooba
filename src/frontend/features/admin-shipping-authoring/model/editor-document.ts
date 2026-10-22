/**
 * Typed read model for ONE existing shipping authoring draft editor document.
 *
 * Mirrors the accepted backend DTOs (`ShippingAuthoringEditorDocumentItem` and children) exactly:
 * camelCase fields, string enums, `decimal` amounts and `TimeOnly` values kept as their exact wire
 * text so nothing is truncated, rounded or re-formatted on the way to the editor. Optional sections
 * that the server omits when null are modelled as `null` — never fabricated.
 */

/** Destination level where a selection stops (contract `ShippingAuthoringAreaKind`). */
export type AuthoringAreaKind = "Country" | "AdministrativeArea" | "City" | "ServiceArea";

/** Freight payment timing (contract `ShippingFreightPayerMode`). */
export type FreightPaymentMode = "Prepaid" | "CollectOnDelivery";

/** Rate calculation mode (contract `ShippingAuthoringCalculationMode`). */
export type AuthoringCalculationMode = "Manual" | "CarrierApi" | "Free";

/** Authorized owner kind of the draft (contract `ShippingAuthoringOwnerKind`). */
export type AuthoringOwnerKind = "Store" | "Marketplace" | "Seller";

/** Result of resolving a stored shipping type id against the live catalog observation. */
export type AuthoringTypeResolution = "Unconfigured" | "Active" | "Inactive" | "Missing";

/** Exact decimal amount text as transported (`0.50` stays `0.50`). */
export type DecimalText = string;

/** Exact `TimeOnly` wire text as transported (`09:30:00` / `09:30:00.5000000`). */
export type TimeText = string;

/** A stored destination selection as READ: identity only, no ancestor chain. */
export interface SelectedAreaRead {
  readonly kind: AuthoringAreaKind;
  readonly id: string;
}

/** A stored rate row of one group. Amounts/durations stay nullable exactly as stored. */
export interface EditorRateRow {
  readonly rateRowId: string;
  readonly minWeightGrams: number | null;
  readonly maxWeightGrams: number | null;
  readonly minItemCount: number | null;
  readonly maxItemCount: number | null;
  readonly minMerchandiseAmount: DecimalText | null;
  readonly maxMerchandiseAmount: DecimalText | null;
  readonly freightAmount: DecimalText;
  readonly packagingAmount: DecimalText;
  readonly minDeliveryDays: number;
  readonly maxDeliveryDays: number;
}

/** A stored group title for one content language (raw, not trimmed, not re-selected). */
export interface EditorGroupTitle {
  readonly languageId: string;
  readonly title: string;
}

/** One region group of the saved document. */
export interface EditorRegionGroup {
  readonly groupId: string;
  readonly order: number;
  readonly currencyCode: string | null;
  readonly areas: readonly SelectedAreaRead[];
  readonly rows: readonly EditorRateRow[];
  readonly titles: readonly EditorGroupTitle[];
}

/** One active content language with its real metadata. */
export interface EditorContentLanguage {
  readonly languageId: string;
  readonly code: string;
  readonly culture: string;
  readonly urlPrefix: string;
  readonly direction: "rtl" | "ltr";
  readonly displayName: string;
  readonly nativeName: string;
  readonly isDefault: boolean;
  readonly sortOrder: number;
}

/** One stored method name/description for one language (raw stored text). */
export interface EditorMethodTranslation {
  readonly languageId: string;
  readonly name: string;
  readonly description: string | null;
}

/** Multilingual presentation section of the document. */
export interface EditorPresentation {
  readonly methodTranslations: readonly EditorMethodTranslation[];
  readonly groupTitles: readonly EditorGroupTitle[];
  readonly contentLanguages: readonly EditorContentLanguage[];
  readonly defaultContentLanguageId: string;
  readonly hasDefaultLanguageName: boolean;
}

/** One weekday of the stored operating schedule. */
export interface EditorScheduleDay {
  readonly weekday: number;
  readonly isWorking: boolean;
  readonly startTime: TimeText | null;
  readonly endTime: TimeText | null;
}

/** Stored weekly schedule (`null` = unconfigured, not seven implicit working days). */
export interface EditorWeeklySchedule {
  readonly timeZoneId: string;
  readonly days: readonly EditorScheduleDay[];
}

/** Stored method settings (`null` = unconfigured; no implicit Prepaid/Manual). */
export interface EditorMethodSettings {
  readonly freightPaymentMode: FreightPaymentMode;
  readonly calculationMode: AuthoringCalculationMode;
}

/** One catalog translation of the assigned shipping type. */
export interface EditorTypeTranslation {
  readonly languageId: string;
  readonly name: string;
  readonly description: string | null;
}

/** The observed shipping type definition. */
export interface EditorShippingType {
  readonly shippingTypeId: string;
  readonly code: string;
  readonly sortOrder: number;
  readonly revision: number;
  readonly translations: readonly EditorTypeTranslation[];
}

/** Current type assignment of the draft plus its honest resolution. */
export interface EditorTypeAssignment {
  readonly draftId: string;
  readonly revision: number;
  readonly shippingTypeId: string | null;
  readonly resolution: AuthoringTypeResolution;
  readonly shippingType: EditorShippingType | null;
}

/** Authorized owner view returned by the server (never supplied by the browser). */
export interface EditorOwnerView {
  readonly kind: AuthoringOwnerKind;
  readonly sellerPartyId: string | null;
}

/** The whole editor document of one existing draft. */
export interface EditorDocument {
  readonly draftId: string;
  readonly owner: EditorOwnerView;
  readonly serviceCode: string;
  readonly optionCode: string | null;
  readonly revision: number;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly type: EditorTypeAssignment;
  readonly settings: EditorMethodSettings | null;
  readonly presentation: EditorPresentation;
  readonly weeklySchedule: EditorWeeklySchedule | null;
  readonly regionGroups: readonly EditorRegionGroup[];
}

/** A read selection that still needs its authoritative ancestor chain before it can be written. */
export interface UnresolvedAreaSelection {
  readonly groupId: string | null;
  readonly area: SelectedAreaRead;
}
