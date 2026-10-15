/**
 * TB-SHIPPING-ONE-LEVEL-TABS-099 — real authorized activation write + read-back + reversal proof.
 *
 * Runs against the TASK-OWNED isolated Host (http://127.0.0.1:5099) and the task-owned scratch
 * database ONLY. It never addresses shared :5088/:3000 and never touches tooba_alpha.
 *
 * Evidence is captured as text lines: HTTP method, status, response body, and the resulting
 * database truth read back from the scratch database through the container.
 */
const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const HOST = 'http://127.0.0.1:5099';
const CONTAINER = 'tooba-099-scratch-pg';
const DB = 'tooba_099_scratch';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-099');
const TARGET = 'scratchsvc:alpha';
const SIBLING = 'scratchsvc:beta';

const lines = [];
function log(line) {
    lines.push(line);
    console.log(line);
}

function psql(sql) {
    const out = cp.execSync(
        `docker exec ${CONTAINER} psql -U scratch099 -d ${DB} -t -A -c "${sql.replace(/"/g, '\\"')}"`,
        { encoding: 'utf8' });
    return out.trim();
}

function dbTruth() {
    return {
        option_alpha_active: psql("select is_active from fulfillment.shipping_service_options where code='alpha'"),
        option_alpha_id: psql("select shipping_service_option_id from fulfillment.shipping_service_options where code='alpha'"),
        option_alpha_updated: psql("select updated_at from fulfillment.shipping_service_options where code='alpha'"),
        option_beta_active: psql("select is_active from fulfillment.shipping_service_options where code='beta'"),
        option_beta_id: psql("select shipping_service_option_id from fulfillment.shipping_service_options where code='beta'"),
        option_beta_updated: psql("select updated_at from fulfillment.shipping_service_options where code='beta'"),
        method_alpha_active: psql("select is_active from fulfillment.shipping_methods where method_code='scratchsvc:alpha'"),
        method_alpha_id: psql("select shipping_method_id from fulfillment.shipping_methods where method_code='scratchsvc:alpha'"),
        method_beta_active: psql("select is_active from fulfillment.shipping_methods where method_code='scratchsvc:beta'"),
        method_beta_id: psql("select shipping_method_id from fulfillment.shipping_methods where method_code='scratchsvc:beta'"),
        service_active: psql("select is_active from fulfillment.shipping_services where code='scratchsvc'"),
        service_id: psql("select shipping_service_id from fulfillment.shipping_services where code='scratchsvc'"),
        optionless_method_active: psql("select is_active from fulfillment.shipping_methods where method_code='scratchsvc' and legacy_option_code is null"),
        off_parent_active: psql("select is_active from fulfillment.shipping_services where code='scratchoff'"),
        off_child_active: psql("select is_active from fulfillment.shipping_methods where method_code='scratchoff:child'"),
    };
}

async function put(methodCode, isActive, token, extraHeaders) {
    const url = `${HOST}/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(methodCode)}/activation`;
    const res = await fetch(url, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
            ...(extraHeaders || {}),
        },
        body: JSON.stringify({ isActive }),
    });
    const text = await res.text();
    return { status: res.status, text };
}

