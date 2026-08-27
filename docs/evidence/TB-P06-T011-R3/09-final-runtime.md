# 09 — Final runtime (TB-P06-T011-R3)

| Runtime | URL | Status |
| --- | --- | --- |
| Backend | http://127.0.0.1:5088 | live 200 / ready 200 |
| Tooba Frontend | http://127.0.0.1:3000 | `/` 200 |
| Shopeiva | http://127.0.0.1:3001 | `/user-panel/orders` 200 |

## USER-PREVIEW — live return scenario

**Customer live return scenario:**  
http://127.0.0.1:3000/customer-panel/orders/01a0408a-be00-7000-94a1-db0d82532d27

**Seller live return scenario:**  
http://127.0.0.1:3000/vendor-panel/returns/72528d83-a924-4ce4-8d25-8fe9bba88af5?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5

**Original Customer:** http://127.0.0.1:3001/user-panel/orders  
**Original Seller:** http://127.0.0.1:3001/vendor-panel/orders/1

Login: dev BFF injects customer actor; seller via localStorage dev actor + seller party selector.
