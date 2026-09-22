# Permission parity
Endpoint authorization now requests `order.view` for notes/history/invoice/receipt and `order.handle` for add/delete. The Host adapter checks an explicit effective grant and denies by default. Dedicated permission-policy tests remain missing, so this gate is not complete.
