const { execFileSync } = require('node:child_process');
const fs=require('node:fs');
const DB='tooba_offer285_repair', HOST='http://127.0.0.1:5091';
const A='01a204d0-edb3-7000-abf4-3ce5d8dfd9b3', AA='01a204d0-f03d-7000-ac39-4923fca2d4a0';
const ADMIN='01a204d0-f0f3-7000-b81d-48fb6f0a831d';
const sql=q=>execFileSync('docker',['exec','postgres-db','psql','-U','admin','-d',DB,'-At','-v','ON_ERROR_STOP=1','-c',q],{encoding:'utf8'}).trim();
const vid=code=>sql(`select v.variant_id from catalog.variants v where v.catalog_code_seam='${code}'`);
const hdr=(extra={})=>({Accept:'application/json','Content-Type':'application/json',...extra});
const post=async(url,headers,body)=>{const r=await fetch(url,{method:'POST',headers,body:JSON.stringify(body)});const text=await r.text();let b=null;try{b=JSON.parse(text)}catch{}return{status:r.status,body:b,text}};
const sellerHeaders={...hdr(),'X-Tooba-Seller-Party-Id':A,'X-Tooba-Dev-Actor-User-Id':AA};
const adminHeaders={...hdr(),'X-Tooba-Dev-Actor-User-Id':ADMIN};
(async()=>{
 const out={startedAt:new Date().toISOString()};
 // same request, same intent
 const v166=vid('TB285-VAR-166');
 const bodyA={catalogVariantId:v166,sellerSku:'TB285-REPAIR-SAME-166',status:'Draft',requestId:'tb285-repair-same-166'};
 const s1=await post(`${HOST}/v1/seller/offers`,sellerHeaders,bodyA);
 const s2=await post(`${HOST}/v1/seller/offers`,sellerHeaders,bodyA);
 out.sellerSame={first:s1.status,second:s2.status,sameId:s1.body?.offerId&&s1.body.offerId===s2.body?.offerId,id:s1.body?.offerId};
 // different SKU, same request id -> typed conflict, no row
 const bodyB={...bodyA,sellerSku:'TB285-REPAIR-DIFF-166'};
 const s3=await post(`${HOST}/v1/seller/offers`,sellerHeaders,bodyB);
 out.sellerDifferent={status:s3.status,error:s3.body?.errorCode||s3.body?.title,rows:Number(sql("select count(*) from offer.offers where seller_sku like 'TB285-REPAIR-%-166'"))};
 // admin entry same/different
 const v164=vid('TB285-VAR-164');
 const adminA={sellerPartyId:A,catalogVariantId:v164,sellerSku:'TB285-REPAIR-ADMIN-164',status:'Draft',requestId:'tb285-repair-admin-164'};
 const a1=await post(`${HOST}/v1/admin/offers`,adminHeaders,adminA);
 const a2=await post(`${HOST}/v1/admin/offers`,adminHeaders,adminA);
 const a3=await post(`${HOST}/v1/admin/offers`,adminHeaders,{...adminA,sellerSku:'TB285-REPAIR-ADMIN-DIFF-164'});
 out.adminSame={first:a1.status,second:a2.status,sameId:a1.body?.offerId&&a1.body.offerId===a2.body?.offerId,id:a1.body?.offerId};
 out.adminDifferent={status:a3.status,error:a3.body?.errorCode||a3.body?.title,rows:Number(sql("select count(*) from offer.offers where seller_sku like 'TB285-REPAIR-ADMIN-%-164'"))};
 // simultaneous identical seller create
 const v162=vid('TB285-VAR-162');
 const cbody={catalogVariantId:v162,sellerSku:'TB285-REPAIR-CONCURRENT-162',status:'Draft',requestId:'tb285-repair-concurrent-162'};
 const [c1,c2]=await Promise.all([post(`${HOST}/v1/seller/offers`,sellerHeaders,cbody),post(`${HOST}/v1/seller/offers`,sellerHeaders,cbody)]);
 out.concurrent={first:c1.status,second:c2.status,firstId:c1.body?.offerId,secondId:c2.body?.offerId,sameId:c1.body?.offerId===c2.body?.offerId,rows:Number(sql("select count(*) from offer.offers where seller_sku='TB285-REPAIR-CONCURRENT-162'")),errors:[c1.body?.errorCode,c2.body?.errorCode]};
 const target=process.argv[2]||'docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/evidence/request-id-285.json';
 fs.writeFileSync(target,JSON.stringify(out,null,2)+'\n'); console.log(JSON.stringify(out,null,2));
})();

