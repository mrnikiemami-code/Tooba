"use strict";
// Architect-authored direct verification. No Agent, no product changes, no new service.
const fs=require("node:fs"), path=require("node:path"), cp=require("node:child_process");
const {randomUUID,createHash}=require("node:crypto");
const ROOT=path.resolve(__dirname,"../../../..");
const HOST="http://127.0.0.1:5088", FRONT="http://127.0.0.1:3000";
const candidate="4f19708be3d980a22535fb446d3cfaa1a2ddfc9c";
const expectedBuild="fzXq0vXgQ2zeCrG2p_IBW";
const expectedHost="10098e16bfca69fa41a736609b2535aad8e911457db194ed916a112e4c379529";
const runId=randomUUID();
const ev=path.join(__dirname,"direct-evidence",runId);
fs.mkdirSync(ev,{recursive:true});
const file=path.join(ev,"DIRECT-RESULT.json");
const out={runId,candidate,identity:{},cases:{},fixtures:[],cleanup:[],status:"NOT_RUN"};
const save=()=>fs.writeFileSync(file,JSON.stringify(out,null,2)+"\n");
const check=(ok,msg)=>{if(!ok)throw new Error(msg)};
const q=s=>"'"+String(s).replaceAll("'","''")+"'";
const uuid=s=>{check(typeof s==="string"&&/^[0-9a-f-]{36}$/i.test(s),"invalid UUID");return s.toLowerCase()};
const eq=(a,b)=>JSON.stringify(a)===JSON.stringify(b);
const PSQL=process.env.P1C_PSQL||[path.join(__dirname,"../TB-296-P1-C-INTEGRATION/p1c-psql.exe")].find(fs.existsSync)||"psql";
const PW=process.env.P1C_PLAYWRIGHT||[path.join(ROOT,"src/frontend/node_modules/playwright"),"C:/Users/User/OpenCode-UI-Lab-20261122-064208/node_modules/playwright"].find(fs.existsSync);
function exec(cmd,args,timeout=60000){return cp.execFileSync(cmd,args,{cwd:ROOT,encoding:"utf8",timeout,windowsHide:true,stdio:["ignore","pipe","pipe"]}).trim()}
const sql=s=>exec(PSQL,["-X","-qAt","-v","ON_ERROR_STOP=1","-d","tooba_alpha","-c",s]);
const json=s=>JSON.parse(sql(s));
const sqlIdentity="select current_database() || '|' || coalesce(inet_server_addr()::text,'local') || '|' || coalesce(inet_server_port()::text,'local') || '|' || extract(epoch from pg_postmaster_start_time())::text";
let admin, browser, contexts=[], chosen=new Set();
const actorHeaders=actor=>({"X-Tooba-Dev-Actor-User-Id":actor});
const sellerHeaders=s=>({...actorHeaders(s.actorUserId),"X-Tooba-Seller-Party-Id":s.sellerPartyId});
async function call(route,headers={},method="GET",body){
 const r=await fetch(HOST+route,{method,headers:{"Accept-Language":"fa-IR",...headers,...(body===undefined?{}:{"Content-Type":"application/json"})},body:body===undefined?undefined:JSON.stringify(body),signal:AbortSignal.timeout(20000)});
 const data=await r.json().catch(()=>null);return {status:r.status,body:data};
}
function requireOK(r,name){check(r.status>=200&&r.status<300,name+" HTTP "+r.status+" "+(r.body?.errorCode||""));return r.body}
function counts(id){
 const s=q(uuid(id));
 return json("select jsonb_build_object('offers',(select count(*) from offer.offers where offer_id="+s+"),'prices',(select count(*) from pricing.prices where offer_id="+s+"),'priceFacts',(select count(*) from pricing.offer_price_changes where offer_id="+s+"),'positions',(select count(*) from inventory.stock_positions where offer_id="+s+"),'inventoryFacts',(select count(*) from inventory.offer_inventory_changes where offer_id="+s+"),'allocations',(select count(*) from inventory.offer_allocations where offer_id="+s+"),'history',(select count(*) from offer.offer_history where offer_id="+s+"),'pricingOutbox',(select count(*) from pricing.outbox_messages where strpos(payload::text,"+s+")>0),'offerOutbox',(select count(*) from offer.outbox_messages where strpos(payload::text,"+s+")>0),'inventoryOutbox',(select count(*) from inventory.outbox_messages where strpos(payload::text,"+s+")>0))");
}
function sqlPrices(id){
 return json("select coalesce(jsonb_agg(jsonb_build_object('currency',currency,'amount',amount,'status',status) order by currency),'[]'::jsonb) from pricing.prices where offer_id="+q(uuid(id))+" and qualifier_kind='Base'");
}
function skuRows(f){
 return json("select coalesce(jsonb_agg(jsonb_build_object('id',offer_id,'seller',seller_party_id,'variant',catalog_variant_id,'channel',channel)),'[]'::jsonb) from offer.offers where seller_sku="+q(f.sku));
}
function identityAbsent(s,v,ch){
 return Number(sql("select count(*) from offer.offers where seller_party_id="+q(uuid(s))+" and catalog_variant_id="+q(uuid(v))+" and channel="+q(ch)));
}
async function detail(f,role=f.role){
 return requireOK(await call("/v1/"+role+"/offers/"+uuid(f.id),role==="admin"?admin:sellerHeaders(f.seller)),"detail "+role);
}
const priceRow=(d,c)=>d.prices?.find(x=>x.currency===c);
const t=(p,id)=>p.getByTestId(id);
const routeIs=(r,pathname,method="GET")=>r.request().method()===method&&new URL(r.url()).pathname===pathname;
const norm=s=>String(s).replace(/[۰-۹]/g,d=>"۰۱۲۳۴۵۶۷۸۹".indexOf(d)).replace(/[٠-٩]/g,d=>"٠١٢٣٤٥٦٧٨٩".indexOf(d));
async function stage(id,fn){
 console.log("START "+id);out.cases[id]={status:"NOT_RUN"};save();
 try{const r=await fn();out.cases[id]={status:"PASS",...r};}
 catch(e){out.cases[id]={status:"BLOCKED_OR_FAIL",reason:e.message.slice(0,700)};}
 save();console.log(id+" "+out.cases[id].status);
}
async function waitEnabled(locator){
 const end=Date.now()+20000;
 while(Date.now()<end){if(await locator.isVisible().catch(()=>false)&&await locator.isEnabled().catch(()=>false))return;await new Promise(r=>setTimeout(r,200));}
 throw new Error("control did not become enabled");
}
async function waitUntil(fn,desc){
 const end=Date.now()+25000;
 while(Date.now()<end){const r=await fn();if(r)return r;await new Promise(r=>setTimeout(r,400));}
 throw new Error(desc);
}
function freeProducts(s,ch){
 const rows=json("select coalesce(jsonb_agg(jsonb_build_object('productId',v.product_id,'variantId',v.variant_id)),'[]'::jsonb) from catalog.variants v join catalog.products p on p.product_id=v.product_id where v.status='Published' and p.quantity_decimal_places=0 and not exists(select 1 from offer.offers o where o.seller_party_id="+q(uuid(s))+" and o.catalog_variant_id=v.variant_id and o.channel="+q(ch)+")");
 const map=new Map();for(const r of rows){if(!map.has(r.productId))map.set(r.productId,new Set());map.get(r.productId).add(r.variantId)}return map;
}
async function discover(role,sellers,needMulti=false){
 const base=role==="admin"?"/v1/admin/catalog/products":"/v1/seller/catalog-products";
 for(const seller of sellers){
  const headers=role==="admin"?admin:sellerHeaders(seller);
  const cr=await call("/v1/"+role+"/offers/allowed-channels",headers);
  if(cr.status!==200||!Array.isArray(cr.body)||!cr.body.includes("Marketplace"))continue;
  const ch="Marketplace", free=freeProducts(seller.sellerPartyId,ch);
  if(!free.size)continue;
  for(let page=1;page<=60;page++){
   const res=await call(base+"?"+new URLSearchParams({page:String(page),pageSize:"100",locale:"fa-IR"}),headers);
   const list=requireOK(res,"catalog list");
   check(Array.isArray(list.items),"catalog items absent");
   for(const product of list.items){
    if(!free.has(product.productId)||!product.title||!(product.publishedVariantCount>0))continue;
    const result=await call(base+"/"+uuid(product.productId)+"/variants?locale=fa-IR",headers);
    if(result.status!==200||!Array.isArray(result.body))continue;
    const v=result.body.find(x=>x.status==="Published"&&x.catalogCode&&free.get(product.productId).has(x.catalogVariantId)&&!chosen.has(x.catalogVariantId)&&(!needMulti||(x.axisLabels?.length||0)>1));
    if(!v)continue;
    // A direct catalog API search must expose the exact product in the real picker page.
    const sr=await call(base+"?"+new URLSearchParams({page:"1",pageSize:"10",locale:"fa-IR",search:product.title}),headers);
    const hits=sr.body?.items||[];
    if(sr.status!==200||hits.filter(x=>x.title===product.title).length!==1||!hits.some(x=>x.productId===product.productId&&x.title===product.title))continue;
    if(identityAbsent(seller.sellerPartyId,v.catalogVariantId,ch)!==0)continue;
    chosen.add(v.catalogVariantId);
    return {role,seller,channel:ch,product:{id:product.productId,title:product.title},variant:{id:v.catalogVariantId,code:v.catalogCode,label:v.displayLabel,axes:v.axisLabels},sku:"R11-DIRECT-"+role+"-"+runId};
   }
   if(!list.hasMore)break;
  }
 }
 throw new Error("No unambiguous free published tuple visible to "+role+" through real catalog APIs");
}
async function newPage(f){
 const context=await browser.newContext({locale:"fa-IR",viewport:{width:1440,height:844}});
 contexts.push(context);
 await context.addInitScript(x=>{localStorage.setItem("tooba.adminActorUserId",x.admin);localStorage.setItem("tooba.sellerPartyId",x.seller);localStorage.setItem("tooba.sellerActorUserId",x.actor)}, {admin:admin["X-Tooba-Dev-Actor-User-Id"],seller:f.seller.sellerPartyId,actor:f.seller.actorUserId});
 const p=await context.newPage();p.setDefaultTimeout(15000);p.setDefaultNavigationTimeout(25000);
 return p;
}
const uiPath=(f,locale="fa",id="new")=>FRONT+"/"+locale+(f.role==="admin"?"/admin/offers/":"/vendor-panel/products/")+id;
async function pricing(p){await t(p,"offer-tab-button-pricing").click();await t(p,"offer-tab-pricing").waitFor();await waitEnabled(t(p,"offer-price-currency-select"))}
async function createUI(f,p){
 check(skuRows(f).length===0&&identityAbsent(f.seller.sellerPartyId,f.variant.id,f.channel)===0,"fixture is not new");
 out.fixtures.push(f);save();
 await p.goto(uiPath(f));await t(p,"offer-workspace").waitFor();
 if(f.role==="admin"){
  await t(p,"offer-seller-picker-toggle").click();
  const sellerSearch=t(p,"offer-seller-dialog-search");await sellerSearch.fill(f.seller.sellerLabel);
  await sellerSearch.press("Enter");
  const seller=t(p,"offer-seller-dialog-option").filter({hasText:f.seller.sellerLabel});
  await seller.first().waitFor();check(await seller.count()===1,"seller name ambiguous");await seller.click();
 }
 await waitUntil(async()=>await t(p,"offer-channel-select").locator("option[value=Marketplace]").count(),"channel not available");
 await t(p,"offer-channel-select").selectOption(f.channel);
 await t(p,"offer-product-picker-toggle").click();
 const search=t(p,"offer-product-dialog-search");
 await search.fill(f.product.title);
 const pathname=f.role==="admin"?"/v1/admin/catalog/products":"/v1/seller/catalog-products";
 const searched=p.waitForResponse(r=>routeIs(r,pathname)&&new URL(r.url()).searchParams.get("search")===f.product.title);
 await search.press("Enter");
 const response=await searched;check(response.ok(),"real UI search failed");
 const body=await response.json();
 check(body.items?.some(x=>x.productId===f.product.id&&x.title===f.product.title),"UI search did not expose verified product id");
 const option=t(p,"offer-product-dialog-option").filter({has:p.getByText(f.product.title,{exact:true})});
 await option.first().waitFor();check(await option.count()===1,"product title ambiguous");await option.click();
 const toggle=t(p,"offer-variant-picker-toggle");await waitEnabled(toggle);await toggle.click();
 const variant=t(p,"offer-variant-option").filter({has:p.getByText(f.variant.code,{exact:true})});
 await variant.first().waitFor();check(await variant.count()===1,"variant code ambiguous");await variant.click();
 await t(p,"offer-seller-sku").fill(f.sku);
 await pricing(p);
 await t(p,"offer-price-currency-select").selectOption("EUR");
 await t(p,"offer-price-input").fill("20.5");
 const submit=t(p,f.role==="admin"?"admin-offer-submit":"seller-create-submit").first();await waitEnabled(submit);
 const request=p.waitForResponse(r=>routeIs(r,"/v1/"+f.role+"/offers","POST"));
 await submit.click();const res=await request;
 const payload=res.request().postDataJSON();
 check(payload.catalogVariantId===f.variant.id&&payload.sellerSku===f.sku&&payload.channel===f.channel,"UI submitted a different identity");
 if(f.role==="admin")check(payload.sellerPartyId===f.seller.sellerPartyId,"UI submitted different seller");
 check(res.status()===201,"create HTTP "+res.status());
 const b=await res.json();f.id=uuid(b.offerId??b.detail?.offerId);save();
 await p.waitForURL(u=>u.pathname.endsWith("/"+f.id),{timeout:30000});
 const d=await detail(f);
 check(d.sellerPartyId===f.seller.sellerPartyId&&d.catalogVariantId===f.variant.id&&d.channel===f.channel,"created detail identity mismatch");
 check(priceRow(d,"EUR")?.amount===20.5&&d.currency==="IRR"&&d.amount==null,"non-default create or primary mismatch");
 check(skuRows(f).length===1&&sqlPrices(f.id).length===1,"create SQL row count");
 f.initial={prices:d.prices.map(x=>({currency:x.currency,amount:x.amount,revision:x.revision})),amount:d.amount,currency:d.currency};save();
}
async function addUIPrice(f,p,c,amount){
 await pricing(p);await t(p,"offer-price-currency-select").selectOption(c);await t(p,"offer-price-input").fill(String(amount));
 const submit=t(p,f.role==="admin"?"admin-offer-save":"offer-save").first();await waitEnabled(submit);
 const response=p.waitForResponse(r=>routeIs(r,"/v1/"+f.role+"/offers/"+f.id+"/price","PUT"));
 await submit.click();const res=await response;check(res.ok(),"UI price HTTP "+res.status());
 await waitUntil(async()=>{const d=await detail(f);return priceRow(d,c)?.amount===amount?d:null},"price not persisted");
 await p.reload();await t(p,"offer-workspace").waitFor();await pricing(p);
 const d=await detail(f);
 check(priceRow(d,c)?.amount===amount,"reload readback mismatch");return d;
}
async function verifyCreate(f,p){
 await createUI(f,p);
 const d=await addUIPrice(f,p,"USD",13.25);
 check(priceRow(d,"EUR")?.revision===priceRow(f.initial,"EUR")?.revision&&priceRow(d,"EUR")?.amount===20.5&&d.amount===null&&d.currency==="IRR","foreign currency overwrote primary/other revision");
 check(priceRow(d,"USD")?.revision&&priceRow(d,"USD").revision!==priceRow(d,"EUR").revision,"currency revisions not independent");
 const rows=sqlPrices(f.id), c=counts(f.id);
 check(rows.length===2&&rows.some(x=>x.currency==="EUR"&&Number(x.amount)===20.5)&&rows.some(x=>x.currency==="USD"&&Number(x.amount)===13.25)&&c.priceFacts===2,"SQL prices/facts mismatch");
 return {role:f.role,offerId:f.id,selected:f.variant.id,rows,counts:c,primary:{amount:d.amount,currency:d.currency},revisions:d.prices.map(x=>({currency:x.currency,revision:x.revision}))};
}
async function localeAndGeometry(f,p){
 const priced=await addUIPrice(f,p,"IRR",100000);
 check(priced.amount===100000,"primary integer seed failed");
 const results=[];
 for(const [locale,code,dir,label] of [["fa","fa-IR","rtl","قیمت‌گذاری"],["en","en-US","ltr","Pricing"],["ru","ru-RU","ltr","Цены"]]){
  if(locale!=="fa"){
   await Promise.all([p.waitForURL(u=>u.pathname.startsWith("/"+locale+"/")),t(p,"panel-language-select").selectOption(locale)]);
  }
  await t(p,"offer-workspace").waitFor();await pricing(p);
  const root=t(p,"offer-workspace"),text=await t(p,"offer-tab-pricing").innerText();
  check(await root.getAttribute("lang")===code&&await root.getAttribute("dir")===dir&&text.includes(label),"seller locale mismatch "+locale);
  await t(p,"offer-price-currency-select").selectOption(locale==="ru"?"USD":"IRR");
  const amount=await t(p,"offer-price-input").inputValue();
  check(locale==="fa"?/[۰-۹]/.test(amount)&&norm(amount).replace(/[^0-9]/g,"")==="100000":locale==="ru"?amount.includes(",25"):amount.replace(/[^0-9]/g,"")==="100000","localized amount mismatch "+locale);
  results.push({locale,code,dir,amount,heading:label});
 }
 out.cases["R11-LOCALE-01-seller"]={status:"PASS",results};save();
 await Promise.all([p.waitForURL(u=>u.pathname.startsWith("/fa/")),t(p,"panel-language-select").selectOption("fa")]);
 const label=priced.variantLabel;check(f.variant.axes?.length>1&&label,"multi-attribute fixture unavailable");
 const geometry=[];
 for(const width of [390,1440]){
  await p.setViewportSize({width,height:844});await t(p,"offer-tab-button-identity").click();
  const v=await p.evaluate(label=>{const root=document.querySelector('[data-testid="offer-workspace"]'),head=document.querySelector('[data-testid="offer-workspace-header"]'),tab=document.querySelector('[data-testid="offer-tab-identity"]');const count=x=>String(x||"").split(label).length-1;return {overflow:document.documentElement.scrollWidth-document.documentElement.clientWidth,dir:root?.getAttribute("dir"),headerCount:count(head?.textContent),fieldCount:count(tab?.textContent),width:root?.getBoundingClientRect().width}},label);
  check(v.overflow<=0&&v.dir==="rtl"&&v.headerCount<=1&&v.fieldCount===1&&v.width<=width+1,"seller geometry or duplicated variant at "+width);
  await pricing(p);const headers=await t(p,"offer-tab-pricing").locator("thead th").allTextContents();
  check(headers.length===3&&!headers.some(x=>/بازار|market/i.test(x)),"unexpected pricing headers");geometry.push({width,...v,headers});
 }
 out.cases["R11-UAT-03-seller"]={status:"PASS",geometry};save();
 return {locale:results,geometry};
}
async function deniedStock(target,other){
 const locations=requireOK(await call("/v1/seller/offers/inventory-locations",sellerHeaders(other)),"other seller locations");
 check(Array.isArray(locations)&&locations.length>0,"no real authorized other-seller location");
 const before=counts(target.id),locationId=uuid(locations[0].locationId);
 const r=await call("/v1/seller/offers/"+target.id+"/inventory/positions",sellerHeaders(other),"POST",{locationId});
 const after=counts(target.id);
 check([403,404].includes(r.status)&&r.body?.errorCode&&eq(before,after),"foreign stock denial missing or mutated state");
 return {http:r.status,errorCode:r.body.errorCode,before,after};
}
async function concurrent(f){
 const d=await detail(f,"admin"), before=counts(f.id), operationId=randomUUID();
 const payload={amount:31.25,currency:"USD",expectedVersion:priceRow(d,"USD").revision,operationId};
 const route="/v1/admin/offers/"+f.id+"/price";
 const [a,b]=await Promise.all([call(route,admin,"PUT",payload),call(route,admin,"PUT",payload)]);
 const arow=priceRow(a.body,"USD"),brow=priceRow(b.body,"USD");
 const after=counts(f.id);
 check(a.status===200&&b.status===200&&arow?.amount===31.25&&brow?.amount===31.25&&arow.revision===brow.revision&&after.priceFacts===before.priceFacts+1&&after.prices===before.prices&&after.pricingOutbox-before.pricingOutbox<=1,"concurrent replay is not one durable write");
 const n=Number(sql("select count(*) from pricing.offer_price_changes where offer_id="+q(f.id)+" and operation_id="+q(operationId)));
 check(n===1,"operation fact count wrong");return {statuses:[a.status,b.status],operationId,revision:arow.revision,before,after,factsForOperation:n};
}
function missingMarket(f){
 // Existing trusted DEV configuration only; never print or persist the connection string.
 const override=process.env.Tooba__PostgreSQL__ConnectionReferences__tenant_alpha??process.env["Tooba__PostgreSQL__ConnectionReferences__tenant-alpha"];
 if(!process.env.TOOBA_R11_CONNECTION){
  const config=JSON.parse(fs.readFileSync(path.join(ROOT,"src/backend/Host/Tooba.Host/appsettings.Development.json"),"utf8").replace(/^\uFEFF/,""));
  process.env.TOOBA_R11_CONNECTION=override||config.Tooba?.PostgreSQL?.ConnectionReferences?.["tenant-alpha"]||"";
 }
 check(process.env.TOOBA_R11_CONNECTION,"No existing local DEV connection; market probe not run");
 const market=sql("select market from pricing.prices where offer_id="+q(f.id)+" and qualifier_kind='Base' limit 1");
 check(market,"positive control has no market");
 const before=counts(f.id),prices=sqlPrices(f.id);
 let raw;try{raw=exec("dotnet",["run","--project",path.join(__dirname,"direct-market-probe/DirectMarketProbe.csproj"),"--",f.id,f.seller.sellerPartyId,market],150000)}catch(e){throw new Error("direct-market-probe exit="+e.status+"; inspect local dotnet output without credentials")}
 const line=raw.split(/\r?\n/).find(x=>x.startsWith("R11_MARKET_JSON="));check(line,"market probe did not emit witness");
 const result=JSON.parse(line.slice("R11_MARKET_JSON=".length));check(result.pass&&result.identity===out.identity.database+"|on"&&result.observations?.length===4,"market probe database or oracle mismatch");
 const after=counts(f.id);check(eq(before,after)&&eq(prices,sqlPrices(f.id)),"missing-market probe changed data");
 return {result,before,after};
}
function cleanup(f){
 // Never delete by broad prefix or a pre-existing seller/variant. Reconcile only exact run SKU.
 const rows=skuRows(f);if(!rows.length)return {sku:f.sku,offers:0};
 check(rows.length===1&&rows[0].seller===f.seller.sellerPartyId&&rows[0].variant===f.variant.id&&rows[0].channel===f.channel,"ambiguous cleanup identity; no deletion");
 const id=uuid(rows[0].id),s=q(id);
 check(!f.id||f.id===id,"cleanup identity mismatch");
 const orders=Number(sql('select count(*) from "order".order_lines where offer_id='+s));
 const carts=Number(sql("select count(*) from cart.cart_lines where offer_id="+s));
 check(orders===0&&carts===0,"fixture acquired order/cart commitment; no deletion");
 const statements=[
  "delete from inventory.reservations where stock_item_id in (select stock_item_id from inventory.stock_positions where offer_id="+s+")",
  ...["inventory.offer_inventory_changes","inventory.offer_allocations","inventory.stock_positions","pricing.offer_price_changes","pricing.prices","offer.offer_history"].map(table=>"delete from "+table+" where offer_id="+s),
  ...["pricing","inventory","offer"].map(schema=>"delete from "+schema+".outbox_messages where strpos(payload::text,"+s+")>0"),
  "delete from offer.offers where offer_id="+s+" and seller_sku="+q(f.sku)
 ];
 sql("BEGIN;"+statements.join(";")+";COMMIT;");
 const after=counts(id);check(Object.values(after).every(x=>Number(x)===0),"fixture residue remains");return {sku:f.sku,id,after};
}
async function prepareIndependentFixture(sellers){
 const f=await discover("admin",sellers);f.sku="R11-DIRECT-probe-"+runId;
 check(skuRows(f).length===0&&identityAbsent(f.seller.sellerPartyId,f.variant.id,f.channel)===0,"independent fixture already exists");
 out.fixtures.push(f);save();
 const created=await call("/v1/admin/offers",admin,"POST",{sellerPartyId:f.seller.sellerPartyId,catalogVariantId:f.variant.id,channel:f.channel,sellerSku:f.sku,status:"Draft",requestId:randomUUID()});
 check(created.status===201,"independent fixture HTTP "+created.status);f.id=uuid(created.body.offerId);save();
 for(const [currency,amount] of [["IRR",100000],["USD",13.25]]){
  const r=await call("/v1/admin/offers/"+f.id+"/price",admin,"PUT",{currency,amount,expectedVersion:"absent",operationId:randomUUID()});check(r.status===200,"independent fixture price HTTP "+r.status);
 }
 const d=await detail(f);check(d.amount===100000&&priceRow(d,"USD")?.amount===13.25&&counts(f.id).priceFacts===2,"independent fixture readback mismatch");return f;
}
function hostWitness(){
 let dll=process.env.TOOBA_R11_HOST_DLL;
 if(!dll&&process.platform==="win32"){
  try{
   const ps="$ids = @(Get-NetTCPConnection -LocalPort 5088 -State Listen -ErrorAction Stop | Select-Object -ExpandProperty OwningProcess -Unique); if ($ids.Count -eq 1) { $p=Get-CimInstance Win32_Process -Filter (\"ProcessId=\"+$ids[0]); $p | Select-Object ExecutablePath,CommandLine | ConvertTo-Json -Compress }";
   const info=JSON.parse(exec("powershell.exe",["-NoProfile","-NonInteractive","-Command",ps],12000));
   const matches=String(info.CommandLine||"").match(/(?:\"([A-Za-z]:[^\"]*Tooba\.Host\.dll)\"|([A-Za-z]:[^\s\"]*Tooba\.Host\.dll))/i);
   if(matches)dll=matches[1]||matches[2];
   else if(/Tooba\.Host\.exe$/i.test(info.ExecutablePath||""))dll=info.ExecutablePath.replace(/\.exe$/i,".dll");
  }catch{}
 }
 return dll&&fs.existsSync(dll)?createHash("sha256").update(fs.readFileSync(dll)).digest("hex"):null;
}
function publishEvidence(){
 check(exec("git",["branch","--show-current"])==="opencode/iso-agents-002","publication branch mismatch");
 check(exec("git",["rev-parse","HEAD"])===out.identity.head,"HEAD changed during run");
 check(!exec("git",["status","--porcelain","--untracked-files=no"]),"unrelated tracked or staged changes; result left local");
 const relative=path.relative(ROOT,file).replaceAll("\\","/");
 exec("git",["add","--",relative]);
 check(exec("git",["diff","--cached","--name-only"])===relative,"unexpected staged files");
 exec("git",["commit","-m","test(pricing): record direct R11 verification "+runId,"--",relative],60000);
 const head=exec("git",["rev-parse","HEAD"]);
 exec("git",["push","origin","HEAD:refs/heads/opencode/iso-agents-002"],90000);
 const remote=exec("git",["ls-remote","origin","refs/heads/opencode/iso-agents-002"],60000).split(/\s+/)[0];
 check(remote===head,"remote HEAD differs after push");console.log("REMOTE HEAD "+remote);
}
async function main(){
 save();
 let a,s,ap,sp;
 try{
  check(exec("git",["branch","--show-current"])==="opencode/iso-agents-002","wrong branch");
  check(!exec("git",["status","--porcelain","--untracked-files=no"]),"tracked checkout dirty");
  const delta=exec("git",["diff","--name-only",candidate,"HEAD"]).split(/\r?\n/).filter(Boolean);
  check(delta.every(p=>p.startsWith("docs/opencode/tasks/")),"product differs from verified candidate");
  out.identity.head=exec("git",["rev-parse","HEAD"]);
  const buildFile=path.join(ROOT,"src/frontend/.next/BUILD_ID");
  out.identity.nextBuildId=fs.existsSync(buildFile)?fs.readFileSync(buildFile,"utf8").trim():null;
  check(out.identity.nextBuildId===expectedBuild,"Next build identity changed or absent");
  out.identity.hostDll=hostWitness();
  check(!out.identity.hostDll||out.identity.hostDll===expectedHost,"Host DLL identity changed");
  out.identity.database=sql(sqlIdentity);check(out.identity.database.startsWith("tooba_alpha|"),"wrong DB");
  check((await fetch(HOST+"/health",{signal:AbortSignal.timeout(10000)})).ok,"Host unhealthy");
  check((await fetch(FRONT+"/fa/admin/offers/new",{signal:AbortSignal.timeout(20000)})).ok,"Next unhealthy");
  const dev=requireOK(await call("/v1/admin/dev-context"),"admin context");admin=actorHeaders(uuid(dev.actorUserId));
  const sellerPayload=requireOK(await call("/v1/seller/dev-contexts"),"seller contexts");
  const sellerRows=sellerPayload.actors;
  check(Array.isArray(sellerRows),"seller context shape");
  const sellers=sellerRows.filter(x=>x.sellerPartyId&&x.actorUserId&&x.sellerLabel).map(x=>({sellerPartyId:uuid(x.sellerPartyId),actorUserId:uuid(x.actorUserId),sellerLabel:x.sellerLabel}));
  check(sellers.length>=2,"at least two authorized seller contexts required");
  out.identity.legacyFractionalIrr=Number(sql("select count(*) from pricing.prices where currency='IRR' and status='Active' and amount<>trunc(amount)"));
  save();check(PW,"existing Playwright not found");browser=await require(PW).chromium.launch({timeout:20000});
  await stage("R11-PRICE-01-admin",async()=>{a=await discover("admin",sellers);ap=await newPage(a);return verifyCreate(a,ap)});
  await stage("R11-PRICE-01-seller",async()=>{s=await discover("seller",sellers,true);sp=await newPage(s);return verifyCreate(s,sp)});
  let f=out.cases["R11-PRICE-01-admin"]?.status==="PASS"?a:out.cases["R11-PRICE-01-seller"]?.status==="PASS"?s:null;
  if(!f)await stage("independent-probe-fixture",async()=>{f=await prepareIndependentFixture(sellers);return {offerId:f.id,prices:sqlPrices(f.id)}});
  if(f){
   await stage("R11-REF-03-missing-market",()=>missingMarket(f));
   await stage("R11-UAT-02-stock",()=>deniedStock(f,sellers.find(x=>x.sellerPartyId!==f.seller.sellerPartyId)));
   await stage("R11-PRICE-05-concurrent",()=>concurrent(f));
  }else for(const k of ["R11-REF-03-missing-market","R11-UAT-02-stock","R11-PRICE-05-concurrent"])out.cases[k]={status:"NOT_RUN",reason:"no newly created verified fixture"};
  if(out.cases["R11-PRICE-01-seller"]?.status==="PASS")await stage("seller-locale-geometry",()=>localeAndGeometry(s,sp));
  else for(const k of ["R11-LOCALE-01-seller","R11-UAT-03-seller"])out.cases[k]={status:"NOT_RUN",reason:"seller create fixture unavailable"};
  // Checkout requires its own isolated buyable fixture and proven owner-side cleanup.
  // Do not mutate a legacy active offer or delete paid order records from this runner.
  out.cases["R11-REG-01-new-currency-checkout"]={status:"NOT_RUN",reason:"separate isolated checkout proof required; existing paid-order fixture cleanup not assumed safe"};
 }catch(e){out.preflightError=e.message.slice(0,700)}
 finally{
  if(browser)await browser.close().catch(()=>{});
  for(const f of out.fixtures){
   try{out.cleanup.push({status:"PASS",...cleanup(f)})}catch(e){out.cleanup.push({status:"BLOCKED",sku:f.sku,reason:e.message.slice(0,500)})}
  }
  out.status="REVIEW_REQUIRED";save();
  console.log("RESULT "+file);
  console.log(JSON.stringify({cases:Object.fromEntries(Object.entries(out.cases).map(([k,v])=>[k,v.status])),cleanup:out.cleanup.map(x=>x.status),preflightError:out.preflightError,identity:out.identity}));
 }
 if(process.argv.includes("--publish")&&out.identity.head){try{publishEvidence()}catch(e){console.error("Evidence publication stopped; local result preserved. "+String(e.message).slice(0,300));process.exitCode=2;return}}
 process.exitCode=out.preflightError||out.cleanup.some(x=>x.status!=="PASS")||Object.values(out.cases).some(x=>x.status!=="PASS")?2:0;
}
main().catch(e=>{console.error(e.message);process.exitCode=2});
