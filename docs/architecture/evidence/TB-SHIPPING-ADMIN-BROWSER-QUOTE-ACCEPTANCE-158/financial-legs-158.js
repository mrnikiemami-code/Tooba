/**
 * TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158 — server-side legs that accompany the real browser flow.
 *
 * These are the SAME canonical HTTP seams TB-157 proved. They exist here to (a) place a provider-level
 * legacy row under the method whose owned rule was created through the real Admin UI, so that
 * "legacy is never this method's money" is re-proved against the browser-created rule, and (b) capture the
 * authoritative read-back used to interpret the customer quote leg. No credentials/tokens/cookies are written.
 */
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const HOST = process.env.TOOBA_158_HOST || 'http://127.0.0.1:5418';
const OUT = path.join(process.cwd(), 'docs', 'architecture', 'evidence', 'TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158');

async function call(method, url, actorUserId, body) {
    const headers = { Accept: 'application/json' };
    if (actorUserId) headers['X-Tooba-Dev-Actor-User-Id'] = actorUserId;
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    const res = await fetch(HOST + url, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await res.text();
    let json = null;
    try { json = JSON.parse(text); } catch { }
    return { status: res.status, json, text };
}

const rows = [];
function record(name, ok, detail) { rows.push({ name, ok: !!ok, detail }); }

(async () => {
    const dev = await call('GET', '/v1/admin/dev-context');
    const actorUserId = dev.json && dev.json.actorUserId;
    record('authorized_dev_admin_resolved', !!actorUserId, `http=${dev.status} actor=${actorUserId ? 'present' : 'missing'}`);

    const scope = {
        countryId: '11111111-1111-1111-1111-111111111111',
        administrativeAreaId: null,
        cityId: null,
        serviceAreaId: null,
    };

    const before = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    fs.writeFileSync(path.join(OUT, 'financial-status-standard-after-browser-create-158.json'), JSON.stringify(before.json, null, 2));

    // Provider-level legacy row on the same service as the browser-created owned rule.
    const legacyWrite = await call('POST', '/v1/admin/shipping-services/tipax/rates', actorUserId, {
        serviceCode: 'tipax', scope, chargeAmount: 99000, currencyCode: 'IRR',
        preparationDays: 1, leadDays: 3, freeAboveThreshold: null, optionCode: null, condition: null,
    });
    fs.writeFileSync(path.join(OUT, 'legacy-service-level-write-158.json'), JSON.stringify(legacyWrite.json, null, 2));
    record('legacy_provider_level_write_accepted', legacyWrite.status === 200, `http=${legacyWrite.status}`);

    const afterLegacy = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    fs.writeFileSync(path.join(OUT, 'financial-status-standard-after-legacy-158.json'), JSON.stringify(afterLegacy.json, null, 2));
    const s = afterLegacy.json && (afterLegacy.json.data || afterLegacy.json);
    const rule = s && (s.rules || [])[0];
    const amount = rule && (rule.chargeAmount !== undefined ? rule.chargeAmount : rule.ChargeAmount);
    record('legacy_service_level_row_never_becomes_method_money',
        afterLegacy.status === 200 && s.state === 'FinanciallyConfigured' && Number(amount) === 185000
        && Number(s.activeServiceLevelRuleCount) === 1,
        `http=${afterLegacy.status} state=${s && s.state} amount=${amount} serviceLevelRules=${s && s.activeServiceLevelRuleCount}`);

    const sibling = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=express', actorUserId);
    fs.writeFileSync(path.join(OUT, 'financial-status-sibling-express-after-legacy-158.json'), JSON.stringify(sibling.json, null, 2));
    const sib = sibling.json && (sibling.json.data || sibling.json);
    record('sibling_option_did_not_inherit_owned_or_legacy_money',
        sibling.status === 200 && sib.state === 'NeedsConfiguration' && Number(sib.ruleCount) === 0,
        `http=${sibling.status} state=${sib && sib.state} ruleCount=${sib && sib.ruleCount} siblingOptionRules=${sib && sib.activeSiblingOptionRuleCount}`);

    // A second write for the same method at the same scope must not silently duplicate the owned rule.
    const duplicate = await call('POST', '/v1/admin/shipping-services/tipax/rates', actorUserId, {
        serviceCode: 'tipax', scope, chargeAmount: 200000, currencyCode: 'IRR',
        preparationDays: 1, leadDays: 3, freeAboveThreshold: null, optionCode: 'standard', condition: null,
    });
    fs.writeFileSync(path.join(OUT, 'duplicate-owned-write-158.json'), JSON.stringify(duplicate.json, null, 2));
    const afterDuplicate = await call('GET', '/v1/admin/shipping-services/tipax/financial-status?optionCode=standard', actorUserId);
    fs.writeFileSync(path.join(OUT, 'financial-status-standard-after-duplicate-158.json'), JSON.stringify(afterDuplicate.json, null, 2));
    const dupState = afterDuplicate.json && (afterDuplicate.json.data || afterDuplicate.json);
    record('same_scope_second_owned_write_stays_unambiguous',
        duplicate.status === 200
        ? Number(dupState.ruleCount) === 1
        : /duplicate|conflict/i.test(JSON.stringify(duplicate.json || {})),
        `http=${duplicate.status} ruleCount=${dupState && dupState.ruleCount} body=${JSON.stringify(duplicate.json).slice(0, 200)}`);

    fs.writeFileSync(path.join(OUT, 'financial-legs-158.json'), JSON.stringify(rows, null, 2));
    for (const r of rows) console.log(`${r.ok ? 'PASS' : 'FAIL'} ${r.name} :: ${r.detail}`);
})().catch((e) => { console.error('LEG_ERROR', e.message); process.exit(1); });
