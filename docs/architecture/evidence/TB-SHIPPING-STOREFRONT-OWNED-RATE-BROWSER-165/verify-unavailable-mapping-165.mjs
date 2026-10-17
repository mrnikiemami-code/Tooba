/**
 * TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165 — verify the frontend consumption of the REAL
 * TB-164 authoritative projection (gate ON) through the shipped normalization + notice model.
 *
 * Read-only: reads the TB-164 evidence payload, never writes it. Produces the exact notices the
 * customer page renders, so reason translation and per-method attribution are proven against real
 * server output rather than a hand-written fixture.
 */
import fs from "node:fs";
import path from "node:path";

import { mapShippingProjection } from "../../../../src/frontend/app/storefront/storefront-shipping-api.ts";
import { resolveShippingUnavailableNotices } from "../../../../src/frontend/app/storefront/storefront-shipping-unavailable-methods.ts";

const repo = process.cwd();
const evid = path.join(repo, "docs", "architecture", "evidence", "TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165");
const source = path.join(repo, "docs", "architecture", "evidence", "TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164", "projection-negative-restricted-164.json");
const onSource = path.join(repo, "docs", "architecture", "evidence", "TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164", "projection-on-164.json");

function sanitize(value) {
  return JSON.parse(
    JSON.stringify(value).replace(/("(?:accessToken|refreshToken|guestSecret|secret|token|sessionId)"\s*:\s*")[^"]*(")/g, "$1REDACTED$2"),
  );
}

const restricted = mapShippingProjection(JSON.parse(fs.readFileSync(source, "utf8")));
const restrictedNotices = resolveShippingUnavailableNotices({
  unavailableMethods: restricted.unavailableMethods,
  methods: restricted.methods,
});
const on = mapShippingProjection(JSON.parse(fs.readFileSync(onSource, "utf8")));
const onNotices = resolveShippingUnavailableNotices({ unavailableMethods: on.unavailableMethods, methods: on.methods });

const eligible = restricted.methods.map((m) => ({ methodCode: m.methodCode, label: m.label, priceAmount: m.priceAmount }));
const output = {
  source: "TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164/projection-negative-restricted-164.json (read-only)",
  restricted: sanitize({
    eligibleMethods: eligible,
    unavailableMethods: restricted.unavailableMethods,
    notices: restrictedNotices,
    standardStillEligible: eligible.some((m) => m.methodCode === "tipax:standard"),
    expressIsNotSelectable: !eligible.some((m) => m.methodCode === "tipax:express"),
    everyNoticeIsMethodScoped: restrictedNotices.every((n) => n.methodCode.length > 0 && "priceAmount" in n === false),
    noFreeOrZeroRendered: restrictedNotices.every((n) => !/رایگان|۰|0/.test(n.reasonText)),
  }),
  onGate: sanitize({
    eligibleMethods: on.methods.map((m) => ({ methodCode: m.methodCode, priceAmount: m.priceAmount })),
    unavailableMethods: on.unavailableMethods,
    notices: onNotices,
  }),
};
fs.mkdirSync(evid, { recursive: true });
fs.writeFileSync(path.join(evid, "mapping-on-real-payload-165.json"), JSON.stringify(output, null, 2) + "\n");
console.log(JSON.stringify(output, null, 2));
