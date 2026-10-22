const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../../../..");
const appRoot = path.join(repoRoot, "src/backend/Modules/Shipping/Tooba.Shipping.Application");

function walk(dir) {
  const out = [];
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (e.name !== "obj" && e.name !== "bin") out.push(...walk(full));
    } else if (e.name.endsWith(".cs")) out.push(full);
  }
  return out;
}

const handlers = new Map();
for (const file of walk(appRoot)) {
  const text = fs.readFileSync(file, "utf8");
  for (const m of text.matchAll(/class\s+(\w+Handler)\s*[\s\S]{0,400}?:?\s*IRequestHandler<\s*(\w+)\s*,/g)) {
    handlers.set(m[2], { handler: m[1], file: path.relative(repoRoot, file).replace(/\\/g, "/") });
  }
}
console.log("HANDLERS", handlers.size);
for (const [k, v] of [...handlers].sort()) console.log(k, "->", v.handler);
