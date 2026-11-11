// TB-263 read-only probe: operations projection + damaged decision facts of the user's screenshot order.
const HOST = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const HEADERS = {
  Accept: "application/json",
  "Accept-Language": "fa-IR",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
};

async function get(path) {
  const res = await fetch(`${HOST}${path}`, { headers: HEADERS });
  const text = await res.text();
  let body = null;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 400); }
  return { status: res.status, body };
}

async function main() {
  const checkoutId = process.argv[2] ?? "13feece0-4890-4e45-b92f-4d4b1a5e0c97";
  const ops = await get(`/v1/admin/orders/${checkoutId}/operations`);
  console.log("OPS", ops.status);
  if (ops.status !== 200) { console.log(JSON.stringify(ops.body)); return; }
  const actions = (ops.body.actions ?? []).filter((a) => a.code === "resolve_damaged_goods");
  console.log("DAMAGED-ACTIONS", JSON.stringify(actions, null, 2));
  const detail = await get(`/v1/admin/orders/${checkoutId}`);
  console.log("DETAIL", detail.status);
  if (detail.status !== 200) { console.log(JSON.stringify(detail.body).slice(0, 800)); return; }
  for (const seller of detail.body.sellerOrders ?? []) {
    for (const shipment of seller.shipments ?? []) {
      console.log("SHIP", JSON.stringify({
        sellerOrderId: seller.id,
        fulfillmentId: seller.fulfillmentId,
        shipmentId: shipment.shipmentId,
        status: shipment.status,
        resendOf: shipment.resendOfShipmentId,
        receipt: shipment.returnReceiptLines,
        damaged: shipment.damagedResolutions,
        lines: (shipment.lines ?? []).map((l) => ({ id: l.orderLineId, qty: l.quantity, outstanding: l.outstandingDamagedQuantity })),
      }, null, 2));
    }
    for (const line of seller.lines ?? []) {
      console.log("LINE", JSON.stringify({ sellerOrderId: seller.id, id: line.orderLineId, title: line.title, qty: line.quantity, packed: line.quantityPacked, allocated: line.quantityAllocated, shipped: line.quantityShipped, usable: line.returnedUsableQuantity, resendable: line.resendableQuantity, op: line.operationalStatus }));
    }
  }
}

main();