async function main() {
    const token = fs.readFileSync('.tmp-099-session.txt', 'utf8').trim();

    const before = dbTruth();
    log('===== BASELINE (scratch DB, read-only) =====');
    log(JSON.stringify(before, null, 1));

    log('');
    log('===== AUTH FAIL-CLOSED ON THIS ISOLATED HOST =====');
    const anon = await put(TARGET, false, null);
    log(`PUT ${TARGET} isActive=false ANONYMOUS => ${anon.status} ${anon.text.slice(0, 160)}`);
    const guest = await put(TARGET, false, 'unknown-session-token-099');
    log(`PUT ${TARGET} isActive=false UNKNOWN BEARER => ${guest.status} ${guest.text.slice(0, 160)}`);
    const guestActor = await put(TARGET, false, null, { 'X-Tooba-Dev-Actor-User-Id': 'aaaaaaaa-aaaa-4aaa-8aaa-000000000009' });
    log(`PUT ${TARGET} isActive=false GUEST DEV-ACTOR (Development-only seam) => ${guestActor.status} ${guestActor.text.slice(0, 160)}`);
    const adminActorHeader = await put('scratchsvc:beta', true, null, { 'X-Tooba-Dev-Actor-User-Id': (await fetch(`${HOST}/v1/admin/dev-context`).then((r) => r.json())).actorUserId });
    log(`GET dev-context actor id then PUT ${SIBLING} isActive=true (already true, no-op) => ${adminActorHeader.status} ${adminActorHeader.text.slice(0, 200)}`);

    log('');
    log('===== 1. REAL AUTHORIZED WRITE (deactivate active option-backed method) =====');
    const w = await put(TARGET, false, token);
    log(`PUT ${TARGET} isActive=false AUTHENTICATED => ${w.status} ${w.text}`);
    const afterWrite = dbTruth();
    log('DB TRUTH AFTER WRITE: ' + JSON.stringify(afterWrite, null, 1));

    log('');
    log('===== 2. REVERSAL THROUGH THE SAME AUTHORIZED API =====');
    const rv = await put(TARGET, true, token);
    log(`PUT ${TARGET} isActive=true AUTHENTICATED => ${rv.status} ${rv.text}`);
    const afterRevert = dbTruth();
    log('DB TRUTH AFTER REVERT: ' + JSON.stringify(afterRevert, null, 1));

    log('');
    log('===== 3. REPEATED SAME-STATE REQUEST (idempotent, must not mutate again) =====');
    const rep = await put(TARGET, true, token);
    log(`PUT ${TARGET} isActive=true AGAIN => ${rep.status} ${rep.text}`);
    const afterRepeat = dbTruth();
    log('DB TRUTH AFTER REPEAT: ' + JSON.stringify(afterRepeat, null, 1));

    log('');
    log('===== 4. OPTIONLESS REFUSAL (never auto-activated) =====');
    const opt = await put('scratchsvc', true, token);
    log(`PUT scratchsvc isActive=true (optionless) => ${opt.status} ${opt.text}`);

    log('');
    log('===== 5. INACTIVE-PARENT REFUSAL (never auto-activated) =====');
    const par = await put('scratchoff:child', true, token);
    log(`PUT scratchoff:child isActive=true (inactive parent) => ${par.status} ${par.text}`);

    const afterRefusals = dbTruth();
    log('DB TRUTH AFTER REFUSALS: ' + JSON.stringify(afterRefusals, null, 1));

    log('');
    log('===== VERDICT CHECKS =====');
    const checks = [
        ['write deactivated legacy option', before.option_alpha_active === 't' && afterWrite.option_alpha_active === 'f'],
        ['write deactivated one-level projection', before.method_alpha_active === 't' && afterWrite.method_alpha_active === 'f'],
        ['sibling option unchanged', afterWrite.option_beta_active === before.option_beta_active && afterWrite.option_beta_id === before.option_beta_id],
        ['sibling method unchanged', afterWrite.method_beta_active === before.method_beta_active && afterWrite.method_beta_id === before.method_beta_id],
        ['option identity preserved', afterWrite.option_alpha_id === before.option_alpha_id],
        ['parent untouched', afterWrite.service_active === before.service_active && afterWrite.service_id === before.service_id],
        ['revert restored baseline activation', afterRevert.option_alpha_active === before.option_alpha_active && afterRevert.method_alpha_active === before.method_alpha_active],
        ['repeat was idempotent (no further mutation)', afterRepeat.option_alpha_updated === afterRevert.option_alpha_updated && afterRepeat.method_alpha_active === 't'],
        ['optionless refused (still inactive)', afterRefusals.optionless_method_active === 'f'],
        ['inactive parent refused (child still inactive)', afterRefusals.off_child_active === 'f' && afterRefusals.off_parent_active === 'f'],
        ['anonymous write denied', anon.status === 401],
        ['unknown bearer denied', guest.status === 401],
        ['guest dev actor denied', guestActor.status === 403],
    ];
    let allPass = true;
    for (const [name, ok] of checks) {
        if (!ok) allPass = false;
        log(`${ok ? 'PASS' : 'FAIL'} :: ${name}`);
    }
    log(`OVERALL=${allPass ? 'PASS' : 'FAIL'}`);

    fs.writeFileSync(path.join(OUT, 'activation-write-proof.txt'), lines.join('\n') + '\n');
}

main().catch((err) => { console.error('ERR', err); process.exit(1); });
