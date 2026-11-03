export type ShipmentStateName = "Pending" | "Succeeded" | "Failed" | "Unknown";

export interface ConfiguredSender {
  fullName: string | null;
  mobile: string | null;
  addressLine: string | null;
  postalCode: string | null;
  locationCode: string | null;
}

export interface CapabilityRow {
  shippingGroupId: string;
  methodLabel: string;
  methodCode: string;
  transportMode: string;
  recipientName: string;
  recipientMobile: string;
  recipientAddress: string;
  postalCode: string | null;
  provinceName: string;
  cityName: string;
  originLocationCode: string | null;
  destinationLocationCode: string | null;
  weightGrams: number | null;
  declaredValue: number | null;
  lengthCentimeters: number | null;
  widthCentimeters: number | null;
  heightCentimeters: number | null;
  canCreate: boolean;
  canTrack: boolean;
  canCancel: boolean;
  blockReasonCodes: readonly string[];
  ineligibilityReasonCode: string | null;
  configuredSender: ConfiguredSender;
  packageReady: boolean;
  allowPackageCompletion: boolean;
  hasEditableSenderPostal: boolean;
  hasEditableSenderContact: boolean;
  executableLines: readonly { orderLineId: string; remainingPackedQuantity: number }[];
}

export interface ShipmentView {
  operationId: string;
  shippingGroupId: string;
  state: ShipmentStateName;
  providerTrackingCode: string | null;
  providerStatus: string | null;
  lastErrorCode: string | null;
  canTrack: boolean;
  canCancel: boolean;
}

function asString(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value : null;
}

function asNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function asBool(value: unknown): boolean {
  return value === true;
}

function readSender(raw: unknown): ConfiguredSender {
  if (!raw || typeof raw !== "object") {
    return { fullName: null, mobile: null, addressLine: null, postalCode: null, locationCode: null };
  }
  const item = raw as Record<string, unknown>;
  return {
    fullName: asString(item.fullName ?? item.FullName),
    mobile: asString(item.mobile ?? item.Mobile),
    addressLine: asString(item.addressLine ?? item.AddressLine),
    postalCode: asString(item.postalCode ?? item.PostalCode),
    locationCode: asString(item.locationCode ?? item.LocationCode),
  };
}

export function normalizeShipmentState(value: unknown): ShipmentStateName {
  if (value === 0 || value === "0" || value === "Pending") return "Pending";
  if (value === 1 || value === "1" || value === "Succeeded") return "Succeeded";
  if (value === 2 || value === "2" || value === "Failed") return "Failed";
  if (value === 3 || value === "3" || value === "Unknown") return "Unknown";
  return "Unknown";
}

export function decodeCapability(row: unknown): CapabilityRow | null {
  if (!row || typeof row !== "object") return null;
  const item = row as Record<string, unknown>;
  const shippingGroupId = asString(item.shippingGroupId ?? item.ShippingGroupId);
  if (!shippingGroupId) return null;
  const blockReasonsRaw = item.blockReasonCodes ?? item.BlockReasonCodes;
  const blockReasonCodes = Array.isArray(blockReasonsRaw)
    ? blockReasonsRaw.filter((x): x is string => typeof x === "string")
    : [];
  return {
    shippingGroupId,
    methodLabel: asString(item.methodLabel ?? item.MethodLabel) ?? "—",
    methodCode: asString(item.methodCode ?? item.MethodCode) ?? "",
    transportMode: asString(item.transportMode ?? item.TransportMode) ?? "",
    recipientName: asString(item.recipientName ?? item.RecipientName) ?? "",
    recipientMobile: asString(item.recipientMobile ?? item.RecipientMobile) ?? "",
    recipientAddress: asString(item.recipientAddress ?? item.RecipientAddress) ?? "",
    postalCode: asString(item.postalCode ?? item.PostalCode),
    provinceName: asString(item.provinceName ?? item.ProvinceName) ?? "",
    cityName: asString(item.cityName ?? item.CityName) ?? "",
    originLocationCode: asString(item.originLocationCode ?? item.OriginLocationCode),
    destinationLocationCode: asString(item.destinationLocationCode ?? item.DestinationLocationCode),
    weightGrams: asNumber(item.weightGrams ?? item.WeightGrams),
    declaredValue: asNumber(item.declaredValue ?? item.DeclaredValue),
    lengthCentimeters: asNumber(item.lengthCentimeters ?? item.LengthCentimeters),
    widthCentimeters: asNumber(item.widthCentimeters ?? item.WidthCentimeters),
    heightCentimeters: asNumber(item.heightCentimeters ?? item.HeightCentimeters),
    canCreate: asBool(item.canCreate ?? item.CanCreate),
    canTrack: asBool(item.canTrack ?? item.CanTrack),
    canCancel: asBool(item.canCancel ?? item.CanCancel),
    blockReasonCodes,
    ineligibilityReasonCode: asString(item.ineligibilityReasonCode ?? item.IneligibilityReasonCode),
    configuredSender: readSender(item.configuredSender ?? item.ConfiguredSender),
    packageReady: asBool(item.packageReady ?? item.PackageReady),
    allowPackageCompletion: asBool(item.allowPackageCompletion ?? item.AllowPackageCompletion),
    hasEditableSenderPostal: asBool(item.hasEditableSenderPostal ?? item.HasEditableSenderPostal),
    hasEditableSenderContact: asBool(item.hasEditableSenderContact ?? item.HasEditableSenderContact),
    executableLines: (() => {
      const raw = item.executableLines ?? item.ExecutableLines;
      if (!Array.isArray(raw)) return [];
      return raw
        .map((entry) => {
          if (!entry || typeof entry !== "object") return null;
          const row = entry as Record<string, unknown>;
          const orderLineId = asString(row.orderLineId ?? row.OrderLineId);
          const remainingPackedQuantity = asNumber(row.remainingPackedQuantity ?? row.RemainingPackedQuantity);
          if (!orderLineId || remainingPackedQuantity == null || remainingPackedQuantity <= 0) return null;
          return { orderLineId, remainingPackedQuantity };
        })
        .filter((x): x is { orderLineId: string; remainingPackedQuantity: number } => x != null);
    })(),
  };
}

export function decodeShipment(row: unknown): ShipmentView | null {
  if (!row || typeof row !== "object") return null;
  const item = row as Record<string, unknown>;
  const operationId = asString(item.operationId ?? item.OperationId);
  const shippingGroupId = asString(item.shippingGroupId ?? item.ShippingGroupId);
  if (!operationId || !shippingGroupId) return null;
  return {
    operationId,
    shippingGroupId,
    state: normalizeShipmentState(item.state ?? item.State),
    providerTrackingCode: asString(item.providerTrackingCode ?? item.ProviderTrackingCode),
    providerStatus: asString(item.providerStatus ?? item.ProviderStatus),
    lastErrorCode: asString(item.lastErrorCode ?? item.LastErrorCode),
    canTrack: asBool(item.canTrack ?? item.CanTrack),
    canCancel: asBool(item.canCancel ?? item.CanCancel),
  };
}

export function isCarrierApiGroup(row: CapabilityRow): boolean {
  return row.transportMode === "CarrierApi";
}
