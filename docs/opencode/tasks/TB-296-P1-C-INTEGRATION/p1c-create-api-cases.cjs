// Real HTTP + independent PostgreSQL oracles for the Offer create boundary.
const assert=require("node:assert/strict");
const {randomUUID,createHash}=require("node:crypto");
const {execFileSync}=require("node:child_process");
const fs=require("node:fs");
const path=require("node:path");
const HOST="http://127.0.0.1:5088";
const uuid=x=>{assert.match(x,/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i);return x.toLowerCase();};
const quote=x=>"'"+String(x).replaceAll("'","''")+"'";
function deterministicId(seller,variant,request){
  const b=createHash("sha256").update(uuid(seller).replaceAll("-","")+"|"+uuid(variant).replaceAll("-","")+"|"+request).digest().subarray(0,16);
  const x=Buffer.from(b);x.subarray(0,4).reverse();x.subarray(4,6).reverse();x.subarray(6,8).reverse();
  const h=x.toString("hex");return h.slice(0,8)+"-"+h.slice(8,12)+"-"+h.slice(12,16)+"-"+h.slice(16,20)+"-"+h.slice(20);
}
function sql(query){
  return execFileSync(process.env.P1C_PSQL||"psql",["-X","-qAt","-v","ON_ERROR_STOP=1","-d","tooba_alpha","-c",query],{encoding:"utf8",timeout:30000,windowsHide:true}).trim();
}
function snapshot(seller,variant,ids){
  const tuple="seller_party_id="+quote(uuid(seller))+" AND catalog_variant_id="+quote(uuid(variant));
  const keys=ids.map(x=>quote(uuid(x))).join(",");
  const predicate="offer_id IN ("+keys+")";
  const outbox=ids.map(x=>"strpos(payload::text,"+quote('"'+uuid(x)+'"')+")>0").join(" OR ");
  const rows=(schema,table,where)=>"(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM "+schema+"."+table+" t WHERE "+where+")";
  const messages=(schema)=>"(SELECT coalesce(jsonb_agg(payload ORDER BY payload::text),\'[]\'::jsonb) FROM "+schema+".outbox_messages WHERE "+outbox+")";
  return JSON.parse(sql("SELECT jsonb_build_object('offers',"+rows("offer","offers",tuple+" OR "+predicate)+",'history',"+rows("offer","offer_history",predicate)+",'offerOutbox',"+messages("offer")+",'prices',"+rows("pricing","prices",predicate)+",'priceFacts',"+rows("pricing","offer_price_changes",predicate)+",'priceOutbox',"+messages("pricing")+",'positions',"+rows("inventory","stock_positions",predicate)+",'inventoryFacts',"+rows("inventory","offer_inventory_changes",predicate)+",'allocations',"+rows("inventory","offer_allocations",predicate)+",'inventoryOutbox',"+messages("inventory")+")"));
}
const noRows=x=>Object.values(x).every(a=>Array.isArray(a)&&a.length===0);
function preflight(){
  // Caller must verify exclusive DEV access, served Host/DB connection and current backup.
  const backup=process.env.P1C_BACKUP_FILE, expected=process.env.P1C_BACKUP_SHA256, identity=process.env.P1C_DB_IDENTITY;
  assert.ok(backup&&expected&&identity,"PRECONDITION: current external backup hash and verified served DEV PostgreSQL identity are required");
  const absolute=fs.realpathSync(backup);const repo=fs.realpathSync(path.resolve(__dirname,"../../../.."));
  const relative=path.relative(repo,absolute);
  assert.ok(relative.startsWith(".."+path.sep)||path.isAbsolute(relative),"Backup must be outside repository");
  assert.equal(createHash("sha256").update(fs.readFileSync(absolute)).digest("hex"),expected.toLowerCase(),"Backup digest mismatch");
  execFileSync(process.env.P1C_PG_RESTORE||"pg_restore",["--list",absolute],{stdio:"ignore",timeout:30000,windowsHide:true});
  const actual=sql("SELECT current_database()||'|'||coalesce(inet_server_addr()::text,'local')||'|'||coalesce(inet_server_port()::text,'local')");
  assert.equal(actual,identity,"SQL target does not equal independently verified served Host database identity");
  assert.ok(actual.startsWith("tooba_alpha|"),"Not authorized DEV database");
  return {database:actual,backupSha256:expected.toLowerCase()};
}
async function run(id,body){
  const root=process.env.P1C_EVIDENCE_DIR;
  assert.ok(root,"Set P1C_EVIDENCE_DIR to a new run-owned directory");
  fs.mkdirSync(root,{recursive:true});
  const evidence=path.join(root,id+".json");
  assert.ok(!fs.existsSync(evidence),"Do not overwrite historical evidence");
  const trace={id,requests:[],sql:[],status:"NOT_RUN"};
  const save=()=>fs.writeFileSync(evidence,JSON.stringify(trace,null,2));
  let fixture,cleanupNeeded=false;
  async function api(route,method="GET",payload,headers={}){
    const req={route,method,payload};trace.requests.push(req);save();
    const res=await fetch(HOST+route,{method,headers:{"Accept-Language":"fa-IR",...(payload?{"Content-Type":"application/json"}:{}),...headers},body:payload?JSON.stringify(payload):undefined,signal:AbortSignal.timeout(20000)});
    req.status=res.status;const raw=await res.text();try{req.body=raw?JSON.parse(raw):null}catch{req.body=raw}save();return req;
  }
  const get=async(route,headers)=>{const r=await api(route,"GET",undefined,headers);assert.equal(r.status,200,route);return r.body;};
  try{
    trace.preflight=preflight();save();
    const admin=await get("/v1/admin/dev-context",{});const actor=uuid(admin.actorUserId);
    const contexts=await get("/v1/seller/dev-contexts",{});
    const seller=uuid(process.env.P1C_SELLER_ID);
    assert.ok(Array.isArray(contexts.actors)&&contexts.actors.some(x=>x.sellerPartyId.toLowerCase()===seller),"Chosen seller not a current DEV synthetic identity");
    const headers={"X-Tooba-Dev-Actor-User-Id":actor};
    const channels=await get("/v1/admin/offers/allowed-channels",headers);
    assert.ok(Array.isArray(channels)&&channels.length>0&&channels.every(x=>typeof x==="string"),"No authorized channel");
    const channel=channels[0];
    const variant=uuid(process.env.P1C_CREATE_VARIANT_ID);
    // This explicit variant must have been verified as published in the current Catalog provider.
    const active=await get("/v1/admin/offers/active-listing?sellerPartyId="+seller+"&catalogVariantId="+variant+"&channel="+encodeURIComponent(channel),headers);
    assert.equal(active,null,"Active-listing read is not empty");
    const requestId="P1C-"+randomUUID(), otherRequest="P1C-"+randomUUID();
    const sku="P1C-"+randomUUID();
    const ids=[deterministicId(seller,variant,requestId),deterministicId(seller,variant,otherRequest)];
    fixture={seller,variant,channel,channels,ids,sku,changedSku:sku+"-C",headers,requestId,otherRequest};
    trace.fixture={seller,variant,channel,ids,sku,actor};save();
    const observe=label=>{const state=snapshot(seller,variant,ids);trace.sql.push({label,state});save();return state;};
    const before=observe("before");assert.ok(noRows(before),"Candidate variant/identities not isolated (including archived rows)");
    cleanupNeeded=true;
    const input={sellerPartyId:seller,catalogVariantId:variant,channel,sellerSku:sku,status:"Draft",requestId};
    const send=(p)=>api("/v1/admin/offers","POST",p,headers);
    const detail=(offerId)=>get("/v1/admin/offers/"+uuid(offerId),headers);
    const deny=(r,code)=>{assert.ok(r.status>=400&&r.status<500,"Expected typed 4xx denial");assert.equal(r.body?.errorCode,code,"Wrong semantic error");};
    const ok=(r)=>{assert.equal(r.status,201);assert.equal(r.body?.offerId,ids[0]);assert.equal(r.body?.sellerPartyId,seller);assert.equal(r.body?.catalogVariantId,variant);assert.equal(r.body?.channel,channel);assert.equal(r.body?.status,"Draft");};
    await body({fixture,input,send,detail,deny,ok,observe,before});
    trace.status="PASS";trace.reason="All executable assertions observed on actual HTTP and independent PostgreSQL";
  }catch(error){trace.status=cleanupNeeded?"FAIL":"NOT_RUN";trace.reason=String(error?.stack??error);}
  finally{
    if(cleanupNeeded){
      try{
        const {seller,variant,ids,sku,changedSku}=fixture;
        const actual=snapshot(seller,variant,ids);
        trace.beforeCleanup=actual;save();
        // Pure create cases must never make pricing/stock/reservation writes.
        assert.ok(["prices","priceFacts","priceOutbox","positions","inventoryFacts","allocations","inventoryOutbox"].every(k=>actual[k].length===0),"Unexpected owning-domain rows; do not perform unsafe cleanup");
        assert.ok(actual.offers.every(r=>ids.includes(r.offer_id)&&r.seller_party_id===seller&&r.catalog_variant_id===variant&&[sku,changedSku].includes(r.seller_sku)&&r.status==="Draft"),"Unexpected fixture identity; do not delete any row");
        const keys=ids.map(quote).join(",");
        const outbox=ids.map(x=>"strpos(payload::text,"+quote('"'+x+'"')+")>0").join(" OR ");
        // Transaction rechecks identity before scoped cleanup; no shared seeds and no broad purge.
        sql("BEGIN; LOCK TABLE offer.offers IN SHARE ROW EXCLUSIVE MODE; DO $$ BEGIN IF EXISTS(SELECT 1 FROM offer.offers WHERE offer_id IN ("+keys+") AND NOT(seller_party_id="+quote(seller)+" AND catalog_variant_id="+quote(variant)+" AND seller_sku IN ("+quote(sku)+","+quote(changedSku)+") AND status='Draft')) THEN RAISE EXCEPTION 'fixture identity changed'; END IF; END $$; DELETE FROM offer.offer_history WHERE offer_id IN ("+keys+"); DELETE FROM offer.outbox_messages WHERE "+outbox+"; DELETE FROM offer.offers WHERE offer_id IN ("+keys+"); COMMIT;");
        trace.afterCleanup=snapshot(seller,variant,ids);assert.ok(noRows(trace.afterCleanup),"Residual fixture data");
      }catch(error){trace.status="FAIL";trace.cleanupError=String(error?.stack??error);trace.reason=(trace.reason||"")+"; cleanup not certified";}
    }
    save();
  }
  return {status:trace.status,reason:trace.reason,evidence};
}
module.exports={
  "R10-01-N":()=>run("R10-01-N",async({fixture,input,send,deny,observe,before})=>{
    const unauthorized=["Direct","Marketplace","Agency","Corporate","Affiliate","Api"].find(x=>!fixture.channels.includes(x));
    assert.ok(unauthorized,"Need a real enumerated unauthorized channel");
    deny(await send({...input,channel:unauthorized}),"offer.channel.not_allowed");
    assert.deepEqual(observe("denied"),before);
  }),
  "R10-04-B":()=>run("R10-04-B",async({fixture,input,send,detail,deny,ok,observe})=>{
    ok(await send(input));
    const first=await detail(fixture.ids[0]);assert.equal(first.sellerSku,fixture.sku);assert.equal(first.channel,fixture.channel);
    const committed=observe("created");
    assert.equal(committed.offers.length,1);assert.equal(committed.offers[0].offer_id,fixture.ids[0]);
    assert.ok(committed.offers[0].creation_intent_fingerprint);
    assert.equal(committed.history.length,1);
    ok(await send(input));assert.deepEqual(await detail(fixture.ids[0]),first);assert.deepEqual(observe("exactReplay"),committed);
    deny(await send({...input,sellerSku:fixture.changedSku}),"offer.create.intent_conflict");assert.deepEqual(observe("changedIntent"),committed);
    deny(await send({...input,requestId:fixture.otherRequest}),"offer.listing.duplicate_active");assert.deepEqual(observe("differentIntent"),committed);
    assert.deepEqual(await detail(fixture.ids[0]),first);
  })
};
