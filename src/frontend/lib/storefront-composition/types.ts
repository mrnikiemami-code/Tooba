/** Shared Home+Landing composition engine types. No arbitrary CSS/HTML/JS settings. */

export const SIZE_PRESETS = ["Compact", "Medium", "Large", "ExtraLarge"] as const;
export type SizePreset = (typeof SIZE_PRESETS)[number];

export const SURFACE_ROLES = ["page", "section", "alternate", "accent"] as const;
export type CompositionSurfaceRole = (typeof SURFACE_ROLES)[number];

export const DATA_SOURCE_KINDS = [
  "Manual",
  "Category",
  "Brand",
  "Newest",
  "BestSelling",
  "MostViewed",
  "Discounted",
  "Featured",
  "HotTrending",
  "LatestArticles",
  "ApprovedReviews",
] as const;
export type DataSourceKind = (typeof DATA_SOURCE_KINDS)[number];

export type DataSourceSupport = "Supported" | "HeuristicHomeOnly" | "Deferred";

export type VariantStatus = "Existing" | "ReusableViaAdapter" | "NewRequiredLater";

export type ControlledSettingKey =
  | "title"
  | "subtitle"
  | "ctaLabel"
  | "ctaHref"
  | "itemCount"
  | "dataSource"
  | "layoutPreset"
  | "heightPreset"
  | "autoplay"
  | "enabled"
  | "surfaceRole";

export const FORBIDDEN_SETTING_KEYS = [
  "css",
  "className",
  "classNames",
  "style",
  "customCss",
  "html",
  "javascript",
  "breakpoint",
  "breakpoints",
  "mobileColumns",
  "widthPx",
  "heightPx",
  "gridTemplate",
] as const;

export type ResponsiveBreakpointMap = {
  desktop: string;
  tablet: string;
  mobile: string;
};

export type ResponsiveContract = {
  variantKey: string;
  columns: ResponsiveBreakpointMap;
  height: ResponsiveBreakpointMap;
  itemVisible: ResponsiveBreakpointMap;
  notesFa: string;
};

export type ControlledSettingsSchema = {
  allowedKeys: ControlledSettingKey[];
  defaults: Partial<Record<ControlledSettingKey, string | number | boolean>>;
};

export type SectionTypeDefinition = {
  key: string;
  nameFa: string;
  descriptionFa: string;
  homeAllowed: boolean;
  landingAllowed: boolean;
  defaultSurfaceRole: CompositionSurfaceRole;
  defaultVariantKey: string;
  settings: ControlledSettingsSchema;
};

export type VariantDefinition = {
  key: string;
  sectionTypeKey: string;
  nameFa: string;
  descriptionFa: string;
  status: VariantStatus;
  responsiveContractKey: string;
  settings: ControlledSettingsSchema;
};

export type CompositionSectionInstance = {
  id: string;
  sectionTypeKey: string;
  variantKey: string;
  enabled: boolean;
  displayOrder: number;
  settings: Record<string, unknown>;
  surfaceRole: CompositionSurfaceRole;
};

export type IndustryTemplateDefinition = {
  templateKey: string;
  nameFa: string;
  industry: string;
  descriptionFa: string;
  previewAsset: string;
  version: number;
  active: boolean;
  sectionPresetList: Array<{
    sectionTypeKey: string;
    variantKey: string;
    dataSourceIntent: string;
  }>;
};
