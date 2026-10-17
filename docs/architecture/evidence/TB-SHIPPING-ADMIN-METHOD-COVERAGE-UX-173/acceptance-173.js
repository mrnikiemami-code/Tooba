/**
 * TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — isolated owner-API acceptance for the per-method
 * geographic coverage mode on the private disposable Host (127.0.0.1:5427) and scratch PostgreSQL
 * (127.0.0.1:55493).
 *
 * What this proves through the real HTTP surface (no fabricated UI screenshot):
 *   - a method without a declaration reads as the Unrestricted default;
 *   - the declaration write persists per method identity (service + option) and re-reads the same;
 *   - an option-scoped grant is written for that method only, never for the sibling option;
 *   - the sibling method stays undeclared / grant-free;
 *   - switching back to Unrestricted keeps the historical grant (auditable), and re-reads as such;
 *   - an unauthenticated admin write fails closed.
 *
 * Usage: node acceptance-173.js
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("node:child_process");

const BASE = process.env.TOOBA_173_HOST || "http://127.0.0.1:5427";
const SERVICE = "tipax";
const OPTION = "standard";
const SIBLING = "express";
const COUNTRY = "11111111-1111-1111-1111-111111111111";

const checks = [];
const trace = [];
const record = (id, ok, detail = "") => {
    checks.push({ id, ok, detail });
    console.log(`${ok ? "PASS" : "FAIL"} ${id} ${detail}`);
};

const prop = (o, ...names) => {
    if (!o || typeof o !== "object") return undefined;
    for (const n of names) if (o[n] !== undefined && o[n] !== null) return o[n];
    return undefined;
};
const unwrap = (j) => (j && typeof j === "object" && j.data !== undefined ? j.data : j);
const errorCode = (p) => (p && typeof p === "object" ? p.errorCode || p.code || (p.error && p.error.code) || null : null);

async function call(name, url, init) {
    const res = await fetch(`${BASE}${url}`, init);
    const text = await res.text();
    let json = null;
    try { json = text ? JSON.parse(text) : null; } catch { }
    trace.push(`${name} ${url} -> ${res.status} :: ${text.slice(0, 700).replace(/\s+/g, " ")}`);
    return { status: res.status, json, text };
}

function psql(sql) {
    const container = process.env.TOOBA_173_CONTAINER || "tooba-tb173-pg";
    const user = process.env.TOOBA_173_USER || "scratch173";
    const database = process.env.TOOBA_173_DATABASE || "tooba_173_scratch";
    const password = process.env.TOOBA_173_SCRATCH_PASSWORD || "";
    return execFileSync(
        "docker",
        ["exec", "-e", `PGPASSWORD=${password}`, container, "psql", "-U", user, "-d", database, "-tAc", sql],
        { encoding: "utf8" }).trim();
}

const adminHeaders = (actorId) => ({
    Accept: "application/json",
    "Accept-Language": "fa-IR",
    "X-Tooba-Dev-Actor-User-Id": actorId,
    "Content-Type": "application/json",
});

const rates = (actorId) => call("RATES", `/v1/admin/shipping-services/${SERVICE}/rates`, { headers: adminHeaders(actorId) });
const hasDeclaration = (view, option) => (view.methodCoverages || []).some(
    (row) => prop(row, "serviceCode") === SERVICE && (prop(row, "optionCode") ?? null) === option);
const ownGrants = (view, option) => (view.coverageGrants || []).filter(
    (row) => prop(row, "serviceCode") === SERVICE && (prop(row, "optionCode") ?? null) === option);

(async () => {
    const dev = await call("DEV_CONTEXT", "/v1/admin/dev-context", { headers: { Accept: "application/json" } });
    const actorUserId = prop(unwrap(dev.json) || {}, "actorUserId");
    record("dev_context_available", dev.status === 200 && !!actorUserId, `http=${dev.status}`);

    await call("BOOTSTRAP", "/v1/admin/access-control/bootstrap", {
        method: "POST", headers: adminHeaders(actorUserId), body: "{}",
    });
    const caps = await call("CAPABILITIES", "/v1/admin/access-control/me/capabilities", { headers: adminHeaders(actorUserId) });
    const perms = ((unwrap(caps.json) || {}).permissions || []).map((p) => (typeof p === "string" ? p : prop(p, "permissionId")));
    record("fulfillment_manage_granted", perms.includes("fulfillment.manage"), `granted=${perms.length}`);

    // ---------- 1. Default read: no declaration => Unrestricted default ----------
    const before = await rates(actorUserId);
    const beforeView = unwrap(before.json) || {};
    record("rates_read_ok", before.status === 200, `http=${before.status}`);
    record("method_absent_from_declarations_before_edit",
        !hasDeclaration(beforeView, OPTION),
        `coverages=${(beforeView.methodCoverages || []).length}`);
    record("sibling_absent_from_declarations_before_edit",
        !hasDeclaration(beforeView, SIBLING),
        `sibling_declared=${hasDeclaration(beforeView, SIBLING)}`);

    // ---------- 2. Declare Restricted for the method's own option ----------
    const restricted = await call("SET_RESTRICTED", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
        method: "PUT", headers: adminHeaders(actorUserId),
        body: JSON.stringify({ optionCode: OPTION, mode: "Restricted" }),
    });
    const restrictedBody = unwrap(restricted.json) || {};
    record("restricted_declaration_saved",
        restricted.status === 200 && prop(restrictedBody, "mode") === "Restricted"
        && (prop(restrictedBody, "optionCode") ?? null) === OPTION,
        `http=${restricted.status} mode=${prop(restrictedBody, "mode")} option=${prop(restrictedBody, "optionCode")}`);

    // Idempotent re-save: the same identity must not create a second declaration row.
    const again = await call("SET_RESTRICTED_AGAIN", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
        method: "PUT", headers: adminHeaders(actorUserId),
        body: JSON.stringify({ optionCode: OPTION, mode: "Restricted" }),
    });
    record("declaration_identity_is_stable",
        again.status === 200
        && prop(unwrap(again.json) || {}, "methodCoverageId") === prop(restrictedBody, "methodCoverageId"),
        `id=${prop(restrictedBody, "methodCoverageId")}`);

    // ---------- 3. Method-owned allowed destination ----------
    const grant = await call("CREATE_OPTION_GRANT", `/v1/admin/shipping-services/${SERVICE}/coverage-grants`, {
        method: "POST", headers: adminHeaders(actorUserId),
        body: JSON.stringify({ serviceCode: SERVICE, optionCode: OPTION, scope: { countryId: COUNTRY } }),
    });
    const grantBody = unwrap(grant.json) || {};
    record("option_scoped_grant_saved",
        grant.status === 200 && (prop(grantBody, "optionCode") ?? null) === OPTION
        && prop(grantBody, "countryId") === COUNTRY,
        `http=${grant.status} option=${prop(grantBody, "optionCode")}`);

    // ---------- 4. Reload: the exact choice and destination come from the server ----------
    const reload = await rates(actorUserId);
    const reloadView = unwrap(reload.json) || {};
    const ownDeclaration = (reloadView.methodCoverages || []).find(
        (row) => (prop(row, "optionCode") ?? null) === OPTION);
    record("reload_shows_same_restricted_choice",
        reload.status === 200 && !!ownDeclaration && prop(ownDeclaration, "mode") === "Restricted",
        `mode=${ownDeclaration && prop(ownDeclaration, "mode")}`);
    record("reload_shows_same_destination",
        ownGrants(reloadView, OPTION).some((g) => prop(g, "countryId") === COUNTRY && prop(g, "isActive") === true),
        `own_active_grants=${ownGrants(reloadView, OPTION).length}`);
    record("only_one_declaration_row_for_method",
        (reloadView.methodCoverages || []).filter((row) => (prop(row, "optionCode") ?? null) === OPTION).length === 1,
        `rows=${(reloadView.methodCoverages || []).length}`);

    // ---------- 5. Sibling isolation ----------
    record("sibling_declaration_untouched", !hasDeclaration(reloadView, SIBLING),
        `sibling_declared=${hasDeclaration(reloadView, SIBLING)}`);
    record("sibling_has_no_inherited_grant", ownGrants(reloadView, SIBLING).length === 0,
        `sibling_grants=${ownGrants(reloadView, SIBLING).length}`);

    const dbDeclarations = psql(
        `select count(*) from shipping.method_coverages where service_code='${SERVICE}' and option_code='${OPTION}'`);
    const dbSibling = psql(
        `select count(*) from shipping.method_coverages where service_code='${SERVICE}' and option_code='${SIBLING}'`);
    const dbOwnGrants = psql(
        `select count(*) from shipping.coverage_grants where service_code='${SERVICE}' and option_code='${OPTION}'`);
    const dbSiblingGrants = psql(
        `select count(*) from shipping.coverage_grants where service_code='${SERVICE}' and option_code='${SIBLING}'`);
    record("db_identity_rows_match",
        dbDeclarations === "1" && dbSibling === "0" && dbOwnGrants === "1" && dbSiblingGrants === "0",
        `decl_own=${dbDeclarations} decl_sibling=${dbSibling} grant_own=${dbOwnGrants} grant_sibling=${dbSiblingGrants}`);

    // ---------- 6. Mode switch back to Unrestricted keeps history ----------
    const unrestricted = await call("SET_UNRESTRICTED", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
        method: "PUT", headers: adminHeaders(actorUserId),
        body: JSON.stringify({ optionCode: OPTION, mode: "Unrestricted" }),
    });
    record("unrestricted_saved", unrestricted.status === 200
        && prop(unwrap(unrestricted.json) || {}, "mode") === "Unrestricted",
        `http=${unrestricted.status}`);

    const after = await rates(actorUserId);
    const afterView = unwrap(after.json) || {};
    const afterDeclaration = (afterView.methodCoverages || []).find(
        (row) => (prop(row, "optionCode") ?? null) === OPTION);
    record("reload_shows_unrestricted", !!afterDeclaration && prop(afterDeclaration, "mode") === "Unrestricted",
        `mode=${afterDeclaration && prop(afterDeclaration, "mode")}`);
    record("grants_not_deleted_by_mode_switch", ownGrants(afterView, OPTION).length === 1,
        `own_grants=${ownGrants(afterView, OPTION).length}`);
    record("sibling_still_isolated_after_switch", !hasDeclaration(afterView, SIBLING)
        && ownGrants(afterView, SIBLING).length === 0);

    // ---------- 7. Authorization fails closed ----------
    const anonymous = await call("SET_COVERAGE_ANONYMOUS", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
        method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ optionCode: OPTION, mode: "Restricted" }),
    });
    record("anonymous_write_denied", anonymous.status === 401 || anonymous.status === 403,
        `http=${anonymous.status} code=${errorCode(anonymous.json)}`);

    const finalView = unwrap((await rates(actorUserId)).json) || {};
    const finalDeclaration = (finalView.methodCoverages || []).find(
        (row) => (prop(row, "optionCode") ?? null) === OPTION);
    record("denied_write_changed_nothing",
        !!finalDeclaration && prop(finalDeclaration, "mode") === "Unrestricted",
        `mode=${finalDeclaration && prop(finalDeclaration, "mode")}`);

    const outDir = path.join(process.cwd(), ".tmp-173-run");
    fs.mkdirSync(outDir, { recursive: true });
    const evidenceDir = __dirname;
    fs.writeFileSync(path.join(evidenceDir, "assertion-matrix-173.json"), JSON.stringify(checks, null, 2) + "\n");
    fs.writeFileSync(path.join(evidenceDir, "http-trace-173.txt"), trace.join("\n") + "\n");
    const failed = checks.filter((c) => !c.ok);
    console.log(`TOTAL=${checks.length} PASS=${checks.length - failed.length} FAIL=${failed.length}`);
    process.exit(failed.length === 0 ? 0 : 4);
})().catch((error) => {
    console.error("ACCEPTANCE_ERROR", error && error.message);
    process.exit(5);
});
