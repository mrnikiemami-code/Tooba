const fs=require("fs");const path=require("path");const {randomUUID}=require("crypto");
const L=require("../TB-296-P1-C-INTEGRATION/finalacceptance-d2-lib.cjs");
const ev=process.env.P1C_EVIDENCE_DIR;const OUT=path.join(ev,"reg01.json");const q=L.quote;
(async()=>{
const admin=await L.adminContext();const A=admin.headers;
const offerId=L.buyableOffer(true);const before=await L.call("/v1/admin/offers/"+offerId,"GET",undefined,A);
const primary={amount:before.body.amount,currency:before.body.currency};if(primary.amount==null) throw new Error("demo offer has no primary price");
const stockBefore=L.stockState(offerId);
const customer=await L.registerCustomer("p1c-reg01");
const co0=await L.createCheckout(customer,offerId,1);const co=L.checkoutOf(co0);
const pay=await L.payCheckout(customer,co);
let st=null;for(let i=0;i<20;i++){st=L.orderState(co.checkoutId);if(st.sellerOrders.length&&st.sellerOrders.every(x=>x.status==="Paid"))break;await new Promise(r=>setTimeout(r,400));}
const line=L.psqlJson(`select jsonb_build_object('unit',unit_price_snapshot,'currency',currency,'qty',quantity,'offer',offer_id) from "order".order_lines where seller_order_id in (select seller_order_id from "order".seller_orders where checkout_id='${co.checkoutId}')`);
const offerAfter=await L.call("/v1/admin/offers/"+offerId,"GET",undefined,A);
const ok=st.sellerOrders.length===1&&st.sellerOrders[0].status==="Paid"&&Number(line.unit)===Number(primary.amount)&&line.currency===primary.currency&&offerAfter.body.amount===primary.amount&&offerAfter.body.currency===primary.currency;
// exact cleanup: checkout/cart/payment/reservation/customer only; the demo offer/pricing remain.
const res=L.psqlJson(`select coalesce(jsonb_agg(jsonb_build_object('stock',stock_item_id,'qty',quantity)),'[]'::jsonb) from inventory.reservations where external_reference like '%${co.cartId.replaceAll("-","")}%'`);
const cid=q(co.checkoutId), cartid=q(co.cartId), uid=q(customer.userId);
const paymentIds=`(select payment_id from payment.payments where checkout_id=${cid})`;
const soIds=`(select seller_order_id from "order".seller_orders where checkout_id=${cid})`;
const S=[
`delete from payment.proof_assets where payment_id in ${paymentIds}`,
`delete from "order".payment_inbox where payment_id in ${paymentIds}`,
`delete from "order".order_line_obligation_closures where checkout_id=${cid}`,
`delete from "order".order_shipping_group_lines where shipping_group_id in (select shipping_group_id from "order".order_shipping_groups where checkout_id=${cid})`,
`delete from "order".order_operational_group_occurrence_members where occurrence_id in (select occurrence_id from "order".order_operational_group_occurrences where checkout_id=${cid})`,
`delete from "order".order_operational_group_occurrences where checkout_id=${cid}`,
`delete from "order".order_shipping_groups where checkout_id=${cid}`,
`delete from "order".shipment_dispatch_plans where checkout_id=${cid}`,
`delete from "order".reservation_cycle_events where checkout_id=${cid}`,
`delete from "order".reservation_cycles where checkout_id=${cid}`,
`delete from "order".checkout_processes where checkout_id=${cid} or cart_id=${cartid}`,
`delete from "order".checkout_reservation_commits where checkout_id=${cid}`,
`delete from "order".checkout_admin_view_acks where checkout_id=${cid}`,
`delete from "order".checkout_operational_notes where checkout_id=${cid}`,
`delete from "order".pending_payment_card_hides where checkout_id=${cid}`,
`delete from "order".order_lines where seller_order_id in ${soIds}`,
`delete from "order".seller_orders where checkout_id=${cid}`,
`delete from "order".checkouts where checkout_id=${cid}`,
`delete from payment.allocations where payment_id in ${paymentIds}`,
`delete from payment.attempts where payment_id in ${paymentIds}`,
`delete from payment.payments where checkout_id=${cid}`,
`delete from "order".cart_shipping_group_lines where shipping_group_id in (select shipping_group_id from "order".cart_shipping_groups where cart_id=${cartid})`,
`delete from "order".cart_shipping_groups where cart_id=${cartid}`,
`delete from "order".cart_shipping_drafts where cart_id=${cartid}`,
`delete from cart.cart_lines where cart_id=${cartid}`,
`delete from cart.carts where cart_id=${cartid}`,
`delete from inventory.reservations where external_reference like '%${co.cartId.replaceAll("-","")}%'`,
`delete from identity.auth_sessions where user_id=${uid}`,
`delete from identity.auth_challenges where user_id=${uid}`,
`delete from identity.users where user_id=${uid}`,
...res.map(r=>`update inventory.stock_positions set reserved=greatest(0,reserved-${Number(r.qty)}) where stock_item_id='${r.stock}'`)
];
L.psql("BEGIN;"+S.map(s=>s+";").join("")+"COMMIT;");
const afterCheckout=Number(L.psql(`select count(*) from "order".checkouts where checkout_id=${cid}`));
const afterOffer=await L.call("/v1/admin/offers/"+offerId,"GET",undefined,A);
const stockAfter=L.stockState(offerId);
const result={case:"R11-REG-01",offerId,primary,paid:st.sellerOrders.map(x=>x.status),line,pass:ok&&afterCheckout===0&&afterOffer.status===200&&JSON.stringify(stockBefore)===JSON.stringify(stockAfter),cleanup:{checkouts:afterCheckout,offerStatus:afterOffer.status,stockBefore,stockAfter,reservations:res.length}};
fs.writeFileSync(OUT,JSON.stringify(result,null,2));console.log(JSON.stringify(result));if(!result.pass)process.exit(1);
})().catch(e=>{console.error(e);process.exit(1)});
