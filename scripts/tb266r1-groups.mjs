/**
 * TB-266-R1 — read one admin order detail and print ONLY the operational delivery projection.
 */
const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const checkoutId = process.argv[2];
const res = await fetch(`${host}/v1/auth/login`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ identifierKind: "Email", identifier: "admin-actor@tooba.local", password: "admin-dev-horse-1" }),
});
const login = await res.json();
const token = login.accessToken || login.AccessToken;
const headers = { authorization: `Bearer ${token}`, "content-type": "application/json" };
const detail = await (await fetch(`${host}/v1/admin/orders/${checkoutId}`, { headers })).json();
const groups = detail.operationalDeliveryGroups ?? [];
console.log(JSON.stringify({
  status: detail.status,
  paymentState: detail.paymentState,
  shippingMethodLabel: detail.shippingMethodLabel,
  groupCount: groups.length,
  groups: groups.map((g) => ({
    key: g.operationalGroupKey,
    methodLabel: g.methodLabel,
    methodCode: g.methodCode,
    deliveryDate: g.deliveryDate,
    window: g.deliveryTimeWindow,
    responsibility: g.responsibilityLabelFa,
    separate: g.separateReasonFa,
    plannedDispatch: g.plannedDispatch ?? null,
    members: (g.members ?? []).map((m) => ({
      shippingGroupId: m.shippingGroupId,
      sellerOrderId: m.sellerOrderId,
      createParcelEligible: m.createParcelEligible,
      blockReason: m.createParcelBlockReasonFa,
      transportMode: m.transportMode,
      lines: m.lines,
    })),
  })),
  shipments: (detail.sellerOrders ?? []).flatMap((s) => (s.shipments ?? []).map((x) => ({
    shipmentId: x.shipmentId, status: x.status, tracking: x.trackingReference, group: x.shippingGroupId,
  }))),
  lineStatuses: (detail.sellerOrders ?? []).flatMap((s) => (s.lines ?? []).map((l) => ({
    orderLineId: l.orderLineId, operationalStatus: l.operationalStatus,
    packed: l.quantityPacked, processing: l.quantityProcessing, allocated: l.quantityAllocated,
  }))),
}, null, 1));
