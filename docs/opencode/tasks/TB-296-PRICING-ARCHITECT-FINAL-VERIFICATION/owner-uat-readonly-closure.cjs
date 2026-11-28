"use strict";
// Read-only connected duplicate and warehouse UI witness. Never create or modify an offer.
const fs=require("node:fs"),path=require("node:path"),{execFileSync}=require("node:child_process"),{randomUUID}=require("node:crypto");
const H="http://127.0.0.1:5088",F="http://127.0.0.1:3000";
const PSQL=process.env.P1C_PSQL||path.resolve(__dirname,"../TB-296-P1-C-INTEGRATION/p1c-psql.exe");
const PW=process.env.P1C_PLAYWRIGHT||[path.resolve(__dirname,"../../../../src/frontend/node_modules/playwright"),"C:/Users/User/OpenCode-UI-Lab-20261122-064208/node_modules/playwright"].find(fs.existsSync);
const out={runId:randomUUID(),cases:{},writeAttempts:0,status:"NOT_RUN"};
const dir=path.join(__dirname,"owner-uat-closure-evidence",out.runId);fs.mkdirSync(dir,{recursive:true});
const save=()=>fs.writeFileSync(path.join(dir,"readonly-closure.json"),JSON.stringify(out,null,2));
const check=(x,m)=>{if(!x)throw Error(m)};
const quote=x=>"'"+String(x).replaceAll("'","''")+"'";
const sql=s=>execFileSync(PSQL,["-X","-qAt","-v","ON_ERROR_STOP=1","-d","tooba_alpha","-c","BEGIN READ ONLY; "+s+"; COMMIT;"],{encoding:"utf8",timeout:30000,windowsHide:true}).trim();
const t=(p,id)=>p.getByTestId(id);
async function get(route,headers={},language="fa-IR"){
 const r=await fetch(H+route,{headers:{...headers,"Accept-Language":language},signal:AbortSignal.timeout(15000)});
 return {status:r.status,body:await r.json().catch(()=>null)};
}
const headers=(actor,seller)=>({"X-Tooba-Dev-Actor-User-Id":actor,...(seller?{"X-Tooba-Seller-Party-Id":seller}:{})});
async function stage(name,fn){try{out.cases[name]={status:"PASS",...await fn()}}catch(e){out.cases[name]={status:"FAIL",error:String(e.message).slice(0,900)}}save()}
async function findExisting(role,actor,seller,language){
 const h=headers(actor,seller.sellerPartyId),base=role==="admin"?"/v1/admin/catalog/products":"/v1/seller/catalog-products";
 const ids=JSON.parse(sql("select coalesce(jsonb_agg(offer_id),'[]') from (select o.offer_id from offer.offers o join catalog.variants v on v.variant_id=o.catalog_variant_id join catalog.products p on p.product_id=v.product_id where o.seller_party_id="+quote(seller.sellerPartyId)+" and o.channel='Marketplace' and o.status<>'Archived' and v.status='Published' and p.status='Published' order by o.offer_id limit 40) s"));
 for(const id of ids){
  const d=await get("/v1/"+role+"/offers/"+id,h,language);
  if(d.status!==200||!d.body?.productTitle||!d.body?.variantCode||d.body.sellerPartyId!==seller.sellerPartyId)continue;
  const x=d.body, search=await get(base+"?"+new URLSearchParams({search:x.productTitle,page:"1",pageSize:"10",locale:language}),h,language);
  if(search.status!==200||search.body?.items?.filter(y=>y.title===x.productTitle).length!==1||!search.body.items.some(y=>y.productId===x.productId))continue;
  const v=await get(base+"/"+x.productId+"/variants?locale="+encodeURIComponent(language),h,language);
  if(v.status!==200||!Array.isArray(v.body)||!v.body.some(y=>y.catalogVariantId===x.catalogVariantId&&y.catalogCode===x.variantCode))continue;
  return x;
 }
 throw Error("No exact visible existing tuple found for "+role);
}
async function selectIdentity(p,role,seller,d,locale){
 const base=role==="admin"?"/admin/offers/":"/vendor-panel/products/";
 await p.goto(F+"/"+locale+base+"new");await t(p,"offer-workspace").waitFor();
 if(role==="admin"){
  await t(p,"offer-seller-picker-toggle").click();const s=t(p,"offer-seller-dialog-search");await s.fill(seller.sellerLabel);await s.press("Enter");
  const o=t(p,"offer-seller-dialog-option").filter({hasText:seller.sellerLabel});await o.first().waitFor();check(await o.count()===1,"seller ambiguous");await o.click();
 }
 await t(p,"offer-channel-select").selectOption(d.channel);
 await t(p,"offer-product-picker-toggle").click();const s=t(p,"offer-product-dialog-search");
 await s.fill(d.productTitle);
 const apiPath=role==="admin"?"/v1/admin/catalog/products":"/v1/seller/catalog-products";
 const search=p.waitForResponse(r=>new URL(r.url()).pathname===apiPath&&new URL(r.url()).searchParams.get("search")===d.productTitle);
 await s.press("Enter");check((await search).ok(),"search failed");
 const o=t(p,"offer-product-dialog-option").filter({has:p.getByText(d.productTitle,{exact:true})});await o.first().waitFor();check(await o.count()===1,"product ambiguous");await o.click();
 // A sole variant is auto-selected by the shared picker; otherwise choose the exact code.
 const duplicate=t(p,"offer-message-duplicate");
 if(!await duplicate.isVisible()){
  const toggle=t(p,"offer-variant-picker-toggle");await toggle.click();
  const opt=t(p,"offer-variant-option").filter({has:p.getByText(d.variantCode,{exact:true})});
  await opt.first().waitFor();check(await opt.count()===1,"variant ambiguous");await opt.click();
 }
 return base;
}
async function main(){
 let browser;
 try{
  out.head=execFileSync("git",["rev-parse","HEAD"],{encoding:"utf8"}).trim();
  out.database=sql("select current_database()");check(out.database==="tooba_alpha","unexpected DB");
  const admin=await get("/v1/admin/dev-context"),seller=await get("/v1/seller/dev-contexts");
  check(admin.status===200&&Array.isArray(seller.body?.actors),"real context unavailable");
  const actors=seller.body.actors;const authorized=[];
  for(const a of actors){const caps=await get("/v1/seller/access-control/me/capabilities",headers(a.actorUserId,a.sellerPartyId));if(caps.status===200&&caps.body?.permissions?.some(g=>g.permissionId==="product.create"&&g.scopeKind===1&&!g.deniedByCeiling))authorized.push(a)}
  check(PW,"Playwright missing");browser=await require(PW).chromium.launch({timeout:20000});
  for(const [role,locale,language] of [["admin","en","en-US"],["seller","fa","fa-IR"]]){
   await stage("duplicate-"+role,async()=>{
    const a=role==="admin"?actors[0]:authorized[0];check(a,"no authorized actor");
    const actor=role==="admin"?admin.body.actorUserId:a.actorUserId;
    const d=await findExisting(role,actor,a,language);
    const ctx=await browser.newContext({locale:language});try{
     await ctx.addInitScript(x=>{localStorage.setItem("tooba.adminActorUserId",x.admin);localStorage.setItem("tooba.sellerPartyId",x.seller);localStorage.setItem("tooba.sellerActorUserId",x.actor)}, {admin:admin.body.actorUserId,seller:a.sellerPartyId,actor:a.actorUserId});
     await ctx.route(/\/v1\/(?:admin|seller)\/offers(?:\/|$|\?)/,async route=>{const req=route.request();if(["PUT","PATCH","DELETE","POST"].includes(req.method())&&!new URL(req.url()).pathname.endsWith("/query")){out.writeAttempts++;await route.abort();return}await route.continue()});
     const p=await ctx.newPage();p.setDefaultTimeout(18000);
     const base=await selectIdentity(p,role,a,d,locale);
     await t(p,"offer-message-duplicate").waitFor();
     const pre=p.url();await t(p,"offer-message-dismiss").click();
     check(p.url()===pre&&!await t(p,"offer-message-duplicate").isVisible(),"dismiss unexpectedly navigated");
     const submit=t(p,role==="admin"?"admin-offer-submit":"seller-create-submit").first();
     check(await submit.isEnabled(),"existing action disabled");
     await submit.click();await t(p,"offer-message-duplicate").waitFor();
     const expected="/"+locale+base+d.offerId;
     await Promise.all([p.waitForURL(u=>u.pathname===expected),t(p,"offer-message-open-existing").click()]);
     await t(p,"offer-workspace").waitFor();check(out.writeAttempts===0,"unexpected write attempted");
     return {role,locale,offerId:d.offerId,variant:d.catalogVariantId,from:pre,to:new URL(p.url()).pathname};
    }finally{await ctx.close()}
   });
  }
 }catch(e){out.error=String(e.message).slice(0,800)}finally{if(browser)await browser.close();out.status=!out.error&&out.writeAttempts===0&&Object.keys(out.cases).length===2&&Object.values(out.cases).every(x=>x.status==="PASS")?"PASS":"BLOCKED";save();console.log(JSON.stringify(out));process.exitCode=out.status==="PASS"?0:2}
}
main().catch(e=>{console.error(e.message);process.exitCode=2});
