import "../order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { ensureDom } from "../order-carrier-execution/test-dom-harness.ts";
import { createAndApplyOffer, type OfferCreateAndApplyDetail, type OfferCreateAndApplyInput, type OfferCreateAndApplyTransport } from "./offer-create-and-apply.ts";

ensureDom();
const input: OfferCreateAndApplyInput = {
  sellerPartyId:"seller",catalogVariantId:"variant",sellerSku:"SKU",channel:"Marketplace",amount:null,currency:"",
  prices:[{currency:"IRR",amount:100000},{currency:"USD",amount:13.25},{currency:"EUR",amount:20.5}],
  returnPolicyChoice:"Default",customReturnWindowDays:null,minimumOrderQuantity:null,maximumOrderQuantity:null,
  responsibility:null,shippingTypeId:null,inventory:[],
};
function fixture() {
  window.sessionStorage.clear();
  let current: OfferCreateAndApplyDetail = {
    offerId:"offer",sellerPartyId:"seller",catalogVariantId:"variant",sellerSku:"SKU",channel:"Marketplace",
    amount:null,currency:"IRR",prices:[],priceRevision:"absent",status:"Draft",returnPolicyChoice:"Default",
    customReturnWindowDays:null,minimumOrderQuantity:null,maximumOrderQuantity:null,fulfillmentResponsibility:null,
    fulfillmentShippingTypeId:null,inventoryLocations:[],revisions:{offer:"o1",price:"absent",offeredQuantity:"absent"},
    capabilities:{canEditPhysicalInventory:true,canAssignFulfillment:true},
  };
  const calls: Array<{currency:string;amount:number;version:string;operationId:string}> = [];
  let creates=0, committed=0;
  const transport: OfferCreateAndApplyTransport = {
    scope:"multi-fixture",
    create:async()=>{creates++;return {ok:true,offerId:"offer"}},
    load:async()=>({ok:true,detail:current}),
    savePrice:async(_id,amount,currency,version,operationId)=>{
      calls.push({currency,amount,version,operationId});
      const row={priceId:currency,currency,amount,revision:currency+"-rev",status:"Active"};
      current={...current,prices:[...(current.prices??[]),row],...(currency==="IRR"?{amount,priceRevision:row.revision,revisions:{...current.revisions!,price:row.revision}}:{})};
      return {ok:true};
    },
    saveConditions:async()=>({ok:true}),provisionPosition:async()=>({ok:false,errorCode:"unexpected"}),
    saveInventory:async()=>({ok:false,errorCode:"unexpected"}),onCommitted:()=>{committed++},
  };
  return {transport,calls,get creates(){return creates},get committed(){return committed},current:()=>current};
}
test("one create, independent currency revisions and readback of every price",async()=>{
  const f=fixture();
  assert.deepEqual(await createAndApplyOffer(f.transport,input),{ok:true,offerId:"offer"});
  assert.equal(f.creates,1);assert.equal(f.committed,1);
  assert.deepEqual(f.calls.map(({currency,amount,version})=>({currency,amount,version})),input.prices!.map(p=>({...p,version:"absent"})));
  assert.equal(new Set(f.calls.map(x=>x.operationId)).size,3);
  assert.equal(f.current().amount,100000);assert.equal(f.current().prices?.length,3);
});
test("duplicate currencies never create an offer",async()=>{
  const f=fixture();
  const r=await createAndApplyOffer(f.transport,{...input,prices:[{currency:"USD",amount:1},{currency:"usd",amount:2}]});
  assert.equal(r.ok,false);assert.equal(f.creates,0);assert.equal(f.calls.length,0);
});
