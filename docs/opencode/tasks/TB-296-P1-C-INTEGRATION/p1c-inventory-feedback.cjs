// Real connected observation for R10-12-N; caller must provision an isolated, owned
// admin fixture and guarantee its exact cleanup in finally. No shared seed writes.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const { chromium } = require("playwright");
async function inventoryFeedback(fixture, evidencePath) {
  const {offerId, stockItemId, actorUserId, sellerPartyId, firstQuantity, retryQuantity} = fixture;
  for (const id of [offerId,stockItemId,actorUserId,sellerPartyId]) assert.match(id,/^[0-9a-f-]{36}$/i);
  assert.equal(fixture.ownedRunFixture,true,"Require caller-owned isolated fixture, not seed");
  assert.match(fixture.sellerSku,/^P1C-/,"Use unique run-owned fixture SKU");
  assert.equal(typeof fixture.readSql,"function","Independent SQL observation is mandatory");
  assert.notEqual(offerId,"457e375f-ceec-4b66-b81e-3829c4e15893");
  assert.notEqual(offerId,"01a0ba10-5a31-7000-b733-4cd74ef3763a");
  assert.ok(Number.isFinite(firstQuantity)&&Number.isFinite(retryQuantity)&&firstQuantity!==retryQuantity);
  const host="http://127.0.0.1:5088", front="http://127.0.0.1:3000";
  const endpoint="/v1/admin/offers/"+offerId;
  const trace={offerId,stockItemId,events:[],observations:[]};
  const saveTrace=()=>fs.writeFileSync(evidencePath,JSON.stringify(trace,null,2));
  async function get() {
    const response=await fetch(host+endpoint,{headers:{"X-Tooba-Dev-Actor-User-Id":actorUserId,"Accept-Language":"fa-IR"}});
    assert.equal(response.status,200);
    const detail=await response.json();
    assert.equal(detail.offerId,offerId);assert.equal(detail.sellerPartyId,sellerPartyId);
    assert.equal(detail.sellerSku,fixture.sellerSku);assert.equal(detail.status,"Draft");
    const row=detail.inventoryLocations.find(x=>x.stockItemId===stockItemId);
    assert.ok(row,"Exact target stock row is absent");
    assert.ok(row.revision,"Missing owner revision");
    return {detail,row};
  }
  async function sql() {
    const row=await fixture.readSql({offerId,stockItemId});
    assert.equal(row.offerId,offerId);assert.equal(row.stockItemId,stockItemId);
    assert.ok(Number.isFinite(row.onHand)&&Number.isFinite(row.reserved)&&Number.isInteger(row.factCount));
    trace.observations.push({independentSql:row});saveTrace();return row;
  }
  let browser, page;
  const writes=()=>trace.events.filter(x=>x.type==="request");
  try {
    const before=await get();
    trace.before=before.row;
    const beforeSql=await sql();
    assert.equal(beforeSql.onHand,before.row.onHand);assert.equal(beforeSql.reserved,before.row.reserved);
    assert.equal(before.detail.capabilities?.canEditPhysicalInventory,true);
    assert.ok(firstQuantity>=before.row.reserved&&retryQuantity>=before.row.reserved&&firstQuantity!==before.row.onHand);
    browser=await chromium.launch();
    const context=await browser.newContext({locale:"fa-IR",viewport:{width:1440,height:1000}});
    await context.addInitScript(actor=>localStorage.setItem("tooba.adminActorUserId",actor),actorUserId);
    page=await context.newPage();
    page.on("request",r=>{if(r.method()==="PUT"&&r.url().endsWith(endpoint+"/inventory"))trace.events.push({type:"request",body:r.postDataJSON()});});
    page.on("response",async r=>{if(r.request().method()==="PUT"&&r.url().endsWith(endpoint+"/inventory")){let body;try{body=await r.json()}catch{body=null}trace.events.push({type:"response",status:r.status(),body});}});
    page.on("requestfailed",r=>{if(r.method()==="PUT"&&r.url().endsWith(endpoint+"/inventory"))trace.events.push({type:"requestfailed",error:r.failure()?.errorText});});
    await page.goto(front+"/fa/admin/offers/"+offerId);
    await page.getByTestId("offer-tab-button-inventory").click();
    const field=page.locator('[data-stock-item-id="'+stockItemId+'"]').getByTestId("offer-inventory-input");
    const reason=page.getByTestId("offer-inventory-reason");
    const save=page.getByTestId("admin-offer-save");
    const dialog=page.locator("dialog[open]");
    const toast=page.locator(".Toastify__toast--success");
    async function visibleOutcome() {
      const choices=[["success",toast],["dialog",dialog],["inline",page.getByTestId("offer-save-error")]];
      return Promise.any(choices.map(async ([kind,locator])=>{await locator.first().waitFor({state:"visible",timeout:12000});return {kind,text:await locator.first().innerText()};}));
    }
    async function successfulSave(expected, expectedRevision, previousSql) {
      await toast.first().waitFor({state:"hidden",timeout:6000});
      assert.equal(await dialog.count(),0,"Unexpected blocking dialog before write");
      const n=writes().length;
      // Do not race a stale inline error left by the preceding validation; the save action clears it.
      const outcome=Promise.any([["success",toast],["dialog",dialog]].map(async ([kind,locator])=>{await locator.first().waitFor({state:"visible",timeout:12000});return {kind,text:await locator.first().innerText()};}));
      outcome.catch(()=>{});
      const response=page.waitForResponse(r=>r.request().method()==="PUT"&&r.url().endsWith(endpoint+"/inventory"),{timeout:12000});
      response.catch(()=>{});
      await save.click();
      const observed=await outcome;
      trace.observations.push(observed);
      const after=await get();trace.observations.push({independentRead:after.row});saveTrace();
      assert.equal(observed.kind,"success",observed.text);
      assert.equal((await response).status(),200);
      assert.equal(writes().length,n+1);
      const body=writes()[n].body;
      assert.equal(body.stockItemId,stockItemId);assert.equal(body.onHand,expected);assert.equal(body.expectedVersion,expectedRevision);assert.ok(body.operationId);
      assert.equal(after.row.onHand,expected);assert.notEqual(after.row.revision,expectedRevision);
      const persisted=await sql();
      assert.equal(persisted.onHand,expected);assert.equal(persisted.reserved,previousSql.reserved);assert.equal(persisted.factCount,previousSql.factCount+1);
      return {...after,persisted};
    }
    await field.fill(String(firstQuantity));await reason.fill("");
    const startWrites=writes().length;
    await save.click();
    await page.getByTestId("offer-save-error").waitFor({state:"visible"});
    const inline=await page.getByTestId("offer-save-error").innerText();
    trace.observations.push({inline});
    assert.match(inline,/دلیل/);
    assert.equal(writes().length,startWrites);
    const afterInvalid=await get();
    assert.equal(afterInvalid.row.onHand,before.row.onHand);
    assert.equal(afterInvalid.row.revision,before.row.revision);
    assert.deepEqual(await sql(),beforeSql);
    await reason.fill("P1C verified correction");
    const first=await successfulSave(firstQuantity,before.row.revision,beforeSql);
    await toast.first().waitFor({state:"hidden",timeout:6000});
    await field.fill(String(retryQuantity));await reason.fill("P1C controlled failure");
    const route="**"+endpoint+"/inventory";
    let aborted=0;
    await page.route(route,async r=>{aborted++;await r.abort("failed");});
    try {
      const outcome=visibleOutcome();outcome.catch(()=>{});
      await save.click();
      const observed=await outcome;trace.observations.push(observed);saveTrace();
      assert.equal(observed.kind,"dialog",observed.text);
      assert.equal(aborted,1);
      const failed=await get();trace.observations.push({afterFailure:failed.row});
      assert.equal(failed.row.onHand,first.row.onHand);assert.equal(failed.row.revision,first.row.revision);
      assert.deepEqual(await sql(),first.persisted);
    } finally {await page.unroute(route);}
    await dialog.locator('button[data-testid="offer-message-reload"]').click();
    await dialog.waitFor({state:"hidden"});
    await save.waitFor({state:"visible"});
    assert.equal(await field.inputValue(),String(retryQuantity));
    assert.equal(await reason.inputValue(),"P1C controlled failure");
    // Reload gives an info Toast; await actual enabled button, not a blind repeated click.
    await page.waitForFunction(()=>!document.querySelector('[data-testid="admin-offer-save"]')?.disabled);
    await successfulSave(retryQuantity,first.row.revision,first.persisted);
    trace.status="PASS";saveTrace();
    return {status:"PASS",reason:"Blank reason rejected without write; correction persisted; controlled failure showed modal and preserved state; reload/retry persisted exact owner revision.",evidence:evidencePath};
  } catch(e) {
    trace.status="FAIL";trace.error=String(e?.stack??e);
    if(page) {trace.dialog=await page.locator("dialog[open]").allTextContents().catch(()=>[]);trace.inline=await page.getByTestId("offer-save-error").allTextContents().catch(()=>[]);}
    try {trace.final=await get()}catch(e){trace.readError=String(e);}
    saveTrace();
    return {status:"FAIL",reason:trace.error,evidence:evidencePath};
  } finally {if(browser)await browser.close();}
}
module.exports={inventoryFeedback};
