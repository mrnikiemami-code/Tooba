# Cancel eligibility — TB-P09-T016

Locked rule: whole-order cancel until first real dispatched quantity anywhere in the Checkout.

| State | Cancel |
| --- | --- |
| Waiting payment | Allowed |
| Waiting manual confirm | Allowed |
| Paid / ReadyToFulfill | Allowed |
| Processing | Allowed |
| Packed | Allowed |
| Shipment Created | Allowed |
| Created + tracking | Allowed |
| Any Dispatched quantity | Blocked |
| InTransit | Blocked |
| Delivered | Blocked |
| Multi-seller: any one dispatched | Whole cancel blocked |

Packed / Created / tracking do not block. Human block: `پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.`
