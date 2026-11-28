const fs=require("fs"),path=require("path");const {randomUUID}=require("crypto");
const {execFileSync}=require("child_process");const L=require("../TB-296-P1-C-INTEGRATION/finalacceptance-d2-lib.cjs");
const H="http://127.0.0.1:5088";const PSQL=process.env.P1C_PSQL||path.resolve(__dirname,"../TB-296-P1-C-INTEGRATION/p1c-psql.exe");
const ev=process.env.P1C_EVIDENCE_DIR||path.join(__dirname,"owner-uat-evidence");fs.mkdirSync(ev,{recursive:true});
const OUT=path.join(ev,"owner-uat-http.json");
const q=s=>"'" + String(s).replaceAll("'","''") + "'";
const sql=s=>execFileSync(PSQL,["-X","-qAt","-v","ON_ERROR_STOP=1","-d","tooba_alpha","-c",s],{encoding:"utf8",timeout:60000,windowsHide:true}).trim();
const json=s=>JSON.parse(sql(s));
const call=async(route,headers={},method="GET",body)=>{const r=await fetch(H+route,{method,headers:{"Accept-Language":"fa-IR",...headers,...(body===undefined?{}:{"Content-Type":"application/json"})},body:body===undefined?undefined:JSON.stringify(body),signal:AbortSignal.timeout(20000)});const t=await r.text();let b;try{b=t?JSON.parse(t):null}catch{b=t}return{status:r.status,body:b}};
const adminH=a=>({"X-Tooba-Dev-Actor-User-Id":a});const sellerH=(s,a)=>({"X-Tooba-Seller-Party-Id":s,"X-Tooba-Dev-Actor-User-Id":a});
const out={task:"TB-296-OWNER-UAT-FINAL-CLOSURE",head:sql("select 'x'"),cases:{},fixtures:[],cleanup:[],status:"NOT_RUN"};
const save=()=>fs.writeFileSync(OUT,JSON.stringify(out,null,2));
const counts=id=>json(`select jsonb_build_object('prices',(select count(*) from pricing.prices where offer_id='${id}'),'facts',(select count(*) from pricing.offer_price_changes where offer_id='${id}'),'positions',(select count(*) from inventory.stock_positions where offer_id='${id}'),'invfacts',(select count(*) from inventory.offer_inventory_changes where offer_id='${id}'))`);
let AUTH=null;
const detail=async id=>call("/v1/admin/offers/"+id,AUTH||{}).then(x=>x.body);
(async()=>{
 const dev=await call("/v1/admin/dev-context");const auth=adminH(dev.body.actorUserId);AUTH=auth;
 const sellers=await call("/v1/seller/dev-contexts");const sA=sellers.body.actors.find(x=>x.sellerLabel.includes("آرمان"));const sB=sellers.body.actors.find(x=>x.sellerLabel.includes("دیجی"))||sellers.body.actors.find(x=>x.sellerLabel!==sA.sellerLabel);
 const free=(seller)=>{const v=sql(`select v.variant_id::text from catalog.variants v join catalog.products p on p.product_id=v.product_id where p.status='Published' and v.status='Published' and p.quantity_decimal_places=0 and not exists (select 1 from offer.offers o where o.seller_party_id=${q(seller)} and o.catalog_variant_id=v.variant_id) limit 1`);return v};
 const seller=sB.sellerPartyId;const variant=free(seller);
 if(!variant){out.preflight="no free published variant for selected seller";}
 const created=await call("/v1/admin/offers",auth,"POST",{sellerPartyId:seller,catalogVariantId:variant,channel:"Marketplace",sellerSku:"R11-UAT-"+randomUUID(),status:"Draft",requestId:randomUUID()});
 if(created.status!==201){out.preflight="create "+created.status+" "+JSON.stringify(created.body).slice(0,300);save();return}
 const id=created.body.offerId;out.fixtures.push({id,seller,variant,sku:"R11-UAT"});save();
 const irr=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{amount:100000,currency:"IRR",expectedVersion:"absent",operationId:randomUUID()});
 const usd=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{amount:13.25,currency:"USD",expectedVersion:"absent",operationId:randomUUID()});
 const eur=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{amount:20.5,currency:"EUR",expectedVersion:"absent",operationId:randomUUID()});
 let d=eur.body;const rows=d.prices;const rev=c=>rows.find(x=>x.currency===c)?.revision;
 // R: retirement
 const op=randomUUID();const before=counts(id);const ret=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{retire:true,currency:"EUR",expectedVersion:rev("EUR"),operationId:op});
 const after=counts(id);const d2=await detail(id);const eur2=d2.prices.find(x=>x.currency==="EUR");const usd2=d2.prices.find(x=>x.currency==="USD");const irr2=d2.prices.find(x=>x.currency==="IRR");
 const fact=json(`select jsonb_build_object('retired',retired,'old',old_amount,'new',new_amount) from pricing.offer_price_changes where operation_id='${op}'`);
 out.cases["price-retire-eur"]={status:ret.status===200&&fact.retired===true&&Number(fact.new)===20.5&&eur2.status==="Retired"&&Number(eur2.amount)===20.5&&usd2.status==="Active"&&usd2.amount===13.25&&irr2.status==="Active"&&after.prices===before.prices?"PASS":"FAIL",http:ret.status,receipt:ret.body,fact,eur:eur2,usd:usd2,irr:irr2,before,after};save();
 const replay=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{retire:true,currency:"EUR",expectedVersion:rev("EUR"),operationId:op});
 const replayFacts=Number(sql(`select count(*) from pricing.offer_price_changes where operation_id='${op}'`));
 out.cases["price-retire-replay"]={status:replay.status===200&&replayFacts===1?"PASS":"FAIL",http:replay.status,facts:replayFacts};save();
 const changed=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{retire:true,currency:"USD",expectedVersion:rev("USD"),operationId:op});
 out.cases["price-retire-changed-intent"]={status:(changed.status===409||changed.status===400)?"PASS":"FAIL",http:changed.status,errorCode:changed.body&&changed.body.errorCode};save();
 // concurrent retire USD + set USD on same revision
 const opA=randomUUID(),opB=randomUUID();const [ra,rb]=await Promise.all([
  call(`/v1/admin/offers/${id}/price`,auth,"PUT",{retire:true,currency:"USD",expectedVersion:rev("USD"),operationId:opA}),
  call(`/v1/admin/offers/${id}/price`,auth,"PUT",{amount:14,currency:"USD",expectedVersion:rev("USD"),operationId:opB})]);
 const wins=[ra,rb].filter(x=>x.status===200);const losses=[ra,rb].filter(x=>x.status>=400);
 out.cases["price-retire-concurrent"]={status:wins.length===1&&losses.length===1?"PASS":"FAIL",statuses:[ra.status,rb.status],codes:losses.map(x=>x.body&&x.body.errorCode)};save();
 // primary retire + purchase prevention: ensure Active + stock first
 const locs=await call("/v1/admin/offers/inventory-locations",auth);const prov=await call(`/v1/admin/offers/${id}/inventory/positions`,auth,"POST",{locationId:locs.body[0].locationId});
 let d3=await call(`/v1/admin/offers/${id}`,auth).then(x=>x.body);const stockId=prov.body&&prov.body.stockItemId;const row=d3.inventoryLocations.find(x=>x.stockItemId===stockId);
 const init=await call(`/v1/admin/offers/${id}/inventory`,auth,"PUT",{onHand:100,stockItemId:stockId,expectedVersion:row.revision,operationId:randomUUID()});
 out.cases["inventory-initial-no-reason"]={status:init.status===200?"PASS":"FAIL",http:init.status,errorCode:init.body&&init.body.errorCode};save();
 const afterInit=await call(`/v1/admin/offers/${id}`,auth).then(x=>x.body);const row2=afterInit.inventoryLocations.find(x=>x.stockItemId===stockId);
 const noReason=await call(`/v1/admin/offers/${id}/inventory`,auth,"PUT",{onHand:90,stockItemId:stockId,expectedVersion:row2.revision,operationId:randomUUID()});
 out.cases["inventory-subsequent-requires-reason"]={status:noReason.status===400&&noReason.body&&noReason.body.errorCode==="inventory.adjustment.reason_required"?"PASS":"FAIL",http:noReason.status,errorCode:noReason.body&&noReason.body.errorCode};save();
 const set0=await call(`/v1/admin/offers/${id}/inventory`,auth,"PUT",{onHand:0,reason:"UAT zero",stockItemId:stockId,expectedVersion:row2.revision,operationId:randomUUID()});
 const d4=await call(`/v1/admin/offers/${id}`,auth).then(x=>x.body);const row3=d4.inventoryLocations.find(x=>x.stockItemId===stockId);
 const zeroNoReason=await call(`/v1/admin/offers/${id}/inventory`,auth,"PUT",{onHand:10,stockItemId:stockId,expectedVersion:row3.revision,operationId:randomUUID()});
 out.cases["inventory-zero-requires-reason"]={status:set0.status===200&&zeroNoReason.status===400&&zeroNoReason.body&&zeroNoReason.body.errorCode==="inventory.adjustment.reason_required"?"PASS":"FAIL",setZero:set0.status,http:zeroNoReason.status,errorCode:zeroNoReason.body&&zeroNoReason.body.errorCode};save();
 // cross-seller denial
 const beforeX=counts(id);const x=await call(`/v1/seller/offers/${id}/inventory`,sellerH(sA.sellerPartyId,sA.actorUserId),"PUT",{onHand:1,reason:"x",stockItemId:stockId,expectedVersion:row3.revision,operationId:randomUUID()});const afterX=counts(id);
 out.cases["inventory-cross-seller-denied"]={status:[403,404].includes(x.status)&&JSON.stringify(beforeX)===JSON.stringify(afterX)?"PASS":"FAIL",http:x.status,errorCode:x.body&&x.body.errorCode,before:beforeX,after:afterX};save();
 // activate and cart: before retire primary must be addable; after retire not
 const set100=await call(`/v1/admin/offers/${id}/inventory`,auth,"PUT",{onHand:100,reason:"UAT stock",stockItemId:stockId,expectedVersion:row3.revision,operationId:randomUUID()});
 const dAct=await call(`/v1/admin/offers/${id}`,auth).then(x=>x.body);const active=await call(`/v1/admin/offers/${id}`,auth,"PATCH",{status:"Active",expectedVersion:dAct.revisions.offer});
 const cart=async()=>{const c=await call("/v1/storefront/cart",{},"POST",{});return c.body};
 const c1=await cart();const add1=await call(`/v1/storefront/cart/${c1.cartId}/lines`,{"X-Tooba-Guest-Secret":c1.guestSecret,"X-Tooba-Cart-Version":String(c1.version)},"POST",{offerId:id,quantity:1});
 const dIrr=await call(`/v1/admin/offers/${id}`,auth).then(x=>x.body);const irrRev=dIrr.prices.find(x=>x.currency==="IRR").revision;const opIrr=randomUUID();const retireIrr=await call(`/v1/admin/offers/${id}/price`,auth,"PUT",{retire:true,currency:"IRR",expectedVersion:irrRev,operationId:opIrr});
 const dAfter=await call(`/v1/admin/offers/${id}`,auth).then(x=>x.body);const c2=await cart();const add2=await call(`/v1/storefront/cart/${c2.cartId}/lines`,{"X-Tooba-Guest-Secret":c2.guestSecret,"X-Tooba-Cart-Version":String(c2.version)},"POST",{offerId:id,quantity:1});
 out.cases["primary-retire-prevents-purchase"]={status:active.status===200&&add1.status===200&&retireIrr.status===200&&dAfter.amount==null&&add2.status>=400?"PASS":"FAIL",activate:active.status,addBefore:add1.status,retire:retireIrr.status,primaryAfter:{amount:dAfter.amount,currency:dAfter.currency},addAfter:add2.status,addAfterCode:add2.body&&add2.body.errorCode};save();
 // catalog phone unit root cause
 const phone=json(`select jsonb_build_object('productId',p.product_id,'unit',u.code,'precision',p.quantity_decimal_places,'step',p.quantity_step,'title',lt.value) from catalog.products p join catalog.units_of_measure u on u.unit_of_measure_id=p.unit_of_measure_id left join catalog.localized_texts lt on lt.owner_id=p.product_id and lt.field_key='name' and lt.locale='fa-IR' where p.product_id='01a05387-fbd0-7000-acd3-4382ce92c773'`);
 const pcs=json(`select jsonb_build_object('productId',p.product_id,'unit',u.code,'precision',p.quantity_decimal_places,'step',p.quantity_step,'title',lt.value) from catalog.products p join catalog.units_of_measure u on u.unit_of_measure_id=p.unit_of_measure_id left join catalog.localized_texts lt on lt.owner_id=p.product_id and lt.field_key='name' and lt.locale='fa-IR' where p.product_id='01a05387-fdf5-7000-95a0-5d7cd42a8747'`);
 out.cases["catalog-phone-unit-root-cause"]={status:phone.unit==="kg"&&Number(phone.precision)===2&&pcs.unit==="pcs"?"PASS":"FAIL",phoneWithKilogram:phone,phoneWithPcs:pcs,rootCause:"Catalog seed data: product 01a05387-fbd0-7000-acd3-4382ce92c773 (گوشی هوشمند سری 1) references unit kg (Mass) with quantity_decimal_places 2; Offer/Inventory only project the Catalog unit. Other phone products use pcs/0. This is Catalog seed data, not an Offer projection bug."};save();
 // cleanup carts + fixture
 for(const c of [c1,c2]){if(c&&c.cartId){try{sql(`BEGIN; DELETE FROM cart.cart_lines WHERE cart_id='${c.cartId}'; DELETE FROM cart.carts WHERE cart_id='${c.cartId}'; COMMIT;`)}catch{}}}
 out.status=Object.values(out.cases).every(x=>x.status==="PASS")?"PASS":"FAIL";save();
 const fixture=out.fixtures[0];try{const c=counts(fixture.id);const s=q(fixture.id);sql(`BEGIN; DELETE FROM inventory.reservations WHERE stock_item_id IN (SELECT stock_item_id FROM inventory.stock_positions WHERE offer_id=${s}); DELETE FROM inventory.offer_inventory_changes WHERE offer_id=${s}; DELETE FROM inventory.offer_allocations WHERE offer_id=${s}; DELETE FROM inventory.stock_positions WHERE offer_id=${s}; DELETE FROM pricing.offer_price_changes WHERE offer_id=${s}; DELETE FROM pricing.prices WHERE offer_id=${s}; DELETE FROM pricing.outbox_messages WHERE strpos(payload::text,${s})>0; DELETE FROM offer.offer_history WHERE offer_id=${s}; DELETE FROM offer.offers WHERE offer_id=${s}; DELETE FROM offer.outbox_messages WHERE strpos(payload::text,${s})>0; DELETE FROM inventory.outbox_messages WHERE strpos(payload::text,${s})>0; COMMIT;`);out.cleanup.push({status:"PASS",id:fixture.id,before:c,after:counts(fixture.id)})}catch(e){out.cleanup.push({status:"BLOCKED",id:fixture.id,reason:String(e).slice(0,300)})};save();
 console.log(JSON.stringify({cases:Object.fromEntries(Object.entries(out.cases).map(([k,v])=>[k,v.status])),status:out.status,cleanup:out.cleanup,preflight:out.preflight||null}));
})().catch(e=>{out.error=String(e&&e.stack?e.stack:e);save();console.error(out.error);process.exit(1)});
