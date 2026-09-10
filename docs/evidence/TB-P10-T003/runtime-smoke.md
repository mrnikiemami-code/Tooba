# Runtime smoke
Host :5088 FE :3000. Script: docs/evidence/TB-P10-T003/_runtime.mjs
A methods+FE no CVV PASS
B manual amount==payable PASS
C gateway sandbox redirect PASS
D idempotency same paymentId PASS
E wrong guest rejected PASS
F spoofed amount ignored PASS
G refresh methods PASS
Raw: runtime-smoke-raw.json
