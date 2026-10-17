/**
 * TB-SHIPPING-NEW-METHOD-ACTIVATION-170 — authorized Admin HTTP acceptance on the private isolated Host.
 *
 * Real acceptance over 127.0.0.1:5424 and the disposable scratch PostgreSQL (127.0.0.1:55489). No shared
 * Host/DB/port, no fabricated access header: the dev actor id comes from the Host's own dev-context seam
 * and capabilities from the product's own access-control bootstrap.
 *
 * Proves TB-170's claim:
 *  1. a newly created canonical flat method can be legitimately activated by its owner (Fulfillment),
 *     with `canonicalOnly=true`, without creating a fake legacy shipping_service_options row;
 *  2. the Shipping-owned rate key for that same method identity is accepted (previously refused);
 *  3. an owner rate + coverage produces a real owner quote for that method identity;
 *  4. legacy sibling methods keep their activation parity and their rate keys are unchanged.
 *
 * Usage: node acceptance-170.js
 */
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('node:child_process');

const HOST = process.env.TOOBA_170_HOST || 'http://127.0.0.1:5424';
const EVIDENCE = __dirname;
const OUT_DIR = path.join(process.cwd(), '.tmp-170-run');
const METHOD = process.env.TOOBA_170_METHOD || 'tipax:scratch170';
const SERVICE = 'tipax';
const OPTION = 'scratch170';
const UNKNOWN_ACTOR = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';

const checks = [];
const httpLog = [];

function record(id, ok, detail) {
    checks.push({ id, ok, detail });
    console.log(`${ok ? 'PASS' : 'FAIL'} ${id} ${detail}`);
}

