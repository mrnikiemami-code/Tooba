/**
 * TB-SHIPPING-ADMIN-METHOD-PACKAGING-176 — isolated owner-API acceptance for the method-owned
 * packaging / freight-payer policy (Admin tab «بسته‌بندی و کرایه») on a disposable loopback Host.
 *
 * Proves, through the real authorized Shipping owner endpoints only:
 *   - the policy identity is (serviceCode, optionCode) from the method binding, never the method text;
 *   - a method-owned policy is created, re-read and updated without touching the sibling option;
 *   - the legacy service-level (optionCode = null) row stays auditable and is never mutated by a
 *     method edit, and is never borrowed as "this method's policy";
 *   - activation only flips the addressed row and keeps it auditable;
 *   - unknown option, body/path service mismatch, cross-service PATCH and anonymous writes fail closed.
 *
 * No fee, payer mode, settlement share or customer amount is invented. The customer checkout effect
 * is deliberately NOT asserted as changed by these writes.
 *
 * Usage:
 *   TOOBA_176_SCRATCH_PASSWORD=... node acceptance-176.js
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("node:child_process");

const BASE = process.env.TOOBA_176_HOST || "http://127.0.0.1:5430";
const EVIDENCE = __dirname;
const SERVICE = "tipax";
const OPTION = "standard";
const SIBLING = "express";
const OTHER_SERVICE = "post";
const UNKNOWN_OPTION = "scratch_unknown_176";

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
const errorCode = (p) => (p && typeof p === "object"
    ? p.errorCode || p.code || (p.error && p.error.code) || null
    : null);

function psql(sql) {
    const container = process.env.TOOBA_176_CONTAINER || "tooba-tb176-pg";
    const user = process.env.TOOBA_176_USER || "scratch176";
    const database = process.env.TOOBA_176_DATABASE || "tooba_176_scratch";
    const password = process.env.TOOBA_176_SCRATCH_PASSWORD || "";
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

async function call(name, url, init) {
    const res = await fetch(`${BASE}${url}`, init);
    const text = await res.text();
    let json = null;
    try { json = text ? JSON.parse(text) : null; } catch { }
    trace.push(`${name} ${url} -> ${res.status} :: ${text.slice(0, 700).replace(/\s+/g, " ")}`);
    return { status: res.status, json, text };
}

const policiesUrl = (service) =>
    `/v1/admin/shipping-services/${encodeURIComponent(service)}/packaging-policies`;
const listPolicies = (actorId, service = SERVICE) =>
    call("LIST", policiesUrl(service), { headers: adminHeaders(actorId) });
const rowUrl = (service, id) => `${policiesUrl(service)}/${encodeURIComponent(id)}`;

function createBody(actorId, optionCode, feeAmount, countryId, payerMode = "Prepaid") {
    return {
        serviceCode: SERVICE,
        optionCode,
        scope: {
            countryId,
            administrativeAreaId: null,
            cityId: null,
            serviceAreaId: null,
        },
        feeAmount,
        currencyCode: "IRR",
        freightPayerMode: payerMode,
    };
}

const rowsFor = (view, service, option) => (prop(view, "packagingPolicies") || []).filter(
    (row) => prop(row, "serviceCode") === service && (prop(row, "optionCode") ?? null) === option);

(async () => {
    // ---------- 0. Authorized Development Admin actor ----------
    const dev = await call("DEV_CONTEXT", "/v1/admin/dev-context", { headers: { Accept: "application/json" } });
    const actorUserId = prop(unwrap(dev.json) || {}, "actorUserId");
    record("dev_context_available", dev.status === 200 && !!actorUserId, `http=${dev.status}`);

    await call("BOOTSTRAP", "/v1/admin/access-control/bootstrap", {
        method: "POST", headers: adminHeaders(actorUserId), body: "{}",
    });
    const caps = await call("CAPABILITIES", "/v1/admin/access-control/me/capabilities", { headers: adminHeaders(actorUserId) });
    const perms = ((unwrap(caps.json) || {}).permissions || [])
        .map((p) => (typeof p === "string" ? p : prop(p, "permissionId")));
    record("fulfillment_manage_granted", perms.includes("fulfillment.manage"), `granted=${perms.length}`);

    // Real country id from the scratch geography reference (no invented scope).
    const countryId = psql("select country_id from geography.countries where country_code='IR' limit 1");
    record("scratch_country_reference_present", /^[0-9a-f-]{36}$/.test(countryId), `country=${countryId}`);

    // ---------- 1. Baseline: no policy for this identity => documented zero ----------
    const baseline = await listPolicies(actorUserId);
    const baselineView = unwrap(baseline.json) || {};
    record("baseline_read_ok", baseline.status === 200, `http=${baseline.status}`);
    record("baseline_empty_all_identities", (prop(baselineView, "packagingPolicies") || []).length === 0,
        `rows=${(prop(baselineView, "packagingPolicies") || []).length}`);
    record("baseline_db_rows_zero",
        psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}'`) === "0",
        "no persisted tipax packaging policy before the write");

    // ---------- 2. Create the method-owned policy (standard, 12000 IRR, Prepaid) ----------
    const created = await call("CREATE_STANDARD", policiesUrl(SERVICE), {
        method: "POST",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify(createBody(actorUserId, OPTION, 12000, countryId)),
    });
    const createdRow = unwrap(created.json) || {};
    const standardId = prop(createdRow, "packagingPolicyId");
    record("standard_policy_created",
        created.status === 200
        && prop(createdRow, "optionCode") === OPTION
        && Number(prop(createdRow, "feeAmount")) === 12000
        && prop(createdRow, "currencyCode") === "IRR"
        && prop(createdRow, "freightPayerMode") === "Prepaid"
        && prop(createdRow, "isActive") === true,
        `http=${created.status} option=${prop(createdRow, "optionCode")} fee=${prop(createdRow, "feeAmount")}`);

    // ---------- 3. Authoritative re-read: exactly this identity ----------
    const reread = await listPolicies(actorUserId);
    const rereadView = unwrap(reread.json) || {};
    const own = rowsFor(rereadView, SERVICE, OPTION);
    record("standard_reopened_from_server",
        reread.status === 200 && own.length === 1
        && Number(prop(own[0], "feeAmount")) === 12000
        && prop(own[0], "packagingPolicyId") === standardId,
        `rows=${own.length}`);
    record("sibling_not_borrowed_by_standard_policy",
        rowsFor(rereadView, SERVICE, SIBLING).length === 0
        && psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}' and option_code='${SIBLING}'`) === "0",
        "express remains undeclared");
    record("db_identity_exact_one_row",
        psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}' and option_code='${OPTION}'`) === "1",
        "one packaging policy row for the method");

    // ---------- 4. Sibling gets its own distinct policy ----------
    const siblingCreate = await call("CREATE_SIBLING", policiesUrl(SERVICE), {
        method: "POST",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify(createBody(actorUserId, SIBLING, 8000, countryId, "CollectOnDelivery")),
    });
    const siblingRow = unwrap(siblingCreate.json) || {};
    const siblingId = prop(siblingRow, "packagingPolicyId");
    record("sibling_policy_created",
        siblingCreate.status === 200
        && prop(siblingRow, "optionCode") === SIBLING
        && Number(prop(siblingRow, "feeAmount")) === 8000
        && prop(siblingRow, "freightPayerMode") === "CollectOnDelivery",
        `http=${siblingCreate.status} fee=${prop(siblingRow, "feeAmount")} payer=${prop(siblingRow, "freightPayerMode")}`);

    const afterSibling = unwrap((await listPolicies(actorUserId)).json) || {};
    record("standard_unchanged_by_sibling_write",
        rowsFor(afterSibling, SERVICE, OPTION).length === 1
        && Number(prop(rowsFor(afterSibling, SERVICE, OPTION)[0], "feeAmount")) === 12000,
        `standard=${prop(rowsFor(afterSibling, SERVICE, OPTION)[0] || {}, "feeAmount")}`);
    record("sibling_reads_its_own_policy",
        Number(prop(rowsFor(afterSibling, SERVICE, SIBLING)[0] || {}, "feeAmount")) === 8000,
        `express=${prop(rowsFor(afterSibling, SERVICE, SIBLING)[0] || {}, "feeAmount")}`);

    // ---------- 5. Legacy service-level row stays auditable and isolated ----------
    const legacyCreate = await call("CREATE_LEGACY", policiesUrl(SERVICE), {
        method: "POST",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify(createBody(actorUserId, null, 5000, countryId)),
    });
    record("legacy_service_level_row_created", legacyCreate.status === 200, `http=${legacyCreate.status}`);
    const legacyId = prop(unwrap(legacyCreate.json) || {}, "packagingPolicyId");

    // Update the method-owned policy (fee + payer mode), scope echoed unchanged.
    const updated = await call("UPDATE_STANDARD", rowUrl(SERVICE, standardId), {
        method: "PUT",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({
            serviceCode: SERVICE,
            optionCode: OPTION,
            scope: {
                countryId,
                administrativeAreaId: null,
                cityId: null,
                serviceAreaId: null,
            },
            feeAmount: 17500,
            currencyCode: "IRR",
            freightPayerMode: "CollectOnDelivery",
        }),
    });
    const updatedRow = unwrap(updated.json) || {};
    record("standard_update_persisted",
        updated.status === 200
        && Number(prop(updatedRow, "feeAmount")) === 17500
        && prop(updatedRow, "freightPayerMode") === "CollectOnDelivery"
        && prop(updatedRow, "packagingPolicyId") === standardId
        && prop(updatedRow, "optionCode") === OPTION,
        `http=${updated.status} fee=${prop(updatedRow, "feeAmount")} id-stable=${prop(updatedRow, "packagingPolicyId") === standardId}`);

    const afterUpdate = unwrap((await listPolicies(actorUserId)).json) || {};
    record("legacy_row_untouched_by_method_edit",
        Number(prop(rowsFor(afterUpdate, SERVICE, null)[0] || {}, "feeAmount")) === 5000
        && prop(rowsFor(afterUpdate, SERVICE, null)[0] || {}, "optionCode") == null,
        `legacy=${prop(rowsFor(afterUpdate, SERVICE, null)[0] || {}, "feeAmount")}`);
    record("sibling_untouched_by_method_edit",
        Number(prop(rowsFor(afterUpdate, SERVICE, SIBLING)[0] || {}, "feeAmount")) === 8000,
        `express=${prop(rowsFor(afterUpdate, SERVICE, SIBLING)[0] || {}, "feeAmount")}`);
    record("db_three_distinct_identities",
        psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}'`) === "3"
        && psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}' and option_code is null`) === "1",
        "service-level + standard + express, all separate rows");

    // ---------- 6. Activation touches only the addressed row ----------
    const deactivate = await call("DEACTIVATE_STANDARD", `${rowUrl(SERVICE, standardId)}/active`, {
        method: "PATCH",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({ isActive: false }),
    });
    const deactivatedRow = unwrap(deactivate.json) || {};
    record("standard_deactivated",
        deactivate.status === 200 && prop(deactivatedRow, "isActive") === false,
        `http=${deactivate.status}`);
    record("deactivation_preserves_row_identity",
        psql(`select count(*) from shipping.packaging_policies where packaging_policy_id='${standardId}'`) === "1"
        && psql(`select is_active from shipping.packaging_policies where packaging_policy_id='${standardId}'`) === "f",
        "row kept auditable, not deleted");

    const afterDeactivate = unwrap((await listPolicies(actorUserId)).json) || {};
    record("sibling_activation_untouched",
        prop(rowsFor(afterDeactivate, SERVICE, SIBLING)[0] || {}, "isActive") === true,
        `express-active=${prop(rowsFor(afterDeactivate, SERVICE, SIBLING)[0] || {}, "isActive")}`);

    const reactivate = await call("REACTIVATE_STANDARD", `${rowUrl(SERVICE, standardId)}/active`, {
        method: "PATCH",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({ isActive: true }),
    });
    record("standard_reactivated", reactivate.status === 200, `http=${reactivate.status}`);

    // ---------- 7. Fail-closed writes ----------
    const beforeFailures = psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}'`);

    const unknown = await call("CREATE_UNKNOWN_OPTION", policiesUrl(SERVICE), {
        method: "POST",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify(createBody(actorUserId, UNKNOWN_OPTION, 1000, countryId)),
    });
    record("unknown_option_rejected",
        unknown.status >= 400 && unknown.status < 500,
        `http=${unknown.status} code=${errorCode(unknown.json)}`);

    const mismatch = await call("CREATE_BODY_PATH_MISMATCH", policiesUrl(SERVICE), {
        method: "POST",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({
            serviceCode: OTHER_SERVICE,
            optionCode: OPTION,
            scope: { countryId, administrativeAreaId: null, cityId: null, serviceAreaId: null },
            feeAmount: 1000,
            currencyCode: "IRR",
            freightPayerMode: "Prepaid",
        }),
    });
    record("body_path_service_mismatch_rejected",
        mismatch.status >= 400 && mismatch.status < 500,
        `http=${mismatch.status} code=${errorCode(mismatch.json)}`);

    const crossService = await call("CROSS_SERVICE_PATCH", `${rowUrl(OTHER_SERVICE, standardId)}/active`, {
        method: "PATCH",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({ isActive: false }),
    });
    record("cross_service_active_toggle_rejected",
        crossService.status >= 400 && crossService.status < 500,
        `http=${crossService.status} code=${errorCode(crossService.json)}`);

    const crossServiceUpdate = await call("CROSS_SERVICE_UPDATE", rowUrl(OTHER_SERVICE, standardId), {
        method: "PUT",
        headers: adminHeaders(actorUserId),
        body: JSON.stringify({
            serviceCode: OTHER_SERVICE,
            optionCode: OPTION,
            scope: { countryId, administrativeAreaId: null, cityId: null, serviceAreaId: null },
            feeAmount: 1,
            currencyCode: "IRR",
            freightPayerMode: "Prepaid",
        }),
    });
    record("cross_service_update_rejected",
        crossServiceUpdate.status >= 400 && crossServiceUpdate.status < 500,
        `http=${crossServiceUpdate.status} code=${errorCode(crossServiceUpdate.json)}`);

    const anonymousGet = await fetch(`${BASE}${policiesUrl(SERVICE)}`, { headers: { Accept: "application/json" } });
    record("anonymous_read_rejected", anonymousGet.status === 401 || anonymousGet.status === 403, `http=${anonymousGet.status}`);

    const anonymousCreate = await fetch(`${BASE}${policiesUrl(SERVICE)}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(createBody("", OPTION, 1000, countryId)),
    });
    record("anonymous_write_rejected",
        anonymousCreate.status === 401 || anonymousCreate.status === 403,
        `http=${anonymousCreate.status}`);

    const afterFailures = psql(`select count(*) from shipping.packaging_policies where service_code='${SERVICE}'`);
    record("rejected_writes_changed_no_row", beforeFailures === afterFailures, `rows=${afterFailures}`);
    record("standard_fee_intact_after_rejections",
        psql(`select fee_amount from shipping.packaging_policies where packaging_policy_id='${standardId}'`) === "17500.00"
        || psql(`select fee_amount from shipping.packaging_policies where packaging_policy_id='${standardId}'`) === "17500",
        "cross-service update did not change the real row");

    // ---------- 8. Final authoritative read-back ----------
    const final = await listPolicies(actorUserId);
    const finalView = unwrap(final.json) || {};
    const finalOwn = rowsFor(finalView, SERVICE, OPTION);
    record("final_read_back_is_method_owned",
        final.status === 200
        && finalOwn.length === 1
        && Number(prop(finalOwn[0], "feeAmount")) === 17500
        && prop(finalOwn[0], "freightPayerMode") === "CollectOnDelivery"
        && prop(finalOwn[0], "isActive") === true
        && (prop(finalOwn[0], "optionCode") ?? null) === OPTION,
        `fee=${prop(finalOwn[0] || {}, "feeAmount")}`);
    record("final_legacy_and_sibling_still_distinct",
        Number(prop(rowsFor(finalView, SERVICE, null)[0] || {}, "feeAmount")) === 5000
        && Number(prop(rowsFor(finalView, SERVICE, SIBLING)[0] || {}, "feeAmount")) === 8000,
        "service-level and sibling identities unchanged");

    const passed = checks.filter((c) => c.ok).length;
    fs.writeFileSync(path.join(EVIDENCE, "assertion-matrix-176.json"), JSON.stringify({
        generatedAtUtc: new Date().toISOString(),
        host: BASE,
        task: "TB-SHIPPING-ADMIN-METHOD-PACKAGING-176",
        identity: { serviceCode: SERVICE, optionCode: OPTION, sibling: SIBLING, legacyOption: null },
        ids: { standard: standardId, sibling: siblingId, legacy: legacyId },
        total: checks.length,
        passed,
        failed: checks.length - passed,
        checks,
    }, null, 2) + "\n");
    fs.writeFileSync(path.join(EVIDENCE, "http-trace-176.txt"), trace.join("\n") + "\n");
    console.log(`TOTAL=${checks.length} PASSED=${passed} FAILED=${checks.length - passed}`);
    if (passed !== checks.length) process.exitCode = 1;
})().catch((err) => {
    console.error("HARNESS_ERROR", err && err.stack);
    fs.writeFileSync(path.join(EVIDENCE, "http-trace-176.txt"), trace.join("\n") + "\n");
    process.exitCode = 2;
});
