/**
 * TB-P09-T022-R1 — FE HTTP smoke for Admin (single/multi) + customer-panel.
 * Exits 1 if any URL is not HTTP 200. Requires FE Ready on :3000.
 */
import { execFileSync } from "node:child_process";

const FE = process.env.TOOBA_FE_ORIGIN || "http://127.0.0.1:3000";

const SINGLE = "01a08973-d831-7000-ae48-d6f8a6bc3fcf";
const MULTI_DELIVERED = "01a08973-dd8c-7000-b205-cc7f15358dfb";
const FRESH_MULTI = "01a0898a-0d7d-7000-b5ed-7f5d85a29241";

const urls = [
  { name: "admin-single", url: `${FE}/fa/admin/orders/${SINGLE}` },
  { name: "admin-multi-delivered", url: `${FE}/fa/admin/orders/${MULTI_DELIVERED}` },
  { name: "admin-fresh-multi", url: `${FE}/fa/admin/orders/${FRESH_MULTI}` },
  { name: "customer-panel", url: `${FE}/fa/customer-panel/orders/${FRESH_MULTI}` },
];

function httpStatus(url) {
  const raw = execFileSync(
    "curl.exe",
    ["-sS", "-o", "NUL", "-w", "%{http_code}", "-L", "--max-time", "30", url],
    { encoding: "utf8" },
  );
  return Number.parseInt(String(raw).trim(), 10);
}

let failed = 0;
for (const { name, url } of urls) {
  let status;
  try {
    status = httpStatus(url);
  } catch (err) {
    console.error(`FAIL ${name} ${url} — curl error: ${err?.message || err}`);
    failed += 1;
    continue;
  }
  const ok = status === 200;
  console.log(`${ok ? "OK" : "FAIL"} ${status} ${name} ${url}`);
  if (!ok) failed += 1;
}

if (failed > 0) {
  console.error(`_r1_http_smoke: ${failed}/${urls.length} failed`);
  process.exit(1);
}
console.log(`_r1_http_smoke: ${urls.length}/${urls.length} HTTP 200`);
