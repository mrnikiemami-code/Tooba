const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../../../..");
const endpointsRoot = path.join(repoRoot, "src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin");
const appRoot = path.join(repoRoot, "src/backend/Modules/Shipping/Tooba.Shipping.Application");

function walk(dir, ext = ".cs") {
  const out = [];
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (e.name !== "obj" && e.name !== "bin") out.push(...walk(full, ext));
    } else if (e.name.endsWith(ext)) out.push(full);
  }
  return out;
}

// 1. shipped routes
const routes = [];
for (const file of fs.readdirSync(endpointsRoot).filter((f) => f.endsWith("Endpoints.cs"))) {
  const text = fs.readFileSync(path.join(endpointsRoot, file), "utf8");
  for (const m of text.matchAll(/group\.Map(Get|Post|Put|Patch|Delete)\(\s*\n?\s*"([^"]+)",\s*(\w+)/g)) {
    const handler = m[3];
    const idx = text.indexOf("private static async Task<IResult> " + handler);
    const body = idx < 0 ? "" : text.slice(idx, text.indexOf("\n    }", idx));
    const req = (body.match(/sender\.Send\(\s*\n?\s*new (\w+)/) || [])[1] || "?";
    const perm = /ShippingAdminPermissions\.(\w+)/.exec(body);
    routes.push({ file, verb: m[1].toUpperCase(), pattern: m[2], handler, req, perm: perm ? perm[1] : "-" });
  }
}

// 2. requests + handlers
const appFiles = walk(appRoot);
const requestTypes = new Map();
const handlerTypes = new Map();
for (const file of appFiles) {
  const text = fs.readFileSync(file, "utf8");
  for (const m of text.matchAll(/public sealed record (\w+)\s*\(([\s\S]*?)\)\s*:\s*IRequest<([^>]+)>/g)) {
    requestTypes.set(m[1], {
      file: path.relative(repoRoot, file).replace(/\\/g, "/"),
      args: m[2].replace(/\s+/g, " ").trim(),
      result: m[3].trim(),
    });
  }
  for (const m of text.matchAll(/(?:internal|public) sealed class (\w+)\s*:\s*IRequestHandler<(\w+),\s*([^>]+)>/g)) {
    handlerTypes.set(m[2], { handler: m[1], file: path.relative(repoRoot, file).replace(/\\/g, "/") });
  }
}

// 3. validators
const validators = new Map();
for (const file of walk(appRoot).filter((f) => f.endsWith("Validators.cs"))) {
  const text = fs.readFileSync(file, "utf8");
  for (const m of text.matchAll(/class (\w+)\s*:\s*AbstractValidator<(\w+)>/g)) {
    const target = m[2];
    if (!validators.has(target)) validators.set(target, []);
    validators.get(target).push({ name: m[1], file: path.relative(repoRoot, file).replace(/\\/g, "/") });
  }
}

// 4. matrix
const rows = routes.map((r) => ({
  ...r,
  isRequest: requestTypes.has(r.req),
  handler: handlerTypes.get(r.req)?.handler ?? "MISSING",
  validator: (validators.get(r.req) || []).map((v) => v.name).join(",") || "NONE",
}));

console.log("ROUTES", routes.length);
for (const r of rows) {
  console.log(
    [
      r.verb,
      r.pattern,
      r.req,
      r.isRequest ? "IRequest" : "NOT_IREQUEST",
      r.handler,
      r.validator,
      r.perm,
      r.file,
    ].join(" | "),
  );
}
console.log("REQUESTS_DECLARED", requestTypes.size);
console.log("HANDLERS_DECLARED", handlerTypes.size);
console.log("VALIDATED_TARGETS", validators.size);
const reachable = new Set(rows.map((r) => r.req));
const unreachable = [...requestTypes.keys()].filter((k) => !reachable.has(k));
console.log("UNREACHABLE_REQUESTS", JSON.stringify(unreachable));
const missingHandler = [...reachable].filter((k) => !handlerTypes.has(k));
console.log("REACHABLE_WITHOUT_HANDLER", JSON.stringify(missingHandler));
const missingValidator = [...reachable].filter((k) => !validators.has(k));
console.log("REACHABLE_WITHOUT_VALIDATOR", JSON.stringify(missingValidator));
const nestedOnly = [...validators.keys()].filter((k) => !reachable.has(k));
console.log("NESTED_VALIDATOR_TARGETS", JSON.stringify(nestedOnly));
