const fs = require("fs");
const path = require("path");
const dir = "src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin";
const lines = [];
for (const f of fs.readdirSync(dir).filter((x) => x.endsWith("Endpoints.cs"))) {
  const t = fs.readFileSync(path.join(dir, f), "utf8");
  const maps = [...t.matchAll(/group\.Map(Get|Post|Put|Patch|Delete)\(\s*\n?\s*"([^"]+)",\s*(\w+)/g)];
  for (const m of maps) {
    const handler = m[3];
    const idx = t.indexOf("private static async Task<IResult> " + handler);
    const body = idx < 0 ? "" : t.slice(idx, t.indexOf("\n    }", idx));
    const req = (body.match(/sender\.Send\(\s*\n?\s*new (\w+)/) || [])[1] || "?";
    const perm = /RequireAuthorizedAsync\(context, ShippingAdminPermissions\.(\w+)/.exec(body);
    lines.push([f, m[1].toUpperCase(), m[2], req, perm ? perm[1] : "-"].join(" | "));
  }
}
console.log(lines.join("\n"));
console.log("TOTAL", lines.length);
