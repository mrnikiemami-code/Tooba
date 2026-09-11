# R3 Sandbox Success Runtime
PASS. Cart→shipping→gateway→/payment/sandbox (store/order/amount/SANDBOX). پرداخت موفق → Host Verify Succeeded. OrderNumber human `TB-...`. Customer order link 200. Converted cart empty. Refresh complete stays Succeeded. Defect fixed: checkout GetAsync no longer rejects Converted empty cart (payment initiate was payment.rejected).
Raw: r3-runtime-raw.json step A-sandbox-success.
