// TB-TMAR-FULFILLMENT-AMSC-001 W2 (part 5): add the Shipping.Ports using to the two
// infrastructure implementers and remove the duplicate using directives introduced by
// the namespace rewrite.
const fs = require("fs");
const path = require("path");
const repoRoot = path.resolve(__dirname, "../../../..");
const mod = path.join(repoRoot, "src/backend/Modules/Fulfillment");

const addUsing = (rel, ns) => {
  const p = path.join(mod, rel);
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

addUsing("Tooba.Fulfillment.Infrastructure/Shipping/ShippingServiceDirectory.cs", "Tooba.Fulfillment.Application.Shipping.Ports");
addUsing("Tooba.Fulfillment.Infrastructure/Shipping/ShippingCatalogSeedAdapter.cs", "Tooba.Fulfillment.Application.Shipping.Ports");

const dedupe = (rel) => {
  const p = path.join(mod, rel);
  const lines = fs.readFileSync(p, "utf8").split("\n");
  const seen = new Set();
  const out = [];
  let removed = 0;
  for (const line of lines) {
    const m = /^using ([^;]+);$/.exec(line.trim());
    if (m) {
      if (seen.has(m[1])) { removed++; continue; }
      seen.add(m[1]);
    }
    out.push(line);
  }
  fs.writeFileSync(p, out.join("\n"));
  console.log("deduped", removed, "usings in", rel);
};

dedupe("Tooba.Fulfillment.Endpoints/Admin/FulfillmentAdminEndpoints.cs");
dedupe("Tooba.Fulfillment.Endpoints/Seller/FulfillmentSellerEndpoints.cs");
dedupe("Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs");
