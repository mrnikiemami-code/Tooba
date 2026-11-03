/**
 * Builds the Shipping carrier shipment create JSON body.
 * Partial line selections require Package weight/dims; full-remaining may omit Package.
 * packageDispatchId is an optional untrusted client locator when the UI holds one.
 */
export function buildCarrierShipmentCreateBody(input: {
  checkoutId: string;
  shippingGroupId: string;
  lines: readonly { orderLineId: string; quantity: number }[];
  executableLines: readonly { orderLineId: string; remainingPackedQuantity: number }[];
  package?: {
    weightGrams: number;
    lengthCentimeters: number;
    widthCentimeters: number;
    heightCentimeters: number;
  } | null;
  packageDispatchId?: string | null;
}): Record<string, unknown> {
  const body: Record<string, unknown> = {
    checkoutId: input.checkoutId,
    shippingGroupId: input.shippingGroupId,
    lines: input.lines.map((line) => ({
      orderLineId: line.orderLineId,
      quantity: line.quantity,
    })),
  };

  const selectsAllRemaining =
    input.executableLines.length > 0
    && input.lines.length === input.executableLines.length
    && input.executableLines.every((exec) =>
      input.lines.some(
        (line) =>
          line.orderLineId === exec.orderLineId
          && line.quantity === exec.remainingPackedQuantity,
      ),
    );
  const isPartial = !selectsAllRemaining;
  const pkg = input.package;
  const packageValid =
    pkg != null
    && Number.isFinite(pkg.weightGrams)
    && pkg.weightGrams > 0
    && Number.isFinite(pkg.lengthCentimeters)
    && pkg.lengthCentimeters > 0
    && Number.isFinite(pkg.widthCentimeters)
    && pkg.widthCentimeters > 0
    && Number.isFinite(pkg.heightCentimeters)
    && pkg.heightCentimeters > 0;

  if (packageValid) {
    body.package = {
      weightGrams: pkg!.weightGrams,
      lengthCentimeters: pkg!.lengthCentimeters,
      widthCentimeters: pkg!.widthCentimeters,
      heightCentimeters: pkg!.heightCentimeters,
    };
  } else if (isPartial) {
    // Partial without package facts is intentionally left for the server to refuse;
    // still mark the intent so tests can assert the client attempted the partial path.
    body.partialWithoutPackage = true;
  }

  const locator = typeof input.packageDispatchId === "string" ? input.packageDispatchId.trim() : "";
  if (locator.length > 0) {
    body.packageDispatchId = locator;
  }

  return body;
}