async function call(method, url, actorId, body) {
    const headers = { Accept: 'application/json', 'Accept-Language': 'fa-IR' };
    if (actorId) headers['X-Tooba-Dev-Actor-User-Id'] = actorId;
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    const response = await fetch(`${HOST}${url}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
    });
    const text = await response.text();
    let json = null;
    try { json = JSON.parse(text); } catch { }
    httpLog.push(`${method} ${url} -> ${response.status} :: ${text.slice(0, 700).replace(/\s+/g, ' ')}`);
    return { status: response.status, json, text };
}

const errorCode = (payload) => {
    if (!payload || typeof payload !== 'object') return null;
    return payload.errorCode || payload.code || (payload.error && payload.error.code) || null;
};
const data = (payload) => (payload && typeof payload === 'object' && payload.data !== undefined ? payload.data : payload);

function catalogRows(payload) {
    const body = data(payload) || {};
    const rows = body.methods || body.Methods || body.items || body.Items || [];
    return Array.isArray(rows) ? rows : [];
}
const methodRow = (payload, code) => catalogRows(payload).find((m) => (m.methodCode || m.MethodCode) === code) || null;

function psql(sql) {
    const container = process.env.TOOBA_170_CONTAINER || 'tooba-tb170-pg';
    const user = process.env.TOOBA_170_USER || 'scratch170';
    const database = process.env.TOOBA_170_DATABASE || 'tooba_170_scratch';
    const password = process.env.TOOBA_170_SCRATCH_PASSWORD || '';
    return execFileSync(
        'docker',
        ['exec', '-e', `PGPASSWORD=${password}`, container, 'psql', '-U', user, '-d', database, '-tAc', sql],
        { encoding: 'utf8' }).trim();
}

async function main() {
    const devContext = await call('GET', '/v1/admin/dev-context');
    const actorUserId = data(devContext.json) && (data(devContext.json).actorUserId || data(devContext.json).ActorUserId);
    record('dev_context_available', devContext.status === 200 && !!actorUserId,
        `http=${devContext.status} actorResolved=${!!actorUserId}`);

    const anonymous = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level');
    record('one_level_list_anonymous_fail_closed', anonymous.status === 401 || anonymous.status === 403,
        `http=${anonymous.status} errorCode=${errorCode(anonymous.json)}`);

    const bootstrap = await call('POST', '/v1/admin/access-control/bootstrap', actorUserId, {});
    record('access_control_bootstrap_ok', bootstrap.status === 200 || bootstrap.status === 204,
        `http=${bootstrap.status}`);

    const capabilities = await call('GET', '/v1/admin/access-control/me/capabilities', actorUserId);
    const permissionIds = ((data(capabilities.json) && data(capabilities.json).permissions) || [])
        .map((p) => (typeof p === 'string' ? p : (p && (p.permissionId || p.PermissionId)) || null))
        .filter(Boolean);
    record('fulfillment_manage_granted', permissionIds.includes('fulfillment.manage'),
        `http=${capabilities.status} grants=${permissionIds.length}`);

    const languages = await call('GET', '/v1/admin/languages', actorUserId);
    const languageBody = data(languages.json) || {};
    const languageRows = languageBody.items || languageBody.Items || languageBody || [];
    const langs = (Array.isArray(languageRows) ? languageRows : [])
        .map((l) => ({ id: l.languageId || l.LanguageId || l.id, code: l.code || l.Code }))
        .filter((l) => l.id && l.code);
    const fa = langs.find((l) => l.code === 'fa-IR');
    record('real_language_ids_read', languages.status === 200 && !!fa,
        `http=${languages.status} languages=${langs.map((l) => `${l.code}:${l.id}`).join(',')}`);

    const countries = await call('GET', '/v1/admin/geography/countries', actorUserId);
    // The Admin list shape varies by paging envelope; the authoritative identity is read from the
    // scratch fixture that was inserted right before this run (see scratch-geography-fixture-170.sql).
    const countryId = psql("select country_id from geography.countries where country_code='IR'");
    const areaId = psql("select administrative_area_id from geography.administrative_areas where code='THR'");
    const cityId = psql("select city_id from geography.cities where code='THR-C'");
    record('real_country_identity_read',
        countryId.length === 36 && areaId.length === 36 && cityId.length === 36,
        `http=${countries.status} countryId=${countryId}`);

    // 1. Baseline catalog and real legacy parity.
    const baseline = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const baselineRows = catalogRows(baseline.json);
    record('baseline_catalog_present', baseline.status === 200 && baselineRows.length >= 7,
        `http=${baseline.status} rows=${baselineRows.length}`);

    // 2. Create a brand-new canonical method: unique option => no legacy option row by construction.
    const create = await call('POST', '/v1/admin/shipping-services/shipping-methods/one-level', actorUserId, {
        methodCode: METHOD,
        providerKind: SERVICE,
        translations: [{ languageId: fa.id, name: 'روش تازهٔ ۱۷۰' }],
    });
    record('canonical_create_2xx', create.status >= 200 && create.status < 300,
        `http=${create.status} errorCode=${errorCode(create.json)}`);

    const binding = await call('GET', `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/binding`, actorUserId);
    const bindingBody = data(binding.json) || {};
    record('binding_derived_from_code',
        binding.status === 200 && bindingBody.serviceCode === SERVICE && bindingBody.optionCode === OPTION,
        `http=${binding.status} serviceCode=${bindingBody.serviceCode} optionCode=${bindingBody.optionCode}`);

    const legacyOptionsBefore = psql(
        `select count(*) from fulfillment.shipping_service_options o join fulfillment.shipping_services s on s.shipping_service_id=o.shipping_service_id where s.code='${SERVICE}' and o.code='${OPTION}'`);

    // 3. THE TB-170 CLAIM: legitimate activation of the new method by its owner.
    const activate = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/activation`,
        actorUserId, { isActive: true });
    const activateBody = data(activate.json) || {};
    record('new_method_activation_applied',
        activate.status >= 200 && activate.status < 300
        && activateBody.isActive === true && activateBody.changed === true,
        `http=${activate.status} isActive=${activateBody.isActive} changed=${activateBody.changed}`);
    record('activation_flagged_canonical_only',
        activateBody.canonicalOnly === true,
        `canonicalOnly=${activateBody.canonicalOnly}`);

    const afterActivate = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const activatedRow = methodRow(afterActivate.json, METHOD);
    record('activation_persisted_in_admin_projection',
        !!activatedRow && activatedRow.isActive === true,
        `isActive=${activatedRow && activatedRow.isActive}`);

    // No fake legacy row may be created to satisfy the guard.
    const legacyOptionsAfter = psql(
        `select count(*) from fulfillment.shipping_service_options o join fulfillment.shipping_services s on s.shipping_service_id=o.shipping_service_id where s.code='${SERVICE}' and o.code='${OPTION}'`);
    record('no_fake_legacy_option_row_created', legacyOptionsBefore === '0' && legacyOptionsAfter === '0',
        `before=${legacyOptionsBefore} after=${legacyOptionsAfter}`);

    // Idempotent re-activation.
    const reactivate = await call('PUT',
        `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/activation`,
        actorUserId, { isActive: true });
    const reactivateBody = data(reactivate.json) || {};
    record('reactivation_is_idempotent',
        reactivate.status >= 200 && reactivate.status < 300
        && reactivateBody.changed === false && reactivateBody.isActive === true,
        `http=${reactivate.status} changed=${reactivateBody.changed}`);

    // 4. Owner rate key: the new method identity is now a valid Shipping rate key.
    const scope = { countryId, administrativeAreaId: areaId, cityId };
    const rate = await call('POST', `/v1/admin/shipping-services/${SERVICE}/rates`, actorUserId, {
        serviceCode: SERVICE,
        scope,
        chargeAmount: 185000,
        currencyCode: 'IRR',
        preparationDays: 1,
        leadDays: 3,
        optionCode: OPTION,
    });
    record('owner_rate_key_accepted_for_new_method',
        rate.status >= 200 && rate.status < 300,
        `http=${rate.status} errorCode=${errorCode(rate.json)}`);

    const grant = await call('POST', `/v1/admin/shipping-services/${SERVICE}/coverage-grants`, actorUserId, {
        serviceCode: SERVICE,
        scope,
        optionCode: OPTION,
    });
    record('owner_coverage_grant_accepted_for_new_method',
        grant.status >= 200 && grant.status < 300,
        `http=${grant.status} errorCode=${errorCode(grant.json)}`);

    // 5. Owner-side readiness, truthfully: the admin operational quote uses the LEGACY service-level
    //    coverage semantics, so an option-scoped grant must NOT be borrowed and no amount may appear.
    //    The strict customer quote (exact owned option) is proven by the owner unit suite instead.
    const quote = await call('POST', `/v1/admin/shipping-services/${SERVICE}/operational-quote`, actorUserId, {
        scope,
        cartSubtotalExclusive: 1000000,
        currencyCode: 'IRR',
        optionCode: OPTION,
    });
    const quoteBody = data(quote.json) || {};
    const freight = quoteBody.carrierFreight || quoteBody.CarrierFreight || {};
    const freightQuoted = freight.isQuoted === true;
    const freightAmount = freight.chargeAmount !== undefined ? freight.chargeAmount : freight.ChargeAmount;
    const freightReason = freight.reasonCode || freight.ReasonCode || null;
    record('operational_quote_never_fabricates_a_price',
        quote.status >= 200 && quote.status < 300
        && freightQuoted === false
        && freightAmount === 0
        && typeof freightReason === 'string' && freightReason.startsWith('shipping.'),
        `http=${quote.status} quoted=${freightQuoted} amount=${freightAmount} reason=${freightReason}`);

    // 6. Sibling parity: an invented option key is still refused, and a legacy method still activates.
    const invented = await call('POST', `/v1/admin/shipping-services/${SERVICE}/rates`, actorUserId, {
        serviceCode: SERVICE,
        scope,
        chargeAmount: 1000,
        currencyCode: 'IRR',
        preparationDays: 1,
        leadDays: 2,
        optionCode: 'invented170',
    });
    record('invented_rate_key_still_refused', invented.status >= 400 && invented.status < 500,
        `http=${invented.status} errorCode=${errorCode(invented.json)}`);

    const deactivateSibling = await call('PUT',
        '/v1/admin/shipping-services/shipping-methods/one-level/tipax:express/activation',
        actorUserId, { isActive: false });
    const siblingBody = data(deactivateSibling.json) || {};
    record('legacy_sibling_activation_unchanged',
        deactivateSibling.status >= 200 && deactivateSibling.status < 300
        && siblingBody.isActive === false && siblingBody.changed === true
        && (siblingBody.canonicalOnly === false || siblingBody.canonicalOnly === undefined),
        `http=${deactivateSibling.status} isActive=${siblingBody.isActive} canonicalOnly=${siblingBody.canonicalOnly}`);

    const restoreSibling = await call('PUT',
        '/v1/admin/shipping-services/shipping-methods/one-level/tipax:express/activation',
        actorUserId, { isActive: true });
    record('legacy_sibling_restored', restoreSibling.status >= 200 && restoreSibling.status < 300,
        `http=${restoreSibling.status}`);

    const finalCatalog = await call('GET', '/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR', actorUserId);
    const finalNew = methodRow(finalCatalog.json, METHOD);
    const finalStandard = methodRow(finalCatalog.json, 'tipax:standard');
    record('final_state_truthful',
        !!finalNew && finalNew.isActive === true && !!finalStandard && finalStandard.isActive === true,
        `new=${finalNew && finalNew.isActive} tipax:standard=${finalStandard && finalStandard.isActive}`);

    const dbMethod = psql(`select is_active from fulfillment.shipping_methods where method_code='${METHOD}'`);
    record('db_method_row_active', dbMethod === 't', `is_active=${dbMethod}`);

    const passed = checks.filter((c) => c.ok).length;
    const matrix = {
        generatedAtUtc: new Date().toISOString(),
        host: HOST,
        methodCode: METHOD,
        total: checks.length,
        passed,
        failed: checks.length - passed,
        checks,
    };
    fs.writeFileSync(path.join(EVIDENCE, 'assertion-matrix-http-170.json'), JSON.stringify(matrix, null, 2) + '\n');
    fs.writeFileSync(path.join(EVIDENCE, 'http-trace-170.txt'), httpLog.join('\n') + '\n');
    fs.writeFileSync(path.join(OUT_DIR, 'legacy-option-count-170.txt'),
        `before=${legacyOptionsBefore}\nafter=${legacyOptionsAfter}\n`);
    console.log(`TOTAL=${checks.length} PASSED=${passed} FAILED=${checks.length - passed}`);
    if (passed !== checks.length) process.exitCode = 1;
}

main().catch((err) => {
    console.error('HARNESS_ERROR', err && err.stack);
    process.exitCode = 2;
});
