// TB-TMAR-FULFILLMENT-AMSC-001 W2 (part 3): post-move using repairs.
const fs = require("fs");
const path = require("path");
const repoRoot = path.resolve(__dirname, "../../../..");
const app = path.join(repoRoot, "src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application");

const addUsing = (rel, ns) => {
  const p = path.join(app, rel);
  let t = fs.readFileSync(p, "utf8");
  if (t.includes(`using ${ns};`)) { console.log("already", rel); return; }
  const lines = t.split("\n");
  let last = -1;
  for (let i = 0; i < lines.length; i++) if (/^using .*;$/.test(lines[i].trim())) last = i;
  if (last < 0) throw new Error("no using block in " + rel);
  lines.splice(last + 1, 0, `using ${ns};`);
  fs.writeFileSync(p, lines.join("\n"));
  console.log("added", ns, "->", rel);
};

addUsing("Shipping/Commands/CreateShippingServiceCommand.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Shipping/Commands/UpdateShippingServiceCommand.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Shipping/Commands/DeactivateShippingServiceCommand.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Shipping/Commands/EnsureShippingCatalogSeedCommand.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Shipping/Queries/GetShippingServiceQuery.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Shipping/Queries/ListShippingServicesQuery.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Shipping/Queries/ListEnabledShippingMethodsTreeQuery.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Fulfillments/Ports/ISellerFulfillmentAuthorizer.cs", "Tooba.Fulfillment.Application.WorkQueue.Models");
