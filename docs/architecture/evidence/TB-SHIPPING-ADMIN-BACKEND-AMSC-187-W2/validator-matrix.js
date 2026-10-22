const fs = require("fs");
const path = require("path");
const root = "src/backend/Modules/Shipping/Tooba.Shipping.Application";
const out = [];
function walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full);
    else if (entry.name.endsWith("Validators.cs")) {
      const text = fs.readFileSync(full, "utf8");
      const matches = [...text.matchAll(/class (\w+)\s*:\s*AbstractValidator<(\w+)>/g)];
      for (const m of matches) {
        out.push([path.relative(root, full).replace(/\\/g, "/"), m[1], m[2]].join(" | "));
      }
    }
  }
}
walk(root);
console.log(out.sort().join("\n"));
console.log("TOTAL", out.length);
